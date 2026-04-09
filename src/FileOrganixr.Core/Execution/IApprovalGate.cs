


using System;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public interface IApprovalGate
{

    void RegisterPending(ActionRequest request);


    ActionRequest? Approve(Guid requestId);


    ActionRequest? Reject(Guid requestId);
}
