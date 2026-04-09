using System;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Runtime.Queries;
public sealed class QueryMatcherEngine : IQueryMatcherEngine
{
    private readonly IQueryMatcherRegistry _registry;

    public QueryMatcherEngine(IQueryMatcherRegistry registry)
    {
        _registry = registry;
    }

    public bool IsMatch(IQueryDefinition query, FileContext file)
    {
        ArgumentNullException.ThrowIfNull(query);

        ArgumentNullException.ThrowIfNull(file);

        var matcher = _registry.Resolve(query);

        return matcher.IsMatch(query, file);
    }
}
