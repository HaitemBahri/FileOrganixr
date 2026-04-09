using System;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Runtime.Queries;
public sealed class FileSizeQueryMatcher : IQueryMatcher
{
    public Type SupportedQueryType => typeof(FileSizeQueryDefinition);

    public bool IsMatch(IQueryDefinition query, FileContext file)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(file);

        if (query is not FileSizeQueryDefinition typedQuery)
            throw new ArgumentException(
                $"Expected query type '{nameof(FileSizeQueryDefinition)}' but got '{query.GetType().Name}'.",
                nameof(query));

        if (file.SizeBytes is null) return false;

        var fileSize = (decimal)file.SizeBytes.Value;

        return fileSize >= typedQuery.MinSize && fileSize <= typedQuery.MaxSize;
    }
}
