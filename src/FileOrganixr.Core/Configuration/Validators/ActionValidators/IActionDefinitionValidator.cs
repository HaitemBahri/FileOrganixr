
using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Configuration.Validators.ActionValidators;
public interface IActionDefinitionValidator
{
    ValidationResult Validate(IActionDefinition action, string basePath);

    string SupportedType { get; }

}
