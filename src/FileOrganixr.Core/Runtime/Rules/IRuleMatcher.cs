using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Runtime.Rules;
public interface IRuleMatcher
{
    RuleDefinition? FindFirstMatch(FolderDefinition folder, FileContext file);
}
