using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Configuration.Validators.QueryValidator;
public interface IQueryDefinitionValidatorRegistry
{

    IQueryDefinitionValidator? Resolve(IQueryDefinition query);
}
