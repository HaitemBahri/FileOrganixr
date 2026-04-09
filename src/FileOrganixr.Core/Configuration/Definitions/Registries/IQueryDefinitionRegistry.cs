using System.Collections.Generic;

namespace FileOrganixr.Core.Configuration.Definitions.Registries;
public interface IQueryDefinitionRegistry
{
    IQueryDefinition Create(string type, IReadOnlyDictionary<string, object?> args);
}
