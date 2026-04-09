


using System;
using System.Collections.Generic;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Execution;
public sealed class ApprovalGate : IApprovalGate
{

    private readonly object _gate = new();


    private readonly Dictionary<Guid, ActionRequest> _pending = new();


    private readonly IActionRequestUpdater _requestUpdater;

    public ApprovalGate(IActionRequestUpdater requestUpdater)
    {

        _requestUpdater = requestUpdater;
    }

    public ActionRequest? Approve(Guid requestId)
    {

        ActionRequest? request;
        lock (_gate)
        {

            if (!_pending.TryGetValue(requestId, out request))

                return null;


            _pending.Remove(requestId);
        }


        _requestUpdater.AddStatus(request, ActionRequestStatus.Approved);


        return request;
    }

    public void RegisterPending(ActionRequest request)
    {

        ArgumentNullException.ThrowIfNull(request);


        lock (_gate)
        {

            _pending[request.Id] = request;
        }
    }

    public ActionRequest? Reject(Guid requestId)
    {

        ActionRequest? request;
        lock (_gate)
        {

            if (!_pending.TryGetValue(requestId, out request))

                return null;


            _pending.Remove(requestId);
        }


        _requestUpdater.AddStatus(request, ActionRequestStatus.Rejected);


        return request;
    }
}
