namespace FileOrganixr.Core.Configuration.Validators;
public interface IConfigurationRootValidator
{

    ValidationResult Validate(ConfigurationRoot root);
}
