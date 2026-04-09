


using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public interface IActionExecutor
{

    Task ExecuteAsync(ActionRequest request, CancellationToken cancellationToken);
}
