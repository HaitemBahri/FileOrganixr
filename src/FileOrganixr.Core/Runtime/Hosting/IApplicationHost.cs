using System.Threading;
using System.Threading.Tasks;

namespace FileOrganixr.Core.Runtime.Hosting;
public interface IApplicationHost
{
    bool IsRunning { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
