

using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Configuration.Validators.QueryValidator;

public sealed class QueryDefinitionValidatorRegistry : IQueryDefinitionValidatorRegistry
{

    private readonly Dictionary<string, IQueryDefinitionValidator> _validators;


    public QueryDefinitionValidatorRegistry(IEnumerable<IQueryDefinitionValidator> validators)
    {

        ArgumentNullException.ThrowIfNull(validators);


        _validators = new Dictionary<string, IQueryDefinitionValidator>(StringComparer.OrdinalIgnoreCase);


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
                    $"Multiple query validators registered for type '{type}'.");
            }


            _validators[type] = validator;
        }
    }


    public IQueryDefinitionValidator? Resolve(IQueryDefinition query)
    {

        if (query is null)
        {
            return null;
        }

        var type = query.Type;


        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }


        return _validators.TryGetValue(type, out var validator) ? validator : null;
    }
}
