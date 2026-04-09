namespace FileOrganixr.Core.Watchers.FileEvents;
public interface IFileEventDispatcher
{
    void Dispatch(FileEvent fileEvent);
}
