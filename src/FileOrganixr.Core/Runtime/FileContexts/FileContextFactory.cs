using System;
using System.IO;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Core.Runtime.FileContexts;
public sealed class FileContextFactory : IFileContextFactory
{
    public FileContext Create(FileEvent fileEvent)
    {
        if (string.IsNullOrWhiteSpace(fileEvent.FullPath))
            throw new ArgumentException("FileEvent.FullPath must be provided.", nameof(fileEvent));

        var fullPath = fileEvent.FullPath;

        var fileName = Path.GetFileName(fullPath);

        var extension = Path.GetExtension(fullPath);

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);

        return new FileContext
        {
            FullPath = fullPath,
            FileName = fileName,
            FileNameWithoutExtension = fileNameWithoutExtension,
            Extension = extension,
            SizeBytes = TryReadSizeBytes(fullPath)
        };
    }

    private static long? TryReadSizeBytes(string fullPath)
    {
        try
        {
            if (!File.Exists(fullPath)) return null;

            var fileInfo = new FileInfo(fullPath);

            return fileInfo.Length;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
