using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public interface IExecutionOrchestrator
{
    void Start();

    void Stop();

    void OnRuleMatched(ActionRequest request);

    void OnApprovalOutcome(ActionRequest request);
}
