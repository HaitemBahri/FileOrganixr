using System;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public interface IApprovalWorkflow
{
    ActionRequest? Approve(Guid requestId);

    ActionRequest? Reject(Guid requestId);
}
