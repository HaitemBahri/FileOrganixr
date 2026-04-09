namespace FileOrganixr.Core.Configuration.Definitions.Actions;
public sealed class MoveActionDefinition : ActionDefinition
{
    public MoveActionDefinition() : base("Move")
    {
    }

    public required string DestinationPath { get; init; }
}
