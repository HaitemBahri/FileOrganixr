using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Configuration.Validators.RuleValidators;
public interface IRuleDefinitionValidator
{

    ValidationResult Validate(RuleDefinition rule, string basePath);
}
