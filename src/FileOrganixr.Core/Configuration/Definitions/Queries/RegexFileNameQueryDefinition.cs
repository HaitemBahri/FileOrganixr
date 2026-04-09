namespace FileOrganixr.Core.Configuration.Definitions.Queries;
public sealed class RegexFileNameQueryDefinition : QueryDefinition
{
    public RegexFileNameQueryDefinition() : base("RegexFileName")
    {
    }

    public bool IgnoreCase { get; init; } = false;
    public required string Pattern { get; init; }
}
