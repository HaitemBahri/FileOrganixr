namespace FileOrganixr.Core.Runtime.FileContexts;
public sealed class FileContext
{
    public required string Extension { get; init; }

    public required string FileName { get; init; }

    public required string FileNameWithoutExtension { get; init; }
    public required string FullPath { get; init; }

    public required long? SizeBytes { get; init; }
}
