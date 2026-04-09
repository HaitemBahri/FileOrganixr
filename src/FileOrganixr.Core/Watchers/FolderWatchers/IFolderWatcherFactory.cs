using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Watchers.FolderWatchers;
public interface IFolderWatcherFactory
{
    IFolderWatcher Create(FolderDefinition folder);
}
