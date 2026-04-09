using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Stores;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Core.Watchers.FileEvents;
public sealed class FileWatcherService : IFileWatcherService, IDisposable
{
    private readonly List<IFolderWatcher> _activeWatchers = new();

    private readonly IConfigurationStore _configurationStore;

    private readonly IFileEventDispatcher _fileEventDispatcher;

    private readonly IFolderWatcherFactory _folderWatcherFactory;

    private readonly object _gate = new();

    private readonly IWatcherErrorHandler _watcherErrorHandler;
    private bool _isRunning;

    public FileWatcherService(
        IConfigurationStore configurationStore,
        IFolderWatcherFactory folderWatcherFactory,
        IFileEventDispatcher fileEventDispatcher,
        IWatcherErrorHandler watcherErrorHandler)
    {
        _configurationStore = configurationStore;

        _folderWatcherFactory = folderWatcherFactory;

        _fileEventDispatcher = fileEventDispatcher;

        _watcherErrorHandler = watcherErrorHandler;
    }

    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _isRunning;
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_isRunning) return;

            var config = _configurationStore.Current;

            var folders = config.Folders ?? [];

            var startupWatchers = new List<IFolderWatcher>();

            try
            {
                foreach (var folder in folders)
                {
                    if (folder is null) continue;

                    var watcher = _folderWatcherFactory.Create(folder);

                    watcher.FileEventReceived += OnFileEventReceived;

                    watcher.Error += OnWatcherError;

                    startupWatchers.Add(watcher);
                }

                foreach (var watcher in startupWatchers) watcher.Start();

                _activeWatchers.Clear();
                _activeWatchers.AddRange(startupWatchers);
                _isRunning = true;
            }
            catch
            {
                foreach (var watcher in startupWatchers)
                {
                    try
                    {
                        watcher.FileEventReceived -= OnFileEventReceived;
                        watcher.Error -= OnWatcherError;
                        watcher.Stop();
                        watcher.Dispose();
                    }
                    catch
                    {

                    }
                }

                throw;
            }
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!_isRunning) return;

            foreach (var watcher in _activeWatchers)
            {
                watcher.FileEventReceived -= OnFileEventReceived;

                watcher.Error -= OnWatcherError;

                watcher.Stop();

                watcher.Dispose();
            }

            _activeWatchers.Clear();

            _isRunning = false;
        }
    }

    private void OnFileEventReceived(object? sender, FileEvent fileEvent)
    {
        _fileEventDispatcher.Dispatch(fileEvent);
    }

    private void OnWatcherError(object? sender, WatcherErrorEvent error)
    {
        if (sender is not IFolderWatcher watcher)
        {
            _watcherErrorHandler.Handle(null, error);
            return;
        }

        _watcherErrorHandler.Handle(watcher, error);
    }
}
