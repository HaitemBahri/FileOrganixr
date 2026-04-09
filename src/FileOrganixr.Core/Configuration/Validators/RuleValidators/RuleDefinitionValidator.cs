

using System;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Validators.ActionValidators;
using FileOrganixr.Core.Configuration.Validators.QueryValidator;

namespace FileOrganixr.Core.Configuration.Validators.RuleValidators;

public sealed class RuleDefinitionValidator : IRuleDefinitionValidator
{

    private readonly IActionDefinitionValidatorRegistry _actionValidatorRegistry;


    private readonly IQueryDefinitionValidatorRegistry _queryValidatorRegistry;


    public RuleDefinitionValidator(
        IActionDefinitionValidatorRegistry actionValidatorRegistry,
        IQueryDefinitionValidatorRegistry queryValidatorRegistry)
    {

        ArgumentNullException.ThrowIfNull(actionValidatorRegistry);
        ArgumentNullException.ThrowIfNull(queryValidatorRegistry);


        _actionValidatorRegistry = actionValidatorRegistry;
        _queryValidatorRegistry = queryValidatorRegistry;
    }


    public ValidationResult Validate(RuleDefinition rule, string basePath)
    {

        ArgumentNullException.ThrowIfNull(rule);

        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path must be provided.", nameof(basePath));
        }


        var result = ValidationResult.Empty();


        if (string.IsNullOrWhiteSpace(rule.Name))
        {
            result.AddError($"{basePath}.Name", "Rule name is required.");
        }


        if (rule.Action is null)
        {
            result.AddError($"{basePath}.Action", "Action definition is required.");
        }
        else
        {

            var actionValidator = _actionValidatorRegistry.Resolve(rule.Action.Type);

            if (actionValidator is null)
            {
                result.AddError(
                    $"{basePath}.Action.Type",
                    $"No validator registered for action type '{rule.Action.Type}'.");
            }
            else
            {

                var actionResult = actionValidator.Validate(rule.Action, $"{basePath}.Action");

                result.Merge(actionResult);
            }
        }


        if (rule.Query is null)
        {
            result.AddError($"{basePath}.Query", "Query definition is required.");
        }
        else
        {

            var queryValidator = _queryValidatorRegistry.Resolve(rule.Query);

            if (queryValidator is null)
            {
                result.AddError(
                    $"{basePath}.Query",
                    $"No validator registered for query type '{rule.Query.GetType().Name}'.");
            }
            else
            {

                var queryResult = queryValidator.Validate(rule.Query, $"{basePath}.Query");

                result.Merge(queryResult);
            }
        }

        return result;
    }
}








