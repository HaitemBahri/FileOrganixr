


using System;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public sealed class UserApprovalPolicy : IUserApprovalPolicy
{
    public bool RequiresApproval(ActionRequest request)
    {

        ArgumentNullException.ThrowIfNull(request);


        if (request.RuleSnapshot is null)

            return false;


        var approval = request.RuleSnapshot.UserApproval;


        return approval;
    }
}
