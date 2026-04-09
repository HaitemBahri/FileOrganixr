using System;
using System.Text.RegularExpressions;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Runtime.Queries;
public sealed class RegexFileNameQueryMatcher : IQueryMatcher
{
    public Type SupportedQueryType => typeof(RegexFileNameQueryDefinition);

    public bool IsMatch(IQueryDefinition query, FileContext file)
    {
        if (query is not RegexFileNameQueryDefinition typedQuery)
            throw new ArgumentException(
                $"Expected query type '{nameof(RegexFileNameQueryDefinition)}' but got '{query.GetType().Name}'.",
                nameof(query));

        if (string.IsNullOrWhiteSpace(typedQuery.Pattern) || string.IsNullOrWhiteSpace(file.FileName)) return false;

        var options = typedQuery.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

        var regex = new Regex(typedQuery.Pattern, options);

        return regex.IsMatch(file.FileName);
    }
}
