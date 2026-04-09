namespace FileOrganixr.Core.Configuration.Definitions.Actions;
public abstract class ActionDefinition : IActionDefinition
{
    protected ActionDefinition(string type)
    {
        Type = type;
    }

    public string Type { get; }
}
