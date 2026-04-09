

using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Validators.FolderValidators;

namespace FileOrganixr.Core.Configuration.Validators;

public sealed class ConfigurationRootValidator : IConfigurationRootValidator
{
    private const int SupportedSchemaVersion = 1;


    private readonly IFolderDefinitionValidator _folderValidator;


    public ConfigurationRootValidator(IFolderDefinitionValidator folderValidator)
    {

        ArgumentNullException.ThrowIfNull(folderValidator);


        _folderValidator = folderValidator;
    }


    public ValidationResult Validate(ConfigurationRoot root)
    {

        ArgumentNullException.ThrowIfNull(root);


        var result = ValidationResult.Empty();

        if (root.SchemaVersion != SupportedSchemaVersion)
        {
            result.AddError(
                nameof(root.SchemaVersion),
                $"Unsupported schemaVersion '{root.SchemaVersion}'. Supported version is '{SupportedSchemaVersion}'.");
        }


        if (root.Folders is null)
        {

            result.AddError("Folders", "Folders collection is missing.");


            return result;
        }


        if (root.Folders.Count == 0)
        {

            result.AddError("Folders", "At least one folder must be configured.");
        }


        var folderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        var folderPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        for (var i = 0; i < root.Folders.Count; i++)
        {

            var folder = root.Folders[i];


            var folderPath = $"Folders[{i}]";


            if (folder is null)
            {

                result.AddError(folderPath, "Folder entry is null.");


                continue;
            }


            if (!string.IsNullOrWhiteSpace(folder.Name))
            {

                if (!folderNames.Add(folder.Name))
                {

                    result.AddError($"{folderPath}.Name", $"Duplicate folder name '{folder.Name}'.");
                }
            }


            if (!string.IsNullOrWhiteSpace(folder.Path))
            {

                if (!folderPaths.Add(folder.Path))
                {

                    result.AddError($"{folderPath}.Path", $"Duplicate folder path '{folder.Path}'.");
                }
            }


            var folderResult = _folderValidator.Validate(folder, folderPath);


            result.Merge(folderResult);
        }


        return result;
    }
}
