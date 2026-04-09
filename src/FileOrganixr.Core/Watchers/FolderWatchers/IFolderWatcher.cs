using System;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Watchers.FileEvents;

namespace FileOrganixr.Core.Watchers.FolderWatchers;
public interface IFolderWatcher : IDisposable
{

    FolderDefinition Folder { get; }


    event EventHandler<FileEvent> FileEventReceived;


    event EventHandler<WatcherErrorEvent> Error;


    void Start();


    void Stop();
}
