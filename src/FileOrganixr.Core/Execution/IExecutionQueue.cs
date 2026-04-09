


using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public interface IExecutionQueue
{

    void Enqueue(ActionRequest request);


    Task<ActionRequest> DequeueAsync(CancellationToken cancellationToken);
}
