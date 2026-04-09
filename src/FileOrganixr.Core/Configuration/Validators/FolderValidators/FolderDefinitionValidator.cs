

using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Validators.RuleValidators;

namespace FileOrganixr.Core.Configuration.Validators.FolderValidators;

public sealed class FolderDefinitionValidator : IFolderDefinitionValidator
{

    private readonly IRuleDefinitionValidator _ruleValidator;


    public FolderDefinitionValidator(IRuleDefinitionValidator ruleValidator)
    {

        ArgumentNullException.ThrowIfNull(ruleValidator);


        _ruleValidator = ruleValidator;
    }


    public ValidationResult Validate(FolderDefinition folder, string basePath)
    {

        ArgumentNullException.ThrowIfNull(folder);


        if (string.IsNullOrWhiteSpace(basePath))
        {

            throw new ArgumentException("Base path must be provided.", nameof(basePath));
        }


        var result = ValidationResult.Empty();


        if (string.IsNullOrWhiteSpace(folder.Name))
        {

            result.AddError($"{basePath}.Name", "Folder name is required.");
        }


        if (string.IsNullOrWhiteSpace(folder.Path))
        {

            result.AddError($"{basePath}.Path", "Folder path is required.");
        }


        if (folder.Rules is null)
        {

            result.AddError($"{basePath}.Rules", "Rules collection is missing.");


            return result;
        }


        if (folder.Rules.Count == 0)
        {

            result.AddWarning($"{basePath}.Rules", "No rules configured for this folder.");
        }


        var ruleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        for (var i = 0; i < folder.Rules.Count; i++)
        {

            var rule = folder.Rules[i];


            var rulePath = $"{basePath}.Rules[{i}]";


            if (rule is null)
            {

                result.AddError(rulePath, "Rule entry is null.");


                continue;
            }


            if (!string.IsNullOrWhiteSpace(rule.Name))
            {

                if (!ruleNames.Add(rule.Name))
                {

                    result.AddError($"{rulePath}.Name", $"Duplicate rule name '{rule.Name}' within the same folder.");
                }
            }


            var ruleResult = _ruleValidator.Validate(rule, rulePath);


            result.Merge(ruleResult);
        }


        return result;
    }
}


