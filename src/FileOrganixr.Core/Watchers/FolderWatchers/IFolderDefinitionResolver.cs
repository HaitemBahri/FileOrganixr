using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Watchers.FolderWatchers;
public interface IFolderDefinitionResolver
{
    FolderDefinition Resolve(string watchedFolderPath);
}
