namespace FileOrganixr.Core.Configuration.Definitions;
public sealed class RuleDefinition
{
    public required IActionDefinition Action { get; init; }
    public required string Name { get; init; }

    public required IQueryDefinition Query { get; init; }

    public bool UserApproval { get; init; } = false;
}
