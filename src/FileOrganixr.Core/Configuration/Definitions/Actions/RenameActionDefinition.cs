namespace FileOrganixr.Core.Configuration.Definitions.Actions;
public sealed class RenameActionDefinition : ActionDefinition
{
    public RenameActionDefinition() : base("Rename")
    {
    }

    public required string Pattern { get; init; }
}
