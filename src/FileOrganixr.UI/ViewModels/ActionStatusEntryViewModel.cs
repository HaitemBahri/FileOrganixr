using System;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.UI.ViewModels;
public sealed class ActionStatusEntryViewModel
{
    public ActionStatusEntryViewModel(ActionStatusEntry statusEntry)
    {
        ArgumentNullException.ThrowIfNull(statusEntry);

        Status = statusEntry.Status;
        TimestampUtc = statusEntry.TimestampUtc;
        Note = statusEntry.Note;
    }

    public string? Note { get; }

    public ActionRequestStatus Status { get; }

    public DateTimeOffset TimestampUtc { get; }
}
