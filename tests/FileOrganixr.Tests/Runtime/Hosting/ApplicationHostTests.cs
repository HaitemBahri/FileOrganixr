using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core;
using FileOrganixr.Core.Configuration;
using FileOrganixr.Core.Configuration.Providers;
using FileOrganixr.Core.Configuration.Stores;
using FileOrganixr.Core.Configuration.Validators;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.Hosting;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Tests.Runtime.Hosting;
public sealed class ApplicationHostTests
{
    [Fact]
    public async Task StartAsync_LoadsValidatesStoresAndStartsServices()
    {
        var config = new ConfigurationRoot();
        var log = new List<string>();
        var provider = new StubConfigurationProvider(config);
        var validator = new StubConfigurationRootValidator(ValidationResult.Empty());
        var store = new RecordingConfigurationStore();
        var orchestrator = new RecordingExecutionOrchestrator(log);
        var watcher = new RecordingWatcherService(log);
        var host = new ApplicationHost(provider, validator, store, orchestrator, watcher);

        await host.StartAsync(CancellationToken.None);

        Assert.True(host.IsRunning);
        Assert.Equal(1, provider.LoadCalls);
        Assert.Equal(1, validator.ValidateCalls);
        Assert.Equal(1, store.SetCalls);
        Assert.Same(config, store.Current);
        Assert.Equal(["orchestrator.start", "watcher.start"], log);
    }

    [Fact]
    public async Task StartAsync_ThrowsWhenValidationFails_AndDoesNotStartServices()
    {
        var config = new ConfigurationRoot();
        var validation = ValidationResult.Empty();
        validation.AddError("Folders[0].Path", "Path is required.");
        var log = new List<string>();
        var provider = new StubConfigurationProvider(config);
        var validator = new StubConfigurationRootValidator(validation);
        var store = new RecordingConfigurationStore();
        var orchestrator = new RecordingExecutionOrchestrator(log);
        var watcher = new RecordingWatcherService(log);
        var host = new ApplicationHost(provider, validator, store, orchestrator, watcher);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.StartAsync(CancellationToken.None));

        Assert.Contains("Configuration validation failed", ex.Message);
        Assert.Contains("Folders[0].Path", ex.Message);
        Assert.False(host.IsRunning);
        Assert.Equal(0, store.SetCalls);
        Assert.Equal(0, orchestrator.StartCalls);
        Assert.Equal(0, watcher.StartCalls);
    }

    [Fact]
    public async Task StartAsync_RollsBackOrchestrator_WhenWatcherStartFails()
    {
        var config = new ConfigurationRoot();
        var log = new List<string>();
        var provider = new StubConfigurationProvider(config);
        var validator = new StubConfigurationRootValidator(ValidationResult.Empty());
        var store = new RecordingConfigurationStore();
        var orchestrator = new RecordingExecutionOrchestrator(log);
        var watcher = new RecordingWatcherService(log) { ThrowOnStart = true };
        var host = new ApplicationHost(provider, validator, store, orchestrator, watcher);

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync(CancellationToken.None));

        Assert.False(host.IsRunning);
        Assert.Equal(1, orchestrator.StartCalls);
        Assert.Equal(1, orchestrator.StopCalls);
        Assert.Equal(1, watcher.StartCalls);
    }

    [Fact]
    public async Task StopAsync_StopsWatcherBeforeOrchestrator()
    {
        var config = new ConfigurationRoot();
        var log = new List<string>();
        var provider = new StubConfigurationProvider(config);
        var validator = new StubConfigurationRootValidator(ValidationResult.Empty());
        var store = new RecordingConfigurationStore();
        var orchestrator = new RecordingExecutionOrchestrator(log);
        var watcher = new RecordingWatcherService(log);
        var host = new ApplicationHost(provider, validator, store, orchestrator, watcher);

        await host.StartAsync(CancellationToken.None);
        await host.StopAsync(CancellationToken.None);

        Assert.False(host.IsRunning);
        Assert.Equal("watcher.stop", log[^2]);
        Assert.Equal("orchestrator.stop", log[^1]);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_DoesNotRepeatWork()
    {
        var config = new ConfigurationRoot();
        var log = new List<string>();
        var provider = new StubConfigurationProvider(config);
        var validator = new StubConfigurationRootValidator(ValidationResult.Empty());
        var store = new RecordingConfigurationStore();
        var orchestrator = new RecordingExecutionOrchestrator(log);
        var watcher = new RecordingWatcherService(log);
        var host = new ApplicationHost(provider, validator, store, orchestrator, watcher);

        await host.StartAsync(CancellationToken.None);
        await host.StartAsync(CancellationToken.None);

        Assert.Equal(1, provider.LoadCalls);
        Assert.Equal(1, validator.ValidateCalls);
        Assert.Equal(1, store.SetCalls);
        Assert.Equal(1, orchestrator.StartCalls);
        Assert.Equal(1, watcher.StartCalls);
    }

    [Fact]
    public async Task StartAsync_ThrowsObjectDisposedException_WhenHostWasDisposed()
    {
        var config = new ConfigurationRoot();
        var provider = new StubConfigurationProvider(config);
        var validator = new StubConfigurationRootValidator(ValidationResult.Empty());
        var store = new RecordingConfigurationStore();
        var orchestrator = new RecordingExecutionOrchestrator([]);
        var watcher = new RecordingWatcherService([]);
        var host = new ApplicationHost(provider, validator, store, orchestrator, watcher);

        host.Dispose();

        await Assert.ThrowsAnyAsync<ObjectDisposedException>(() => host.StartAsync(CancellationToken.None));
    }

    private sealed class StubConfigurationProvider : IConfigurationProvider
    {
        private readonly ConfigurationRoot _root;

        public StubConfigurationProvider(ConfigurationRoot root)
        {
            _root = root;
        }

        public int LoadCalls { get; private set; }

        public Task<ConfigurationRoot> LoadAsync(CancellationToken cancellationToken)
        {
            LoadCalls++;
            return Task.FromResult(_root);
        }
    }

    private sealed class StubConfigurationRootValidator : IConfigurationRootValidator
    {
        private readonly ValidationResult _result;

        public StubConfigurationRootValidator(ValidationResult result)
        {
            _result = result;
        }

        public int ValidateCalls { get; private set; }

        public ValidationResult Validate(ConfigurationRoot root)
        {
            ValidateCalls++;
            return _result;
        }
    }

    private sealed class RecordingConfigurationStore : IConfigurationStore
    {
        private ConfigurationRoot? _current;

        public int SetCalls { get; private set; }

        public ConfigurationRoot Current
        {
            get => _current ?? throw new InvalidOperationException("No configuration set.");
            set
            {
                _current = value;
                SetCalls++;
            }
        }
    }

    private sealed class RecordingExecutionOrchestrator : IExecutionOrchestrator
    {
        private readonly List<string> _log;

        public RecordingExecutionOrchestrator(List<string> log)
        {
            _log = log;
        }

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public void Start()
        {
            StartCalls++;
            _log.Add("orchestrator.start");
        }

        public void Stop()
        {
            StopCalls++;
            _log.Add("orchestrator.stop");
        }

        public void OnRuleMatched(ActionRequest request)
        {
        }

        public void OnApprovalOutcome(ActionRequest request)
        {
        }
    }

    private sealed class RecordingWatcherService : IFileWatcherService
    {
        private readonly List<string> _log;

        public RecordingWatcherService(List<string> log)
        {
            _log = log;
        }

        public bool ThrowOnStart { get; init; }

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            StartCalls++;
            _log.Add("watcher.start");

            if (ThrowOnStart)
            {
                throw new InvalidOperationException("Watcher failed to start.");
            }

            IsRunning = true;
        }

        public void Stop()
        {
            StopCalls++;
            _log.Add("watcher.stop");
            IsRunning = false;
        }
    }
}
