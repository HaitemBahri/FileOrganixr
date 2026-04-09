using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Runtime.ActionRequests;
public interface IActionRequestUpdater
{
    void AddStatus(ActionRequest request, ActionRequestStatus status, string note = "");
}
