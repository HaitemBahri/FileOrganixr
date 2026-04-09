namespace FileOrganixr.Core.Runtime.ActionRequests;
public enum ActionRequestStatus
{
    Detected,

    NoRuleMatched,

    RuleMatched,

    PendingApproval,

    Approved,

    Rejected,

    Queued,

    Processing,

    Completed,

    Failed
}
