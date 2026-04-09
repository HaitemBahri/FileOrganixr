using System.Collections.ObjectModel;

namespace FileOrganixr.UI.ViewModels;
public sealed class RequestDetailsViewModel : ViewModelBase
{
    private string _currentStatus = string.Empty;
    private string _fileName = string.Empty;
    private string _fullPath = string.Empty;
    private bool _hasSelection;
    private string? _latestNote;
    private string _monitoredFolderName = string.Empty;
    private string? _ruleName;

    public string CurrentStatus
    {
        get => _currentStatus;
        private set => SetProperty(ref _currentStatus, value);
    }

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

    public bool HasSelection
    {
        get => _hasSelection;
        private set
        {
            if (!SetProperty(ref _hasSelection, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectionHintText));
        }
    }

    public ObservableCollection<ActionStatusEntryViewModel> History { get; } = [];

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

    public string SelectionHintText =>
        HasSelection
            ? string.Empty
            : "Select a request to inspect details and use approval actions.";

    public void BindTo(ActionRequestItemViewModel? request)
    {
        if (request is null)
        {
            HasSelection = false;
            FileName = string.Empty;
            FullPath = string.Empty;
            MonitoredFolderName = string.Empty;
            RuleName = null;
            CurrentStatus = string.Empty;
            LatestNote = null;
            History.Clear();
            return;
        }

        HasSelection = true;
        FileName = request.FileName;
        FullPath = request.FullPath;
        MonitoredFolderName = request.MonitoredFolderName;
        RuleName = request.RuleName;
        CurrentStatus = request.CurrentStatusText;
        LatestNote = request.LatestNote;

        History.Clear();
        foreach (var historyEntry in request.History)
        {
            History.Add(historyEntry);
        }
    }
}
