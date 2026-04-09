using System.Collections.Generic;

namespace FileOrganixr.Core.Configuration.Definitions;
public sealed class FolderDefinition
{
    public required string Name { get; init; }

    public required string Path { get; init; }

    public List<RuleDefinition> Rules { get; init; } = new();
}
