namespace FileOrganixr.Core.Configuration.Definitions.Queries;
public abstract class QueryDefinition : IQueryDefinition
{
    public QueryDefinition(string type)
    {
        Type = type;
    }

    public string Type { get; }
}
