using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Runtime.Rules;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;
using FileOrganixr.Core.Runtime.Queries;

namespace FileOrganixr.Tests.Runtime.Rules;
public sealed class RuleMatcherTests
{
    [Fact]
    public void FindFirstMatch_ReturnsFirstMatchingRule_AndStopsFurtherEvaluation()
    {
        var firstQuery = new RegexFileNameQueryDefinition { Pattern = @"\.txt$" };
        var secondQuery = new RegexFileNameQueryDefinition { Pattern = @"\.log$" };
        var thirdQuery = new RegexFileNameQueryDefinition { Pattern = @"\.json$" };
        var matcherEngine = new RecordingQueryMatcherEngine(secondQuery, thirdQuery);
        var sut = new RuleMatcher(matcherEngine);
        var folder = new FolderDefinition
        {
            Name = "Inbox",
            Path = @"C:\Inbox",
            Rules =
            [
                CreateRule("Rule1", firstQuery),
                CreateRule("Rule2", secondQuery),
                CreateRule("Rule3", thirdQuery)
            ]
        };

        var result = sut.FindFirstMatch(folder, CreateFileContext("file.log"));

        Assert.NotNull(result);
        Assert.Equal("Rule2", result.Name);
        Assert.Equal([firstQuery, secondQuery], matcherEngine.SeenQueries);
    }

    [Fact]
    public void FindFirstMatch_IgnoresNullRulesAndNullQueries_AndReturnsNullWhenNoMatch()
    {
        var query = new RegexFileNameQueryDefinition { Pattern = @"\.txt$" };
        var matcherEngine = new RecordingQueryMatcherEngine();
        var sut = new RuleMatcher(matcherEngine);
        var folder = new FolderDefinition
        {
            Name = "Inbox",
            Path = @"C:\Inbox",
            Rules =
            [
                null!,
                CreateRule("RuleWithNullQuery", null),
                CreateRule("RuleWithQuery", query)
            ]
        };

        var result = sut.FindFirstMatch(folder, CreateFileContext("file.log"));

        Assert.Null(result);
        Assert.Equal([query], matcherEngine.SeenQueries);
    }

    private static RuleDefinition CreateRule(string name, IQueryDefinition? query)
    {
        return new RuleDefinition
        {
            Name = name,
            Action = new DeleteActionDefinition(),
            Query = query!
        };
    }

    private static FileContext CreateFileContext(string fileName)
    {
        return new FileContext
        {
            FullPath = $@"C:\Inbox\{fileName}",
            FileName = fileName,
            FileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName),
            Extension = Path.GetExtension(fileName),
            SizeBytes = 64
        };
    }

    private sealed class RecordingQueryMatcherEngine : IQueryMatcherEngine
    {
        private readonly HashSet<IQueryDefinition> _matchingQueries;

        public RecordingQueryMatcherEngine(params IQueryDefinition[] matchingQueries)
        {
            _matchingQueries = [.. matchingQueries];
        }

        public List<IQueryDefinition> SeenQueries { get; } = [];

        public bool IsMatch(IQueryDefinition query, FileContext file)
        {
            SeenQueries.Add(query);
            return _matchingQueries.Contains(query);
        }
    }
}
