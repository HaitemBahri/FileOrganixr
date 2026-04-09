using System;
using System.Windows.Input;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.UI.Commands;

namespace FileOrganixr.UI.ViewModels;
public sealed class ApprovalActionsViewModel : ViewModelBase
{
    private readonly DelegateCommand _approveCommand;
    private readonly IApprovalWorkflow _approvalWorkflow;
    private readonly DelegateCommand _rejectCommand;
    private string _lastActionMessage = string.Empty;
    private ActionRequestItemViewModel? _selectedRequest;

    public ApprovalActionsViewModel(IApprovalWorkflow approvalWorkflow)
    {
        ArgumentNullException.ThrowIfNull(approvalWorkflow);

        _approvalWorkflow = approvalWorkflow;

        _approveCommand = new DelegateCommand(
            execute: () => ApplyDecision(_approvalWorkflow.Approve, "approved"),
            canExecute: CanRunDecision);

        _rejectCommand = new DelegateCommand(
            execute: () => ApplyDecision(_approvalWorkflow.Reject, "rejected"),
            canExecute: CanRunDecision);
    }

    public event EventHandler<ActionRequest>? RequestUpdated;
    public event EventHandler<ApprovalActionMessageEventArgs>? ActionMessageRaised;

    public ICommand ApproveCommand => _approveCommand;

    public bool CanTakeAction => _selectedRequest?.IsPendingApproval == true;

    public string LastActionMessage
    {
        get => _lastActionMessage;
        private set => SetProperty(ref _lastActionMessage, value);
    }

    public ICommand RejectCommand => _rejectCommand;

    public void SetSelectedRequest(ActionRequestItemViewModel? selectedRequest)
    {
        _selectedRequest = selectedRequest;
        OnPropertyChanged(nameof(CanTakeAction));
        RaiseCommandsCanExecuteChanged();
    }

    private void ApplyDecision(Func<Guid, ActionRequest?> action, string decisionLabel)
    {
        if (_selectedRequest is null)
        {
            PublishMessage("Select a pending request first.", NotificationSeverity.Warning);
            return;
        }

        if (!_selectedRequest.IsPendingApproval)
        {
            PublishMessage("Only requests in PendingApproval can be actioned.", NotificationSeverity.Warning);
            return;
        }

        try
        {
            var updated = action(_selectedRequest.Id);
            if (updated is null)
            {
                PublishMessage("Request was not found.", NotificationSeverity.Warning);
                return;
            }

            _selectedRequest.UpdateFrom(updated);
            PublishMessage($"Request {updated.Id} {decisionLabel}.", NotificationSeverity.Info);
            RequestUpdated?.Invoke(this, updated);
        }
        catch (Exception ex)
        {
            PublishMessage($"Approval action failed: {ex.Message}", NotificationSeverity.Error, ex.ToString());
        }
        finally
        {
            OnPropertyChanged(nameof(CanTakeAction));
            RaiseCommandsCanExecuteChanged();
        }
    }

    private bool CanRunDecision()
    {
        return _selectedRequest?.IsPendingApproval == true;
    }

    private void RaiseCommandsCanExecuteChanged()
    {
        _approveCommand.RaiseCanExecuteChanged();
        _rejectCommand.RaiseCanExecuteChanged();
    }

    private void PublishMessage(string message, NotificationSeverity severity, string? details = null)
    {
        LastActionMessage = message;
        ActionMessageRaised?.Invoke(this, new ApprovalActionMessageEventArgs(message, severity, details));
    }
}
