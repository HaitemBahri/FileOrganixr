using System;

namespace FileOrganixr.UI.ViewModels;
public sealed class ApprovalActionMessageEventArgs : EventArgs
{
    public ApprovalActionMessageEventArgs(
        string message,
        NotificationSeverity severity,
        string? details = null)
    {
        Message = message;
        Severity = severity;
        Details = details;
    }

    public string? Details { get; }

    public string Message { get; }

    public NotificationSeverity Severity { get; }
}
