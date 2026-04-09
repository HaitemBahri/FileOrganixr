using System;
using System.Collections.Generic;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Runtime.ActionRequests;
public interface IActionRequestStore
{
    event EventHandler<ActionRequest>? Added;

    void Add(ActionRequest request);

    IReadOnlyList<ActionRequest> GetSnapshot();
}
