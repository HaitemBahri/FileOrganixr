using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Runtime.Queries;

namespace FileOrganixr.Tests.Runtime.Queries;
public sealed class QueryMatcherRegistryTests
{
    [Fact]
    public void Resolve_ReturnsFileSizeMatcher_WhenFileSizeQueryIsProvided()
    {
        var registry = new QueryMatcherRegistry();
        registry.Register(new FileSizeQueryMatcher());

        var query = new FileSizeQueryDefinition
        {
            MinSize = 0m,
            MaxSize = 100m
        };

        var matcher = registry.Resolve(query);

        Assert.IsType<FileSizeQueryMatcher>(matcher);
    }
}
