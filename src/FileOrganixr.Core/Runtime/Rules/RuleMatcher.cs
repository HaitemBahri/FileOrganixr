using System;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;
using FileOrganixr.Core.Runtime.Queries;
using FileOrganixr.Core.Runtime.Rules;

namespace FileOrganixr.Core.Runtime.Rules;
public sealed class RuleMatcher : IRuleMatcher
{
    private readonly IQueryMatcherEngine _queryMatcherEngine;

    public RuleMatcher(IQueryMatcherEngine queryMatcherEngine)
    {
        _queryMatcherEngine = queryMatcherEngine;
    }

    public RuleDefinition? FindFirstMatch(FolderDefinition folder, FileContext file)
    {
        ArgumentNullException.ThrowIfNull(folder);

        ArgumentNullException.ThrowIfNull(file);

        if (folder.Rules is null || folder.Rules.Count == 0) return null;

        foreach (var rule in folder.Rules)
        {
            if (rule is null) continue;

            if (rule.Query is null) continue;

            var matches = _queryMatcherEngine.IsMatch(rule.Query, file);

            if (matches) return rule;
        }

        return null;
    }
}
