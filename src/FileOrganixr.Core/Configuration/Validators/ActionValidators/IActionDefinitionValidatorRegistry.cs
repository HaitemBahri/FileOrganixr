namespace FileOrganixr.Core.Configuration.Validators.ActionValidators;
public interface IActionDefinitionValidatorRegistry
{

    IActionDefinitionValidator? Resolve(string actionType);
}
