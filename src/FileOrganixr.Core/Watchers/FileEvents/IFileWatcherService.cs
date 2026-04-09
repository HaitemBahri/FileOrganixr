namespace FileOrganixr.Core.Watchers.FileEvents;
public interface IFileWatcherService
{
    void Start();

    void Stop();

    bool IsRunning { get; }
}
