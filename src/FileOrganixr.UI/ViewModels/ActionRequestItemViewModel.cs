using System;
using System.Collections.Generic;
using System.Linq;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.UI.ViewModels;
public sealed class ActionRequestItemViewModel : ViewModelBase
{
    private ActionRequestStatus _currentStatus;
    private string _fileName = string.Empty;
    private string _fullPath = string.Empty;
    private IReadOnlyList<ActionStatusEntryViewModel> _history = Array.Empty<ActionStatusEntryViewModel>();
    private Guid _id;
    private DateTimeOffset? _lastStatusTimestampUtc;
    private string? _latestNote;
    private string _monitoredFolderName = string.Empty;
    private string? _ruleName;

    public ActionRequestStatus CurrentStatus
    {
        get => _currentStatus;
        private set
        {
            if (!SetProperty(ref _currentStatus, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CurrentStatusText));
            OnPropertyChanged(nameof(IsPendingApproval));
        }
    }

    public string CurrentStatusText => CurrentStatus.ToString();

    public string FileName
    {
        get => _fileName;
        private set => SetProperty(ref _fileName, value);
    }

    public string FullPath
    {
        get => _fullPath;
        private set => SetProperty(ref _fullPath, value);
    }

    public IReadOnlyList<ActionStatusEntryViewModel> History
    {
        get => _history;
        private set => SetProperty(ref _history, value);
    }

    public Guid Id
    {
        get => _id;
        private set => SetProperty(ref _id, value);
    }

    public bool IsPendingApproval => CurrentStatus == ActionRequestStatus.PendingApproval;

    public DateTimeOffset? LastStatusTimestampUtc
    {
        get => _lastStatusTimestampUtc;
        private set => SetProperty(ref _lastStatusTimestampUtc, value);
    }

    public string? LatestNote
    {
        get => _latestNote;
        private set => SetProperty(ref _latestNote, value);
    }

    public string MonitoredFolderName
    {
        get => _monitoredFolderName;
        private set => SetProperty(ref _monitoredFolderName, value);
    }

    public string? RuleName
    {
        get => _ruleName;
        private set => SetProperty(ref _ruleName, value);
    }

    public static ActionRequestItemViewModel FromActionRequest(ActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = new ActionRequestItemViewModel();
        item.UpdateFrom(request);
        return item;
    }

    public void UpdateFrom(ActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Id = request.Id;
        FileName = request.File.FileName;
        FullPath = request.File.FullPath;
        MonitoredFolderName = request.Folder.Name;
        RuleName = request.RuleSnapshot?.Name;
        CurrentStatus = request.CurrentStatus;

        var history = request.History
            .Select(entry => new ActionStatusEntryViewModel(entry))
            .ToList();

        History = history;
        LastStatusTimestampUtc = request.History.LastOrDefault()?.TimestampUtc;
        LatestNote = request.History.LastOrDefault()?.Note;
    }
}
