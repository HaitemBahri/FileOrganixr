using System.Threading;
using System.Threading.Tasks;

namespace FileOrganixr.Core.Watchers.FileEvents;
public interface IFileEventHandler
{
    Task HandleAsync(FileEvent fileEvent, CancellationToken cancellationToken);
}
