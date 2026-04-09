using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Configuration.Validators.QueryValidator;
public interface IQueryDefinitionValidator
{
    ValidationResult Validate(IQueryDefinition query, string basePath);

    string SupportedType { get; }

}
