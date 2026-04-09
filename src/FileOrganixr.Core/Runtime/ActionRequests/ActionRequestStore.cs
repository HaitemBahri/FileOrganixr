using System;
using System.Collections.Generic;
using System.Linq;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Runtime.ActionRequests;
public sealed class ActionRequestStore : IActionRequestStore
{
    private readonly object _gate = new();

    private readonly List<ActionRequest> _items = [];

    public event EventHandler<ActionRequest>? Added;

    public void Add(ActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            _items.Add(request);
        }

        Added?.Invoke(this, request);
    }

    public IReadOnlyList<ActionRequest> GetSnapshot()
    {
        lock (_gate)
        {
            return _items.ToList();
        }
    }
}
