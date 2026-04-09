using System;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;

namespace FileOrganixr.Core.Configuration.Validators.ActionValidators;
public sealed class DeleteActionDefinitionValidator : IActionDefinitionValidator
{

    public string SupportedType => "Delete";


    public ValidationResult Validate(IActionDefinition action, string basePath)
    {

        ArgumentNullException.ThrowIfNull(action);


        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path must be provided.", nameof(basePath));
        }


        var result = ValidationResult.Empty();


        if (action is not DeleteActionDefinition)
        {

            result.AddError($"{basePath}.Type", $"Validator '{SupportedType}' received '{action.Type}'.");
            return result;
        }


        return result;
    }
}
