using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Runtime.ActionRequests;
public sealed class ActionRequest
{
    public bool ApprovalRequired { get; set; }

    public ActionRequestStatus CurrentStatus { get; private set; }

    public required FileContext File { get; init; }

    public required FolderDefinition Folder { get; init; }

    public List<ActionStatusEntry> History { get; } = [];
    public required Guid Id { get; init; }

    public RuleDefinition? RuleSnapshot { get; set; }

    public void AddStatus(ActionRequestStatus status, string? note = null)
    {
        History.Add(new ActionStatusEntry(status, DateTimeOffset.UtcNow, note));

        CurrentStatus = status;
    }
}
