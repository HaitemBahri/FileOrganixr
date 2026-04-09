using System;
using System.IO;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Infrastructure.Watchers;
public sealed class FolderWatcher : IFolderWatcher
{
    private readonly object _gate = new();

    private readonly FileSystemWatcher _watcher;

    private bool _isDisposed;

    private bool _isRunning;

    public FolderWatcher(FolderDefinition folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        Folder = folder;

        if (string.IsNullOrWhiteSpace(folder.Path))
            throw new ArgumentException("FolderDefinition.Path must be provided.", nameof(folder));

        _watcher = new FileSystemWatcher(folder.Path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,

            IncludeSubdirectories = false,

            EnableRaisingEvents = false
        };


        _watcher.Created += OnCreated;


        _watcher.Changed += OnChanged;


        _watcher.Renamed += OnRenamed;


        _watcher.Error += OnError;
    }

    public FolderDefinition Folder { get; }

    public event EventHandler<WatcherErrorEvent>? Error;

    public event EventHandler<FileEvent>? FileEventReceived;

    public void Dispose()
    {

        lock (_gate)
        {

            if (_isDisposed)

                return;


            _watcher.EnableRaisingEvents = false;


            _watcher.Created -= OnCreated;


            _watcher.Changed -= OnChanged;


            _watcher.Renamed -= OnRenamed;


            _watcher.Error -= OnError;


            _watcher.Dispose();


            _isDisposed = true;


            _isRunning = false;
        }
    }

    public void Start()
    {

        lock (_gate)
        {

            if (_isDisposed)

                throw new ObjectDisposedException(nameof(FolderWatcher));


            if (_isRunning)

                return;


            _watcher.EnableRaisingEvents = true;


            _isRunning = true;
        }
    }

    public void Stop()
    {

        lock (_gate)
        {

            if (_isDisposed)

                return;


            if (!_isRunning)

                return;


            _watcher.EnableRaisingEvents = false;


            _isRunning = false;
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {

        RaiseFileEvent(FileEventType.Changed, e.FullPath, null);
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {

        RaiseFileEvent(FileEventType.Created, e.FullPath, null);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {

        var ex = e.GetException();


        var message = ex is null ? "File system watcher error occurred." : ex.Message;


        var error = new WatcherErrorEvent(

            message,

            ex,

            DateTimeOffset.UtcNow);


        Error?.Invoke(this, error);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {

        RaiseFileEvent(FileEventType.Renamed, e.FullPath, e.OldFullPath);
    }

    private void RaiseFileEvent(FileEventType type, string fullPath, string? oldFullPath)
    {

        if (string.IsNullOrWhiteSpace(fullPath))

            return;


        var evt = new FileEvent(

            Folder.Path,

            type,

            fullPath,

            oldFullPath,

            DateTimeOffset.UtcNow);


        FileEventReceived?.Invoke(this, evt);
    }
}
