using System;
using System.Text.RegularExpressions;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Configuration.Validators.QueryValidator;

namespace FileOrganixr.Core.Configuration.Validators.QueryValidator;
public sealed class RegexFileNameQueryDefinitionValidator : IQueryDefinitionValidator
{

    public string SupportedType => "RegexFileName";


    public ValidationResult Validate(IQueryDefinition query, string basePath)
    {

        ArgumentNullException.ThrowIfNull(query);


        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path must be provided.", nameof(basePath));
        }


        var result = ValidationResult.Empty();


        if (query is not RegexFileNameQueryDefinition regex)
        {

            result.AddError($"{basePath}.Type", $"Validator '{SupportedType}' received '{query.GetType().Name}'.");
            return result;
        }


        if (string.IsNullOrWhiteSpace(regex.Pattern))
        {

            result.AddError($"{basePath}.Pattern", "Pattern is required for RegexFileName query.");
            return result;
        }


        try
        {

            var options = regex.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;


            _ = new Regex(regex.Pattern, options);
        }
        catch (ArgumentException ex)
        {

            result.AddError($"{basePath}.Pattern", $"Invalid regex pattern: {ex.Message}");
        }


        return result;
    }
}
