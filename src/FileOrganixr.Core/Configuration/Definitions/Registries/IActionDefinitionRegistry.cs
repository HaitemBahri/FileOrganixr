using System.Collections.Generic;

namespace FileOrganixr.Core.Configuration.Definitions.Registries;
public interface IActionDefinitionRegistry
{
    IActionDefinition Create(string type, IReadOnlyDictionary<string, object?> args);
}
