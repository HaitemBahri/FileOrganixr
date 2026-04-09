using System;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Runtime.Queries;
public interface IQueryMatcher
{
    bool IsMatch(IQueryDefinition query, FileContext file);

    Type SupportedQueryType { get; }
}
