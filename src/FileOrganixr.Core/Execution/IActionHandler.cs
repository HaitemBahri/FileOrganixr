


using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public interface IActionHandler
{

    string SupportedActionType { get; }


    Task ExecuteAsync(ActionRequest request, IActionDefinition actionDefinition, CancellationToken cancellationToken);
}
