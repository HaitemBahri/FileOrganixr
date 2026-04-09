using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Configuration;
public sealed class ConfigurationRoot
{
    public int SchemaVersion { get; init; } = 1;

    public List<FolderDefinition> Folders { get; init; } = [];
}
