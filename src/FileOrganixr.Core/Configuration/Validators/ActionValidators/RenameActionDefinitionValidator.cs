using System;
using System.IO;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;

namespace FileOrganixr.Core.Configuration.Validators.ActionValidators;
public sealed class RenameActionDefinitionValidator : IActionDefinitionValidator
{

    public string SupportedType => "Rename";


    public ValidationResult Validate(IActionDefinition action, string basePath)
    {

        ArgumentNullException.ThrowIfNull(action);


        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path must be provided.", nameof(basePath));
        }


        var result = ValidationResult.Empty();


        if (action is not RenameActionDefinition rename)
        {

            result.AddError($"{basePath}.Type", $"Validator '{SupportedType}' received '{action.Type}'.");
            return result;
        }


        if (string.IsNullOrWhiteSpace(rename.Pattern))
        {

            result.AddError($"{basePath}.Pattern", "Pattern is required for Rename.");
            return result;
        }


        if (rename.Pattern.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {

            result.AddError($"{basePath}.Pattern", "Pattern contains invalid file name characters.");
        }


        return result;
    }
}
