


namespace FileOrganixr.Core.Watchers.FolderWatchers;
public interface IWatcherErrorHandler
{

    void Handle(IFolderWatcher? watcher, WatcherErrorEvent error);
}
