using System;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public sealed class ApprovalWorkflow : IApprovalWorkflow
{
    private readonly IApprovalGate _approvalGate;
    private readonly IExecutionOrchestrator _executionOrchestrator;

    public ApprovalWorkflow(IApprovalGate approvalGate, IExecutionOrchestrator executionOrchestrator)
    {
        ArgumentNullException.ThrowIfNull(approvalGate);
        ArgumentNullException.ThrowIfNull(executionOrchestrator);

        _approvalGate = approvalGate;
        _executionOrchestrator = executionOrchestrator;
    }

    public ActionRequest? Approve(Guid requestId)
    {
        var request = _approvalGate.Approve(requestId);
        if (request is null) return null;

        _executionOrchestrator.OnApprovalOutcome(request);
        return request;
    }

    public ActionRequest? Reject(Guid requestId)
    {
        return _approvalGate.Reject(requestId);
    }
}
