using FileOrganixr.UI.Commands;
using System;
using System.Windows.Input;

namespace FileOrganixr.UI.ViewModels;
public sealed class NotificationBannerViewModel : ViewModelBase
{
    private readonly DelegateCommand _dismissCommand;
    private string _details = string.Empty;
    private bool _isVisible;
    private string _message = string.Empty;
    private NotificationSeverity _severity = NotificationSeverity.Info;

    public NotificationBannerViewModel()
    {
        _dismissCommand = new DelegateCommand(Dismiss, () => IsVisible);
    }

    public string Details
    {
        get => _details;
        private set
        {
            if (!SetProperty(ref _details, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasDetails));
        }
    }

    public ICommand DismissCommand => _dismissCommand;

    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    public bool IsVisible
    {
        get => _isVisible;
        private set
        {
            if (!SetProperty(ref _isVisible, value))
            {
                return;
            }

            _dismissCommand.RaiseCanExecuteChanged();
        }
    }

    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public NotificationSeverity Severity
    {
        get => _severity;
        private set
        {
            if (!SetProperty(ref _severity, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SeverityText));
        }
    }

    public string SeverityText =>
        Severity switch
        {
            NotificationSeverity.Error => "Error",
            NotificationSeverity.Warning => "Warning",
            _ => "Info"
        };

    public void Dismiss()
    {
        IsVisible = false;
        Message = string.Empty;
        Details = string.Empty;
        Severity = NotificationSeverity.Info;
    }

    public void Show(string message, NotificationSeverity severity, string? details = null)
    {
        Message = string.IsNullOrWhiteSpace(message) ? "Notification" : message;
        Details = details?.Trim() ?? string.Empty;
        Severity = severity;
        IsVisible = true;
    }
}
