

using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Configuration.Validators.ActionValidators;

public sealed class ActionDefinitionValidatorRegistry : IActionDefinitionValidatorRegistry
{

    private readonly Dictionary<string, IActionDefinitionValidator> _validators;


    public ActionDefinitionValidatorRegistry(IEnumerable<IActionDefinitionValidator> validators)
    {

        ArgumentNullException.ThrowIfNull(validators);


        _validators = new Dictionary<string, IActionDefinitionValidator>(StringComparer.OrdinalIgnoreCase);


        foreach (var validator in validators)
        {

            if (validator is null)
            {
                continue;
            }


            var type = validator.SupportedType;


            if (string.IsNullOrWhiteSpace(type))
            {
                throw new InvalidOperationException(
                    $"Validator '{validator.GetType().Name}' has an empty SupportedType.");
            }


            if (_validators.ContainsKey(type))
            {
                throw new InvalidOperationException(
                    $"Multiple action validators registered for type '{type}'.");
            }


            _validators[type] = validator;
        }
    }


    public IActionDefinitionValidator? Resolve(string actionType)
    {

        if (string.IsNullOrWhiteSpace(actionType))
        {
            return null;
        }


        return _validators.TryGetValue(actionType, out var validator) ? validator : null;
    }
}


