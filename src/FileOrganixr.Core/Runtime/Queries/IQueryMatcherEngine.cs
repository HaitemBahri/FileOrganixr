using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Runtime.Queries;
public interface IQueryMatcherEngine
{
    bool IsMatch(IQueryDefinition query, FileContext file);
}
