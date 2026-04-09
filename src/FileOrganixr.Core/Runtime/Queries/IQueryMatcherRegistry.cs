


using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Runtime.Queries;
public interface IQueryMatcherRegistry
{

    void Register(IQueryMatcher matcher);


    IQueryMatcher Resolve(IQueryDefinition query);
}
