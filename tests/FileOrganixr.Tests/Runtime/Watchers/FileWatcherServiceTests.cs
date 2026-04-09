using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Stores;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Tests.Runtime.Watchers;
public sealed class FileWatcherServiceTests
{
    [Fact]
    public void Start_CreatesAndStartsWatchers_AndIsIdempotent()
    {
        var folderA = CreateFolder("Inbox", "/tmp/file-organixr/inbox");
        var folderB = CreateFolder("Archive", "/tmp/file-organixr/archive");
        var watcherA = new RecordingFolderWatcher(folderA);
        var watcherB = new RecordingFolderWatcher(folderB);
        var factory = new StubFolderWatcherFactory(
            new Dictionary<string, IFolderWatcher>
            {
                [folderA.Path] = watcherA,
                [folderB.Path] = watcherB
            });
        var dispatcher = new RecordingFileEventDispatcher();
        var errorHandler = new RecordingWatcherErrorHandler();
        var service = new FileWatcherService(
            new StubConfigurationStore(new ConfigurationRoot { Folders = [folderA, folderB] }),
            factory,
            dispatcher,
            errorHandler);

        service.Start();
        service.Start();

        Assert.True(service.IsRunning);
        Assert.Equal(2, factory.CreateCalls);
        Assert.Equal(1, watcherA.StartCalls);
        Assert.Equal(1, watcherB.StartCalls);
    }

    [Fact]
    public void Stop_StopsAndDisposesWatchers_AndIsIdempotent()
    {
        var folder = CreateFolder("Inbox", "/tmp/file-organixr/inbox");
        var watcher = new RecordingFolderWatcher(folder);
        var factory = new StubFolderWatcherFactory(new Dictionary<string, IFolderWatcher> { [folder.Path] = watcher });
        var service = new FileWatcherService(
            new StubConfigurationStore(new ConfigurationRoot { Folders = [folder] }),
            factory,
            new RecordingFileEventDispatcher(),
            new RecordingWatcherErrorHandler());

        service.Start();
        service.Stop();
        service.Stop();

        Assert.False(service.IsRunning);
        Assert.Equal(1, watcher.StopCalls);
        Assert.Equal(1, watcher.DisposeCalls);
    }

    [Fact]
    public void OnFileEventReceived_DispatchesToFileEventDispatcher()
    {
        var folder = CreateFolder("Inbox", "/tmp/file-organixr/inbox");
        var watcher = new RecordingFolderWatcher(folder);
        var factory = new StubFolderWatcherFactory(new Dictionary<string, IFolderWatcher> { [folder.Path] = watcher });
        var dispatcher = new RecordingFileEventDispatcher();
        var service = new FileWatcherService(
            new StubConfigurationStore(new ConfigurationRoot { Folders = [folder] }),
            factory,
            dispatcher,
            new RecordingWatcherErrorHandler());
        var fileEvent = new FileEvent(
            WatchedFolderPath: folder.Path,
            Type: FileEventType.Created,
            FullPath: "/tmp/file-organixr/inbox/sample.txt",
            OldFullPath: null,
            TimestampUtc: DateTimeOffset.UtcNow);

        service.Start();
        watcher.RaiseFileEvent(fileEvent);

        Assert.Single(dispatcher.DispatchedEvents);
        Assert.Same(fileEvent, dispatcher.DispatchedEvents[0]);
    }

    [Fact]
    public void OnWatcherError_ForwardsWatcherToErrorHandler()
    {
        var folder = CreateFolder("Inbox", "/tmp/file-organixr/inbox");
        var watcher = new RecordingFolderWatcher(folder);
        var factory = new StubFolderWatcherFactory(new Dictionary<string, IFolderWatcher> { [folder.Path] = watcher });
        var errorHandler = new RecordingWatcherErrorHandler();
        var service = new FileWatcherService(
            new StubConfigurationStore(new ConfigurationRoot { Folders = [folder] }),
            factory,
            new RecordingFileEventDispatcher(),
            errorHandler);
        var error = new WatcherErrorEvent("boom", null, DateTimeOffset.UtcNow);

        service.Start();
        watcher.RaiseError(error);

        Assert.Single(errorHandler.Errors);
        Assert.Same(watcher, errorHandler.Errors[0].Watcher);
        Assert.Equal(error, errorHandler.Errors[0].Error);
    }

    [Fact]
    public void OnWatcherError_UsesNullWatcher_WhenEventSenderIsUnknown()
    {
        var folder = CreateFolder("Inbox", "/tmp/file-organixr/inbox");
        var watcher = new RecordingFolderWatcher(folder);
        var factory = new StubFolderWatcherFactory(new Dictionary<string, IFolderWatcher> { [folder.Path] = watcher });
        var errorHandler = new RecordingWatcherErrorHandler();
        var service = new FileWatcherService(
            new StubConfigurationStore(new ConfigurationRoot { Folders = [folder] }),
            factory,
            new RecordingFileEventDispatcher(),
            errorHandler);
        var error = new WatcherErrorEvent("boom", null, DateTimeOffset.UtcNow);

        service.Start();
        watcher.RaiseErrorWithUnknownSender(error);

        Assert.Single(errorHandler.Errors);
        Assert.Null(errorHandler.Errors[0].Watcher);
        Assert.Equal(error, errorHandler.Errors[0].Error);
    }

    [Fact]
    public void Start_RollsBackCreatedWatchers_WhenStartupFails()
    {
        var folderA = CreateFolder("Inbox", "/tmp/file-organixr/inbox");
        var folderB = CreateFolder("Archive", "/tmp/file-organixr/archive");
        var watcherA = new RecordingFolderWatcher(folderA);
        var watcherB = new RecordingFolderWatcher(folderB) { ThrowOnStart = true };
        var factory = new StubFolderWatcherFactory(
            new Dictionary<string, IFolderWatcher>
            {
                [folderA.Path] = watcherA,
                [folderB.Path] = watcherB
            });
        var service = new FileWatcherService(
            new StubConfigurationStore(new ConfigurationRoot { Folders = [folderA, folderB] }),
            factory,
            new RecordingFileEventDispatcher(),
            new RecordingWatcherErrorHandler());

        Assert.Throws<InvalidOperationException>(() => service.Start());
        Assert.False(service.IsRunning);
        Assert.Equal(1, watcherA.StopCalls);
        Assert.Equal(1, watcherA.DisposeCalls);
        Assert.Equal(1, watcherB.StopCalls);
        Assert.Equal(1, watcherB.DisposeCalls);
    }

    private static FolderDefinition CreateFolder(string name, string path)
    {
        return new FolderDefinition
        {
            Name = name,
            Path = path,
            Rules = []
        };
    }

    private sealed class StubConfigurationStore : IConfigurationStore
    {
        public StubConfigurationStore(ConfigurationRoot current)
        {
            Current = current;
        }

        public ConfigurationRoot Current { get; set; }
    }

    private sealed class StubFolderWatcherFactory : IFolderWatcherFactory
    {
        private readonly Dictionary<string, IFolderWatcher> _watchersByPath;

        public StubFolderWatcherFactory(Dictionary<string, IFolderWatcher> watchersByPath)
        {
            _watchersByPath = watchersByPath;
        }

        public int CreateCalls { get; private set; }

        public IFolderWatcher Create(FolderDefinition folder)
        {
            CreateCalls++;
            return _watchersByPath[folder.Path];
        }
    }

    private sealed class RecordingFileEventDispatcher : IFileEventDispatcher
    {
        public List<FileEvent> DispatchedEvents { get; } = [];

        public void Dispatch(FileEvent fileEvent)
        {
            DispatchedEvents.Add(fileEvent);
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

    private sealed class RecordingFolderWatcher : IFolderWatcher
    {
        public RecordingFolderWatcher(FolderDefinition folder)
        {
            Folder = folder;
        }

        public event EventHandler<FileEvent>? FileEventReceived;

        public event EventHandler<WatcherErrorEvent>? Error;

        public FolderDefinition Folder { get; }

        public bool ThrowOnStart { get; init; }

        public int DisposeCalls { get; private set; }

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public void Dispose()
        {
            DisposeCalls++;
        }

        public void Start()
        {
            StartCalls++;
            if (ThrowOnStart)
            {
                throw new InvalidOperationException("Watcher start failed.");
            }
        }

        public void Stop()
        {
            StopCalls++;
        }

        public void RaiseFileEvent(FileEvent fileEvent)
        {
            FileEventReceived?.Invoke(this, fileEvent);
        }

        public void RaiseError(WatcherErrorEvent error)
        {
            Error?.Invoke(this, error);
        }

        public void RaiseErrorWithUnknownSender(WatcherErrorEvent error)
        {
            Error?.Invoke(new object(), error);
        }
    }
}
