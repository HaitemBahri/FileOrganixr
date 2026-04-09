using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Configuration.Stores;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;
using FileOrganixr.Core.Runtime.Rules;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Tests.Runtime.Watchers;
public sealed class FileEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_StoresNoRuleMatchedRequest_WhenNoRuleMatches()
    {
        var folder = CreateFolder();
        var fileContext = CreateFileContext("/tmp/file-organixr/inbox/sample.txt");
        var requestStore = new RecordingActionRequestStore();
        var orchestrator = new RecordingExecutionOrchestrator();
        var handler = new FileEventHandler(
            new StubConfigurationStore(),
            new StubFolderDefinitionResolver(folder),
            new StubFileContextFactory(fileContext),
            new StubRuleMatcher(null),
            new ActionRequestUpdater(),
            orchestrator,
            requestStore);

        await handler.HandleAsync(CreateDirectChildEvent(), CancellationToken.None);

        Assert.Single(requestStore.Items);
        var request = requestStore.Items[0];
        Assert.Equal(ActionRequestStatus.NoRuleMatched, request.CurrentStatus);
        Assert.Equal(ActionRequestStatus.Detected, request.History[0].Status);
        Assert.Equal(ActionRequestStatus.NoRuleMatched, request.History[1].Status);
        Assert.Empty(orchestrator.RuleMatchedRequests);
    }

    [Fact]
    public async Task HandleAsync_StoresRuleMatchedRequest_AndNotifiesOrchestrator()
    {
        var folder = CreateFolder();
        var fileContext = CreateFileContext("/tmp/file-organixr/inbox/sample.txt");
        var matchedRule = new RuleDefinition
        {
            Name = "Match txt",
            Action = new DeleteActionDefinition(),
            Query = new RegexFileNameQueryDefinition { Pattern = ".*\\.txt$" }
        };
        var requestStore = new RecordingActionRequestStore();
        var orchestrator = new RecordingExecutionOrchestrator();
        var handler = new FileEventHandler(
            new StubConfigurationStore(),
            new StubFolderDefinitionResolver(folder),
            new StubFileContextFactory(fileContext),
            new StubRuleMatcher(matchedRule),
            new ActionRequestUpdater(),
            orchestrator,
            requestStore);

        await handler.HandleAsync(CreateDirectChildEvent(), CancellationToken.None);

        Assert.Single(requestStore.Items);
        Assert.Single(orchestrator.RuleMatchedRequests);

        var stored = requestStore.Items[0];
        Assert.Same(stored, orchestrator.RuleMatchedRequests[0]);
        Assert.Equal(ActionRequestStatus.RuleMatched, stored.CurrentStatus);
        Assert.Same(matchedRule, stored.RuleSnapshot);
        Assert.Equal("Match txt", stored.History[^1].Note);
    }

    [Fact]
    public async Task HandleAsync_IgnoresEvent_WhenEventIsNotDirectChildFile()
    {
        var folder = CreateFolder();
        var fileContext = CreateFileContext("/tmp/file-organixr/inbox/sub/sample.txt");
        var requestStore = new RecordingActionRequestStore();
        var fileContextFactory = new StubFileContextFactory(fileContext);
        var ruleMatcher = new StubRuleMatcher(null);
        var orchestrator = new RecordingExecutionOrchestrator();
        var handler = new FileEventHandler(
            new StubConfigurationStore(),
            new StubFolderDefinitionResolver(folder),
            fileContextFactory,
            ruleMatcher,
            new ActionRequestUpdater(),
            orchestrator,
            requestStore);

        var nestedEvent = new FileEvent(
            WatchedFolderPath: "/tmp/file-organixr/inbox",
            Type: FileEventType.Created,
            FullPath: "/tmp/file-organixr/inbox/sub/sample.txt",
            OldFullPath: null,
            TimestampUtc: DateTimeOffset.UtcNow);

        await handler.HandleAsync(nestedEvent, CancellationToken.None);

        Assert.Empty(requestStore.Items);
        Assert.Empty(orchestrator.RuleMatchedRequests);
        Assert.Equal(0, fileContextFactory.CreateCalls);
        Assert.Equal(0, ruleMatcher.FindCalls);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCanceledTask_WhenTokenIsAlreadyCanceled()
    {
        var folder = CreateFolder();
        var fileContext = CreateFileContext("/tmp/file-organixr/inbox/sample.txt");
        var requestStore = new RecordingActionRequestStore();
        var fileContextFactory = new StubFileContextFactory(fileContext);
        var ruleMatcher = new StubRuleMatcher(null);
        var orchestrator = new RecordingExecutionOrchestrator();
        var handler = new FileEventHandler(
            new StubConfigurationStore(),
            new StubFolderDefinitionResolver(folder),
            fileContextFactory,
            ruleMatcher,
            new ActionRequestUpdater(),
            orchestrator,
            requestStore);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(CreateDirectChildEvent(), cts.Token));
        Assert.Empty(requestStore.Items);
        Assert.Equal(0, fileContextFactory.CreateCalls);
        Assert.Equal(0, ruleMatcher.FindCalls);
        Assert.Empty(orchestrator.RuleMatchedRequests);
    }

    private static FolderDefinition CreateFolder()
    {
        return new FolderDefinition
        {
            Name = "Inbox",
            Path = "/tmp/file-organixr/inbox",
            Rules = []
        };
    }

    private static FileContext CreateFileContext(string fullPath)
    {
        return new FileContext
        {
            FullPath = fullPath,
            FileName = "sample.txt",
            FileNameWithoutExtension = "sample",
            Extension = ".txt",
            SizeBytes = 10
        };
    }

    private static FileEvent CreateDirectChildEvent()
    {
        return new FileEvent(
            WatchedFolderPath: "/tmp/file-organixr/inbox",
            Type: FileEventType.Created,
            FullPath: "/tmp/file-organixr/inbox/sample.txt",
            OldFullPath: null,
            TimestampUtc: DateTimeOffset.UtcNow);
    }

    private sealed class StubConfigurationStore : IConfigurationStore
    {
        public ConfigurationRoot Current { get; set; } = new();
    }

    private sealed class StubFolderDefinitionResolver : IFolderDefinitionResolver
    {
        private readonly FolderDefinition _folder;

        public StubFolderDefinitionResolver(FolderDefinition folder)
        {
            _folder = folder;
        }

        public FolderDefinition Resolve(string watchedFolderPath)
        {
            return _folder;
        }
    }

    private sealed class StubFileContextFactory : IFileContextFactory
    {
        private readonly FileContext _fileContext;

        public StubFileContextFactory(FileContext fileContext)
        {
            _fileContext = fileContext;
        }

        public int CreateCalls { get; private set; }

        public FileContext Create(FileEvent fileEvent)
        {
            CreateCalls++;
            return _fileContext;
        }
    }

    private sealed class StubRuleMatcher : IRuleMatcher
    {
        private readonly RuleDefinition? _matchedRule;

        public StubRuleMatcher(RuleDefinition? matchedRule)
        {
            _matchedRule = matchedRule;
        }

        public int FindCalls { get; private set; }

        public RuleDefinition? FindFirstMatch(FolderDefinition folder, FileContext file)
        {
            FindCalls++;
            return _matchedRule;
        }
    }

    private sealed class RecordingExecutionOrchestrator : IExecutionOrchestrator
    {
        public List<ActionRequest> RuleMatchedRequests { get; } = [];

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void OnRuleMatched(ActionRequest request)
        {
            RuleMatchedRequests.Add(request);
        }

        public void OnApprovalOutcome(ActionRequest request)
        {
        }
    }

    private sealed class RecordingActionRequestStore : IActionRequestStore
    {
        public event EventHandler<ActionRequest>? Added;

        public List<ActionRequest> Items { get; } = [];

        public void Add(ActionRequest request)
        {
            Items.Add(request);
            Added?.Invoke(this, request);
        }

        public IReadOnlyList<ActionRequest> GetSnapshot()
        {
            return Items;
        }
    }
}
