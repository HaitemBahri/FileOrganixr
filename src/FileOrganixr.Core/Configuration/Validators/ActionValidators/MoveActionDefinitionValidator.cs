

using System;
using System.IO;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;

namespace FileOrganixr.Core.Configuration.Validators.ActionValidators;

public sealed class MoveActionDefinitionValidator : IActionDefinitionValidator
{

    public string SupportedType => "Move";


    public ValidationResult Validate(IActionDefinition action, string basePath)
    {

        ArgumentNullException.ThrowIfNull(action);


        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path must be provided.", nameof(basePath));
        }


        var result = ValidationResult.Empty();


        if (action is not MoveActionDefinition move)
        {

            result.AddError($"{basePath}.Type", $"Validator '{SupportedType}' received '{action.Type}'.");
            return result;
        }


        if (string.IsNullOrWhiteSpace(move.DestinationPath))
        {

            result.AddError($"{basePath}.DestinationPath", "DestinationPath is required for Move.");
            return result;
        }


        if (move.DestinationPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {

            result.AddError($"{basePath}.DestinationPath", "DestinationPath contains invalid path characters.");
        }


        return result;
    }
}
