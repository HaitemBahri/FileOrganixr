using System;

namespace FileOrganixr.Core.Watchers.FolderWatchers;
public sealed record WatcherErrorEvent(
    string Message,
    Exception? Exception,
    DateTimeOffset TimestampUtc
);
