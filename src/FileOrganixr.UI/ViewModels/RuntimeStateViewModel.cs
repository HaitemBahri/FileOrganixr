using System;
using System.Collections.Generic;
using System.Linq;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.UI.ViewModels;
public sealed class RuntimeStateViewModel : ViewModelBase
{
    private int _errorCount;
    private string _effectiveConfigPath = "Unavailable";
    private bool _isHostRunning;
    private string _lastEventText = "No runtime events yet.";
    private DateTimeOffset? _lastEventTimestampUtc;

    public int ErrorCount
    {
        get => _errorCount;
        private set => SetProperty(ref _errorCount, value);
    }

    public string EffectiveConfigPath
    {
        get => _effectiveConfigPath;
        private set => SetProperty(ref _effectiveConfigPath, value);
    }

    public string HostStateText => IsHostRunning ? "Running" : "Stopped";

    public bool IsHostRunning
    {
        get => _isHostRunning;
        private set
        {
            if (!SetProperty(ref _isHostRunning, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HostStateText));
        }
    }

    public string LastEventText
    {
        get => _lastEventText;
        private set => SetProperty(ref _lastEventText, value);
    }

    public DateTimeOffset? LastEventTimestampUtc
    {
        get => _lastEventTimestampUtc;
        private set
        {
            if (!SetProperty(ref _lastEventTimestampUtc, value))
            {
                return;
            }

            OnPropertyChanged(nameof(LastEventTimestampText));
        }
    }

    public string LastEventTimestampText => LastEventTimestampUtc?.ToString("u") ?? "N/A";

    public void RecordRequestEvent(ActionRequestItemViewModel request)
    {
        ArgumentNullException.ThrowIfNull(request);

        LastEventTimestampUtc = request.LastStatusTimestampUtc ?? DateTimeOffset.UtcNow;

        LastEventText = string.IsNullOrWhiteSpace(request.LatestNote)
            ? $"{request.CurrentStatusText}: {request.FileName}"
            : $"{request.CurrentStatusText}: {request.FileName} ({request.LatestNote})";
    }

    public void SetConfigPath(string? configPath)
    {
        EffectiveConfigPath = string.IsNullOrWhiteSpace(configPath)
            ? "Unavailable"
            : configPath;
    }

    public void SetHostState(bool isRunning, string? configPath = null)
    {
        IsHostRunning = isRunning;

        if (configPath is not null)
        {
            SetConfigPath(configPath);
        }
    }

    public void UpdateFromRequests(IEnumerable<ActionRequestItemViewModel> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var snapshot = requests.ToList();
        ErrorCount = snapshot.Count(r => r.CurrentStatus == ActionRequestStatus.Failed);

        var latest = snapshot
            .OrderByDescending(r => r.LastStatusTimestampUtc)
            .FirstOrDefault();

        if (latest is null)
        {
            LastEventTimestampUtc = null;
            LastEventText = "No runtime events yet.";
            return;
        }

        RecordRequestEvent(latest);
    }
}
