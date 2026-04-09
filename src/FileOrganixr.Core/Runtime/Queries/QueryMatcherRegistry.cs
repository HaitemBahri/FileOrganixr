using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Runtime.Queries;
public sealed class QueryMatcherRegistry : IQueryMatcherRegistry
{
    private readonly Dictionary<Type, IQueryMatcher> _map = new();

    public void Register(IQueryMatcher matcher)
    {
        ArgumentNullException.ThrowIfNull(matcher);

        _map[matcher.SupportedQueryType] = matcher;
    }

    public IQueryMatcher Resolve(IQueryDefinition query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var type = query.GetType();

        if (_map.TryGetValue(type, out var matcher)) return matcher;

        throw new NotSupportedException($"No matcher registered for query type '{type.Name}'.");
    }
}
