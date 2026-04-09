using System;

namespace FileOrganixr.Core.Watchers.FileEvents;
public sealed record FileEvent(
    string WatchedFolderPath,
    FileEventType Type,
    string FullPath,
    string? OldFullPath,
    DateTimeOffset TimestampUtc
);
