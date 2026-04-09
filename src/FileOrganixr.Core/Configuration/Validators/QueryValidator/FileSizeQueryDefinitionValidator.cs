using System;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Configuration.Validators.QueryValidator;

namespace FileOrganixr.Core.Configuration.Validators.QueryValidator;
public sealed class FileSizeQueryDefinitionValidator : IQueryDefinitionValidator
{

    public string SupportedType => "FileSize";


    public ValidationResult Validate(IQueryDefinition query, string basePath)
    {

        ArgumentNullException.ThrowIfNull(query);


        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path must be provided.", nameof(basePath));
        }


        var result = ValidationResult.Empty();


        if (query is not FileSizeQueryDefinition size)
        {

            result.AddError($"{basePath}.Type", $"Validator '{SupportedType}' received '{query.GetType().Name}'.");
            return result;
        }


        if (size.MinSize < 0)
        {

            result.AddError($"{basePath}.MinSize", "MinSize cannot be negative.");
        }


        if (size.MaxSize < 0)
        {

            result.AddError($"{basePath}.MaxSize", "MaxSize cannot be negative.");
        }


        if (size.MinSize > size.MaxSize)
        {

            result.AddError($"{basePath}", "MinSize cannot be greater than MaxSize.");
        }


        return result;
    }
}
