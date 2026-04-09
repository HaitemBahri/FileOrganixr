using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core;
using FileOrganixr.Core.Configuration;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Configuration.Providers;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;
using FileOrganixr.Core.Runtime.Hosting;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;
using FileOrganixr.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FileOrganixr.Tests.Integration;
public sealed class ExecutionPipelineIntegrationTests
{
    [Fact]
    public async Task NoApproval_Move_Completes_AndPersistsRequest()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);
            Directory.CreateDirectory(paths.ArchivePath);

            var fileName = "invoice.txt";
            var sourcePath = Path.Combine(paths.InboxPath, fileName);
            var destinationPath = Path.Combine(paths.ArchivePath, fileName);
            await File.WriteAllTextAsync(sourcePath, "payload");

            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Move txt",
                new MoveActionDefinition { DestinationPath = paths.ArchivePath },
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: false);

            await using var system = TestSystem.Create(config);
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitCreated(sourcePath);

            var request = await WaitForSingleRequestWithStatusAsync(
                system.RequestStore,
                ActionRequestStatus.Completed,
                TimeSpan.FromSeconds(3));

            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(
                new[]
                {
                    ActionRequestStatus.Detected,
                    ActionRequestStatus.RuleMatched,
                    ActionRequestStatus.Queued,
                    ActionRequestStatus.Processing,
                    ActionRequestStatus.Completed
                },
                request.History.Select(x => x.Status).ToArray());
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task ApprovalRequired_WaitsPending_UntilApproved()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);
            Directory.CreateDirectory(paths.ArchivePath);

            var fileName = "needs-approval.txt";
            var sourcePath = Path.Combine(paths.InboxPath, fileName);
            var destinationPath = Path.Combine(paths.ArchivePath, fileName);
            await File.WriteAllTextAsync(sourcePath, "payload");

            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Move with approval",
                new MoveActionDefinition { DestinationPath = paths.ArchivePath },
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: true);

            await using var system = TestSystem.Create(config);
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitCreated(sourcePath);

            var request = await WaitForSingleRequestWithStatusAsync(
                system.RequestStore,
                ActionRequestStatus.PendingApproval,
                TimeSpan.FromSeconds(3));

            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(destinationPath));

            var approved = system.ApprovalWorkflow.Approve(request.Id);
            Assert.NotNull(approved);

            await WaitForStatusAsync(request, ActionRequestStatus.Completed, TimeSpan.FromSeconds(3));

            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(
                new[]
                {
                    ActionRequestStatus.Detected,
                    ActionRequestStatus.RuleMatched,
                    ActionRequestStatus.PendingApproval,
                    ActionRequestStatus.Approved,
                    ActionRequestStatus.Queued,
                    ActionRequestStatus.Processing,
                    ActionRequestStatus.Completed
                },
                request.History.Select(x => x.Status).ToArray());
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task ApprovalRequired_Reject_KeepsFileUnchanged()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);

            var fileName = "reject-me.txt";
            var sourcePath = Path.Combine(paths.InboxPath, fileName);
            await File.WriteAllTextAsync(sourcePath, "payload");

            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Delete with approval",
                new DeleteActionDefinition(),
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: true);

            await using var system = TestSystem.Create(config);
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitCreated(sourcePath);

            var request = await WaitForSingleRequestWithStatusAsync(
                system.RequestStore,
                ActionRequestStatus.PendingApproval,
                TimeSpan.FromSeconds(3));

            var rejected = system.ApprovalWorkflow.Reject(request.Id);
            Assert.NotNull(rejected);

            await WaitForStatusAsync(request, ActionRequestStatus.Rejected, TimeSpan.FromSeconds(3));

            Assert.True(File.Exists(sourcePath));
            Assert.Equal(ActionRequestStatus.Rejected, request.CurrentStatus);
            Assert.DoesNotContain(request.History, x => x.Status == ActionRequestStatus.Processing);
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task NoRuleMatch_PersistsNoRuleMatched_WithoutExecution()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);
            Directory.CreateDirectory(paths.ArchivePath);

            var fileName = "ignore-me.txt";
            var sourcePath = Path.Combine(paths.InboxPath, fileName);
            var destinationPath = Path.Combine(paths.ArchivePath, fileName);
            await File.WriteAllTextAsync(sourcePath, "payload");

            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Rule that should not match",
                new MoveActionDefinition { DestinationPath = paths.ArchivePath },
                new RegexFileNameQueryDefinition { Pattern = "^never-match\\.txt$", IgnoreCase = false },
                userApproval: false);

            await using var system = TestSystem.Create(config);
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitCreated(sourcePath);

            var request = await WaitForSingleRequestWithStatusAsync(
                system.RequestStore,
                ActionRequestStatus.NoRuleMatched,
                TimeSpan.FromSeconds(3));

            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(destinationPath));
            Assert.Equal(
                new[]
                {
                    ActionRequestStatus.Detected,
                    ActionRequestStatus.NoRuleMatched
                },
                request.History.Select(x => x.Status).ToArray());
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task WatcherLifecycle_StartStop_WiresAndUnwiresPipeline()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);

            var firstFile = Path.Combine(paths.InboxPath, "first.txt");
            var secondFile = Path.Combine(paths.InboxPath, "second.txt");
            await File.WriteAllTextAsync(firstFile, "first");
            await File.WriteAllTextAsync(secondFile, "second");

            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Delete txt",
                new DeleteActionDefinition(),
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: false);

            await using var system = TestSystem.Create(config);
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitCreated(firstFile);

            await WaitForSingleRequestWithStatusAsync(
                system.RequestStore,
                ActionRequestStatus.Completed,
                TimeSpan.FromSeconds(3));

            Assert.False(File.Exists(firstFile));

            await system.StopAsync();

            var beforeCount = system.RequestStore.GetSnapshot().Count;
            watcher.EmitCreated(secondFile);
            await Task.Delay(250);
            var afterCount = system.RequestStore.GetSnapshot().Count;

            Assert.Equal(beforeCount, afterCount);
            Assert.True(File.Exists(secondFile));
            Assert.Equal(1, watcher.StartCalls);
            Assert.Equal(1, watcher.StopCalls);
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task Move_MissingSourceFile_MarksRequestFailed()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);
            Directory.CreateDirectory(paths.ArchivePath);

            var missingSourcePath = Path.Combine(paths.InboxPath, "missing.txt");
            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Move missing file",
                new MoveActionDefinition { DestinationPath = paths.ArchivePath },
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: false);

            await using var system = TestSystem.Create(config);
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitCreated(missingSourcePath);

            var request = await WaitForSingleRequestWithStatusAsync(
                system.RequestStore,
                ActionRequestStatus.Failed,
                TimeSpan.FromSeconds(3));

            Assert.Equal(ActionRequestStatus.Failed, request.CurrentStatus);
            Assert.Contains(request.History, x => x.Status == ActionRequestStatus.Processing);
            Assert.False(string.IsNullOrWhiteSpace(request.History[^1].Note));
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task Delete_DuplicateEvents_RemainsStable()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);

            var filePath = Path.Combine(paths.InboxPath, "duplicate.txt");
            await File.WriteAllTextAsync(filePath, "payload");

            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Delete txt",
                new DeleteActionDefinition(),
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: false);

            await using var system = TestSystem.Create(config);
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitCreated(filePath);
            watcher.EmitCreated(filePath);

            var requests = await WaitForRequestCountWithAllStatusAsync(
                system.RequestStore,
                expectedCount: 2,
                expectedStatus: ActionRequestStatus.Completed,
                timeout: TimeSpan.FromSeconds(4));

            Assert.Equal(2, requests.Count);
            Assert.False(File.Exists(filePath));
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task DirectoryEvent_IsIgnored_EndToEnd()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);
            var nestedDirectory = Path.Combine(paths.InboxPath, "sub");
            Directory.CreateDirectory(nestedDirectory);

            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Delete txt",
                new DeleteActionDefinition(),
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: false);

            await using var system = TestSystem.Create(config);
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitCreated(nestedDirectory);

            await Task.Delay(250);
            var snapshot = system.RequestStore.GetSnapshot();

            Assert.Empty(snapshot);
            Assert.True(Directory.Exists(nestedDirectory));
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task WatcherError_DoesNotBreakSubsequentProcessing()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);
            var filePath = Path.Combine(paths.InboxPath, "after-error.txt");
            await File.WriteAllTextAsync(filePath, "payload");

            var errorHandler = new RecordingWatcherErrorHandler();
            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Delete txt",
                new DeleteActionDefinition(),
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: false);

            await using var system = TestSystem.Create(
                config,
                services => services.AddSingleton<IWatcherErrorHandler>(errorHandler));
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitError(new WatcherErrorEvent("Synthetic watcher failure", null, DateTimeOffset.UtcNow));
            await WaitForConditionAsync(
                () => errorHandler.Errors.Count > 0,
                TimeSpan.FromSeconds(2),
                "Watcher error event was not observed.");

            watcher.EmitCreated(filePath);
            var request = await WaitForSingleRequestWithStatusAsync(
                system.RequestStore,
                ActionRequestStatus.Completed,
                TimeSpan.FromSeconds(3));

            Assert.Single(errorHandler.Errors);
            Assert.Equal(ActionRequestStatus.Completed, request.CurrentStatus);
            Assert.False(File.Exists(filePath));
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task BurstEvents_AllRequestsPersisted()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);

            const int eventCount = 20;
            var files = new List<string>(eventCount);
            for (var i = 0; i < eventCount; i++)
            {
                var filePath = Path.Combine(paths.InboxPath, $"batch-{i:D2}.txt");
                files.Add(filePath);
                await File.WriteAllTextAsync(filePath, $"payload-{i}");
            }

            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Delete txt",
                new DeleteActionDefinition(),
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: false);

            await using var system = TestSystem.Create(config);
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            foreach (var file in files)
            {
                watcher.EmitCreated(file);
            }

            var requests = await WaitForRequestCountWithAllStatusAsync(
                system.RequestStore,
                expectedCount: eventCount,
                expectedStatus: ActionRequestStatus.Completed,
                timeout: TimeSpan.FromSeconds(8));

            Assert.Equal(eventCount, requests.Count);
            foreach (var file in files)
            {
                Assert.False(File.Exists(file));
            }
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    [Fact]
    public async Task StopDuringInFlightExecution_RecordsCancellationFailure()
    {
        var paths = CreateTestPaths();
        try
        {
            Directory.CreateDirectory(paths.InboxPath);

            var filePath = Path.Combine(paths.InboxPath, "long-running.txt");
            await File.WriteAllTextAsync(filePath, "payload");

            var blockingExecutor = new BlockingActionExecutor();
            var config = CreateSingleRuleConfiguration(
                paths.InboxPath,
                "Delete txt",
                new DeleteActionDefinition(),
                new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$", IgnoreCase = true },
                userApproval: false);

            await using var system = TestSystem.Create(
                config,
                services => services.AddSingleton<IActionExecutor>(blockingExecutor));
            await system.StartAsync();

            var watcher = system.WatcherFactory.GetWatcher(paths.InboxPath);
            watcher.EmitCreated(filePath);

            var request = await WaitForSingleRequestWithStatusAsync(
                system.RequestStore,
                ActionRequestStatus.Processing,
                TimeSpan.FromSeconds(3));

            await system.StopAsync();
            await WaitForStatusAsync(request, ActionRequestStatus.Failed, TimeSpan.FromSeconds(3));

            Assert.Equal(ActionRequestStatus.Failed, request.CurrentStatus);
            Assert.Equal("Cancelled", request.History[^1].Note);
            Assert.Equal(1, blockingExecutor.ExecuteCalls);
        }
        finally
        {
            DeleteDirectoryIfExists(paths.RootPath);
        }
    }

    private static ConfigurationRoot CreateSingleRuleConfiguration(
        string folderPath,
        string ruleName,
        IActionDefinition action,
        IQueryDefinition query,
        bool userApproval)
    {
        return new ConfigurationRoot
        {
            Folders = new List<FolderDefinition>
            {
                new()
                {
                    Name = "Inbox",
                    Path = folderPath,
                    Rules = new List<RuleDefinition>
                    {
                        new()
                        {
                            Name = ruleName,
                            Action = action,
                            Query = query,
                            UserApproval = userApproval
                        }
                    }
                }
            }
        };
    }

    private static async Task<ActionRequest> WaitForSingleRequestWithStatusAsync(
        IActionRequestStore store,
        ActionRequestStatus status,
        TimeSpan timeout)
    {
        var startedAt = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            var snapshot = store.GetSnapshot();
            var request = snapshot.SingleOrDefault();

            if (request is not null && request.CurrentStatus == status)
            {
                return request;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException($"Timed out waiting for a single request in status '{status}'.");
    }

    private static async Task WaitForStatusAsync(
        ActionRequest request,
        ActionRequestStatus status,
        TimeSpan timeout)
    {
        var startedAt = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            if (request.CurrentStatus == status)
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException(
            $"Timed out waiting for status '{status}'. Last observed status '{request.CurrentStatus}'.");
    }

    private static async Task<IReadOnlyList<ActionRequest>> WaitForRequestCountWithAllStatusAsync(
        IActionRequestStore store,
        int expectedCount,
        ActionRequestStatus expectedStatus,
        TimeSpan timeout)
    {
        var startedAt = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            var snapshot = store.GetSnapshot();
            if (snapshot.Count == expectedCount && snapshot.All(r => r.CurrentStatus == expectedStatus))
            {
                return snapshot;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException(
            $"Timed out waiting for {expectedCount} requests in status '{expectedStatus}'.");
    }

    private static async Task WaitForConditionAsync(
        Func<bool> condition,
        TimeSpan timeout,
        string timeoutMessage)
    {
        var startedAt = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException(timeoutMessage);
    }

    private static (string RootPath, string InboxPath, string ArchivePath) CreateTestPaths()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "FileOrganixr.Tests", Guid.NewGuid().ToString("N"));
        var inboxPath = Path.Combine(rootPath, "inbox");
        var archivePath = Path.Combine(rootPath, "archive");
        return (rootPath, inboxPath, archivePath);
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class StubConfigurationProvider : IConfigurationProvider
    {
        private readonly ConfigurationRoot _configuration;

        public StubConfigurationProvider(ConfigurationRoot configuration)
        {
            _configuration = configuration;
        }

        public Task<ConfigurationRoot> LoadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_configuration);
        }
    }

    private sealed class TestFolderWatcherFactory : IFolderWatcherFactory
    {
        private readonly Dictionary<string, TestFolderWatcher> _watchers =
            new(StringComparer.OrdinalIgnoreCase);

        public IFolderWatcher Create(FolderDefinition folder)
        {
            var watcher = new TestFolderWatcher(folder);
            _watchers[folder.Path] = watcher;
            return watcher;
        }

        public TestFolderWatcher GetWatcher(string folderPath)
        {
            if (_watchers.TryGetValue(folderPath, out var watcher))
            {
                return watcher;
            }

            throw new InvalidOperationException($"Watcher for path '{folderPath}' was not created.");
        }
    }

    private sealed class TestFolderWatcher : IFolderWatcher
    {
        public TestFolderWatcher(FolderDefinition folder)
        {
            Folder = folder;
        }

        public FolderDefinition Folder { get; }

        public event EventHandler<FileEvent>? FileEventReceived;

        public event EventHandler<WatcherErrorEvent>? Error;

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public void Start()
        {
            StartCalls++;
        }

        public void Stop()
        {
            StopCalls++;
        }

        public void Dispose()
        {
        }

        public void EmitCreated(string fullPath)
        {
            var fileEvent = new FileEvent(
                WatchedFolderPath: Folder.Path,
                Type: FileEventType.Created,
                FullPath: fullPath,
                OldFullPath: null,
                TimestampUtc: DateTimeOffset.UtcNow);

            FileEventReceived?.Invoke(this, fileEvent);
        }

        public void EmitError(WatcherErrorEvent error)
        {
            Error?.Invoke(this, error);
        }
    }

    private sealed class RecordingWatcherErrorHandler : IWatcherErrorHandler
    {
        public List<(IFolderWatcher? Watcher, WatcherErrorEvent Error)> Errors { get; } = [];

        public void Handle(IFolderWatcher? watcher, WatcherErrorEvent error)
        {
            Errors.Add((watcher, error));
        }
    }

    private sealed class BlockingActionExecutor : IActionExecutor
    {
        public int ExecuteCalls { get; private set; }

        public async Task ExecuteAsync(ActionRequest request, CancellationToken cancellationToken)
        {
            ExecuteCalls++;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class TestSystem : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private bool _started;

        private TestSystem(
            ServiceProvider provider,
            IApplicationHost host,
            TestFolderWatcherFactory watcherFactory,
            IActionRequestStore requestStore,
            IApprovalWorkflow approvalWorkflow)
        {
            _provider = provider;
            Host = host;
            WatcherFactory = watcherFactory;
            RequestStore = requestStore;
            ApprovalWorkflow = approvalWorkflow;
        }

        public IApprovalWorkflow ApprovalWorkflow { get; }

        public IApplicationHost Host { get; }

        public IActionRequestStore RequestStore { get; }

        public TestFolderWatcherFactory WatcherFactory { get; }

        public static TestSystem Create(
            ConfigurationRoot configuration,
            Action<IServiceCollection>? configureServices = null)
        {
            var services = new ServiceCollection();
            services.RegisterCoreServices();
            services.RegisterInfrastructureServices();

            var watcherFactory = new TestFolderWatcherFactory();
            services.AddSingleton<IFolderWatcherFactory>(watcherFactory);
            services.AddSingleton<IConfigurationProvider>(new StubConfigurationProvider(configuration));
            configureServices?.Invoke(services);

            var provider = services.BuildServiceProvider();
            var host = provider.GetRequiredService<IApplicationHost>();
            var requestStore = provider.GetRequiredService<IActionRequestStore>();
            var approvalWorkflow = provider.GetRequiredService<IApprovalWorkflow>();

            return new TestSystem(provider, host, watcherFactory, requestStore, approvalWorkflow);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_started)
                {
                    await Host.StopAsync(CancellationToken.None);
                }
            }
            finally
            {
                await _provider.DisposeAsync();
            }
        }

        public async Task StartAsync()
        {
            await Host.StartAsync(CancellationToken.None);
            _started = true;
        }

        public async Task StopAsync()
        {
            if (!_started)
            {
                return;
            }

            await Host.StopAsync(CancellationToken.None);
            _started = false;
        }
    }
}
