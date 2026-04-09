using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Core.Runtime.FileContexts;
public interface IFileContextFactory
{
    FileContext Create(FileEvent fileEvent);
}
