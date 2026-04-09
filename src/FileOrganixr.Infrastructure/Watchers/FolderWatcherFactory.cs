


using System;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Infrastructure.Watchers;
public sealed class FolderWatcherFactory : IFolderWatcherFactory
{
    public IFolderWatcher Create(FolderDefinition folder)
    {

        ArgumentNullException.ThrowIfNull(folder);


        return new FolderWatcher(folder);
    }
}
