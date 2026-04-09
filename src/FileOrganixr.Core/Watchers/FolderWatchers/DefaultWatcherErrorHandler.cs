using System;
using System.Diagnostics;

namespace FileOrganixr.Core.Watchers.FolderWatchers;
public sealed class DefaultWatcherErrorHandler : IWatcherErrorHandler
{
    public void Handle(IFolderWatcher? watcher, WatcherErrorEvent error)
    {
        if (error is null)
        {
            Trace.TraceError("Watcher error handler received a null error event.");
            return;
        }

        var folderPath = watcher?.Folder?.Path;
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            folderPath = "<unknown>";
        }

        if (error.Exception is null)
        {
            Trace.TraceError(
                $"Watcher error at {error.TimestampUtc:O} for '{folderPath}': {error.Message}");
            return;
        }

        Trace.TraceError(
            $"Watcher error at {error.TimestampUtc:O} for '{folderPath}': {error.Message}. Exception: {error.Exception}");
    }
}
