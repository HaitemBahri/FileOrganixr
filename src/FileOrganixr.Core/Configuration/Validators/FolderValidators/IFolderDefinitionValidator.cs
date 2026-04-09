using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Configuration.Validators.FolderValidators;
public interface IFolderDefinitionValidator
{

    ValidationResult Validate(FolderDefinition folder, string basePath);
}
