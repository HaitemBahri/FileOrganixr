


using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public interface IUserApprovalPolicy
{

    bool RequiresApproval(ActionRequest request);
}
