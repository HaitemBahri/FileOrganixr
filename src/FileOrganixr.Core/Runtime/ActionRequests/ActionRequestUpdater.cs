using System;
using System.Collections.Generic;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Runtime.ActionRequests;
public sealed class ActionRequestUpdater : IActionRequestUpdater
{
    private static readonly IReadOnlyDictionary<ActionRequestStatus, HashSet<ActionRequestStatus>> AllowedTransitions =
        new Dictionary<ActionRequestStatus, HashSet<ActionRequestStatus>>
        {
            [ActionRequestStatus.Detected] =
            [
                ActionRequestStatus.NoRuleMatched,
                ActionRequestStatus.RuleMatched
            ],
            [ActionRequestStatus.RuleMatched] =
            [
                ActionRequestStatus.PendingApproval,
                ActionRequestStatus.Queued
            ],
            [ActionRequestStatus.PendingApproval] =
            [
                ActionRequestStatus.Approved,
                ActionRequestStatus.Rejected
            ],
            [ActionRequestStatus.Approved] =
            [
                ActionRequestStatus.Queued
            ],
            [ActionRequestStatus.Queued] =
            [
                ActionRequestStatus.Processing
            ],
            [ActionRequestStatus.Processing] =
            [
                ActionRequestStatus.Completed,
                ActionRequestStatus.Failed
            ]
        };

    public void AddStatus(ActionRequest request, ActionRequestStatus status, string note = "")
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (request)
        {
            ValidateTransition(request, status);

            request.ApprovalRequired = request.RuleSnapshot?.UserApproval ?? false;

            var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

            request.AddStatus(status, normalizedNote);
        }
    }

    private static void ValidateTransition(ActionRequest request, ActionRequestStatus nextStatus)
    {
        if (request.History.Count == 0)
        {
            if (nextStatus != ActionRequestStatus.Detected)
                throw new InvalidOperationException(
                    $"First status must be '{ActionRequestStatus.Detected}', but was '{nextStatus}'.");

            return;
        }

        var current = request.CurrentStatus;

        if (current == nextStatus) return;

        if (IsTerminal(current))
            throw new InvalidOperationException(
                $"Cannot transition from terminal status '{current}' to '{nextStatus}'.");

        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(nextStatus))
            throw new InvalidOperationException($"Invalid ActionRequest transition '{current}' -> '{nextStatus}'.");
    }

    private static bool IsTerminal(ActionRequestStatus status)
    {
        return status is ActionRequestStatus.NoRuleMatched
            or ActionRequestStatus.Rejected
            or ActionRequestStatus.Completed
            or ActionRequestStatus.Failed;
    }
}
