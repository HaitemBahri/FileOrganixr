using System;
using System.Linq;
using System.Windows.Threading;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.UI.ViewModels;
public sealed class MainViewModel : ViewModelBase
{
    private readonly Dispatcher _dispatcher;
    private readonly IActionRequestStore _requestStore;
    private readonly IActionRequestUpdater _requestUpdater;

    public MainViewModel(
        RequestsViewModel requests,
        RequestDetailsViewModel requestDetails,
        ApprovalActionsViewModel approvalActions,
        RuntimeStateViewModel runtimeState,
        SettingsPanelViewModel settingsPanel,
        NotificationBannerViewModel notification,
        IActionRequestStore requestStore,
        IActionRequestUpdater requestUpdater,
        Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(requestDetails);
        ArgumentNullException.ThrowIfNull(approvalActions);
        ArgumentNullException.ThrowIfNull(runtimeState);
        ArgumentNullException.ThrowIfNull(settingsPanel);
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(requestStore);
        ArgumentNullException.ThrowIfNull(requestUpdater);
        ArgumentNullException.ThrowIfNull(dispatcher);

        Requests = requests;
        RequestDetails = requestDetails;
        ApprovalActions = approvalActions;
        RuntimeState = runtimeState;
        SettingsPanel = settingsPanel;
        Notification = notification;
        _requestStore = requestStore;
        _requestUpdater = requestUpdater;
        _dispatcher = dispatcher;

        Requests.SelectedItemChanged += OnSelectedRequestChanged;
        ApprovalActions.RequestUpdated += OnApprovalRequestUpdated;
        ApprovalActions.ActionMessageRaised += OnApprovalActionMessageRaised;
        SettingsPanel.RefreshFailed += OnSettingsPanelRefreshFailed;
        _requestStore.Added += OnRequestAdded;
        _requestUpdater.StatusChanged += OnRequestStatusChanged;

        LoadInitialRequests();
    }

    public ApprovalActionsViewModel ApprovalActions { get; }

    public RequestDetailsViewModel RequestDetails { get; }

    public RequestsViewModel Requests { get; }
    public SettingsPanelViewModel SettingsPanel { get; }
    public NotificationBannerViewModel Notification { get; }
    public RuntimeStateViewModel RuntimeState { get; }

    private void LoadInitialRequests()
    {
        var snapshot = _requestStore.GetSnapshot();
        DispatchToUi(() =>
        {
            Requests.ReplaceItems(snapshot);

            if (Requests.SelectedItem is null)
            {
                Requests.SelectedItem = Requests.Items.FirstOrDefault();
            }

            RequestDetails.BindTo(Requests.SelectedItem);
            RuntimeState.UpdateFromRequests(Requests.Items);
        });
    }

    private void OnRequestAdded(object? sender, ActionRequest request)
    {
        DispatchToUi(() =>
        {
            ApplyRequestUpdate(request, ensureSelectionWhenEmpty: true);
        });
    }

    private void OnApprovalRequestUpdated(object? sender, ActionRequest updatedRequest)
    {
        DispatchToUi(() =>
        {
            ApplyRequestUpdate(updatedRequest, ensureSelectionWhenEmpty: false);
        });
    }

    private void OnRequestStatusChanged(object? sender, ActionRequest updatedRequest)
    {
        DispatchToUi(() =>
        {
            if (!IsPersisted(updatedRequest.Id))
            {
                return;
            }

            ApplyRequestUpdate(updatedRequest, ensureSelectionWhenEmpty: false);
        });
    }

    private void OnSelectedRequestChanged(object? sender, ActionRequestItemViewModel? selectedRequest)
    {
        RequestDetails.BindTo(selectedRequest);
        ApprovalActions.SetSelectedRequest(selectedRequest);
    }

    private void OnApprovalActionMessageRaised(object? sender, ApprovalActionMessageEventArgs message)
    {
        DispatchToUi(() => { Notification.Show(message.Message, message.Severity, message.Details); });
    }

    private void OnSettingsPanelRefreshFailed(string message)
    {
        DispatchToUi(() => { Notification.Show("Settings refresh failed.", NotificationSeverity.Warning, message); });
    }

    private void ApplyRequestUpdate(ActionRequest request, bool ensureSelectionWhenEmpty)
    {
        Requests.Upsert(request);

        if (ensureSelectionWhenEmpty && Requests.SelectedItem is null)
        {
            Requests.SelectedItem = Requests.Items.FirstOrDefault();
        }

        RequestDetails.BindTo(Requests.SelectedItem);

        var updated = Requests.FindById(request.Id);
        if (updated is not null)
        {
            RuntimeState.RecordRequestEvent(updated);
            if (updated.CurrentStatus == ActionRequestStatus.Failed)
            {
                Notification.Show(
                    $"Request failed: {updated.FileName}",
                    NotificationSeverity.Error,
                    updated.LatestNote);
            }
        }

        RuntimeState.UpdateFromRequests(Requests.Items);
    }

    private bool IsPersisted(Guid requestId)
    {
        return _requestStore.GetSnapshot().Any(request => request.Id == requestId);
    }

    private void DispatchToUi(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.Invoke(action);
    }
}
