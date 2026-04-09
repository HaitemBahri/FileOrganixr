using System;
using System.IO;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Tests.Runtime;
public sealed class FileContextFactoryTests
{
    [Fact]
    public void Create_SetsSizeBytes_WhenFileExists()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllBytes(path, [1, 2, 3, 4, 5]);

            var fileEvent = new FileEvent(
                WatchedFolderPath: Path.GetDirectoryName(path) ?? string.Empty,
                Type: FileEventType.Created,
                FullPath: path,
                OldFullPath: null,
                TimestampUtc: DateTimeOffset.UtcNow);

            var sut = new FileContextFactory();

            var context = sut.Create(fileEvent);

            Assert.Equal(5, context.SizeBytes);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Create_SetsSizeBytesToNull_WhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.tmp");

        var fileEvent = new FileEvent(
            WatchedFolderPath: Path.GetDirectoryName(path) ?? string.Empty,
            Type: FileEventType.Created,
            FullPath: path,
            OldFullPath: null,
            TimestampUtc: DateTimeOffset.UtcNow);

        var sut = new FileContextFactory();

        var context = sut.Create(fileEvent);

        Assert.Null(context.SizeBytes);
    }
}
