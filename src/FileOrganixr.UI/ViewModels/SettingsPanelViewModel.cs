using FileOrganixr.Infrastructure.Configuration;
using FileOrganixr.UI.Commands;
using System;
using System.Windows.Input;

namespace FileOrganixr.UI.ViewModels;
public sealed class SettingsPanelViewModel : ViewModelBase
{
    private string _effectiveConfigPath = "Unavailable";
    private string _lastRefreshError = string.Empty;
    private string _overrideValue = "(not set)";
    private bool _usingEnvironmentOverride;

    public SettingsPanelViewModel(ISettingsResolver settingsResolver)
    {
        ArgumentNullException.ThrowIfNull(settingsResolver);

        SettingsResolver = settingsResolver;
        RefreshCommand = new DelegateCommand(Refresh);

        Refresh();
    }

    public string EffectiveConfigPath
    {
        get => _effectiveConfigPath;
        private set => SetProperty(ref _effectiveConfigPath, value);
    }

    public string LastRefreshError
    {
        get => _lastRefreshError;
        private set
        {
            if (!SetProperty(ref _lastRefreshError, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasRefreshError));
        }
    }

    public string OverrideEnvironmentVariableName => UserSettingsResolver.ConfigPathEnvironmentVariable;

    public string OverrideGuidance =>
        $"Set environment variable '{OverrideEnvironmentVariableName}' to override the default YAML config path.";

    public string OverrideStateText => IsUsingEnvironmentOverride ? "Active" : "Not set";

    public string OverrideValue
    {
        get => _overrideValue;
        private set => SetProperty(ref _overrideValue, value);
    }

    public ICommand RefreshCommand { get; }

    public ISettingsResolver SettingsResolver { get; }

    public bool HasRefreshError => !string.IsNullOrWhiteSpace(LastRefreshError);

    public bool IsUsingEnvironmentOverride
    {
        get => _usingEnvironmentOverride;
        private set
        {
            if (!SetProperty(ref _usingEnvironmentOverride, value))
            {
                return;
            }

            OnPropertyChanged(nameof(OverrideStateText));
        }
    }

    public event Action<string>? RefreshFailed;

    public void Refresh()
    {
        try
        {
            var settings = SettingsResolver.ResolveSettings();
            var envValue = Environment.GetEnvironmentVariable(OverrideEnvironmentVariableName);

            EffectiveConfigPath = settings.ConfigFilePath;
            IsUsingEnvironmentOverride = !string.IsNullOrWhiteSpace(envValue);
            OverrideValue = string.IsNullOrWhiteSpace(envValue) ? "(not set)" : envValue.Trim();
            LastRefreshError = string.Empty;
        }
        catch (Exception ex)
        {
            EffectiveConfigPath = "Unavailable";
            IsUsingEnvironmentOverride = false;
            OverrideValue = "(not set)";
            LastRefreshError = ex.Message;
            RefreshFailed?.Invoke(ex.Message);
        }
    }
}
