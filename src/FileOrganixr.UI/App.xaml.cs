using System.Windows;
using FileOrganixr.Core;
using FileOrganixr.Core.Runtime.Hosting;
using FileOrganixr.Infrastructure.Configuration;
using FileOrganixr.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FileOrganixr.UI;
public partial class App : Application
{
    private IApplicationHost? _applicationHost;
    private MainWindow? _mainWindow;
    private RuntimeStateViewModel? _runtimeState;
    private ISettingsResolver? _settingsResolver;

    public static IServiceProvider? Services { get; set; }

    public App()
    {
    }

    public App(
        MainWindow mainWindow,
        IApplicationHost applicationHost,
        ISettingsResolver settingsResolver,
        RuntimeStateViewModel runtimeState)
    {
        ArgumentNullException.ThrowIfNull(mainWindow);
        ArgumentNullException.ThrowIfNull(applicationHost);
        ArgumentNullException.ThrowIfNull(settingsResolver);
        ArgumentNullException.ThrowIfNull(runtimeState);

        _mainWindow = mainWindow;
        _applicationHost = applicationHost;
        _settingsResolver = settingsResolver;
        _runtimeState = runtimeState;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        EnsureDependencies();

        if (_mainWindow is null || _applicationHost is null || _settingsResolver is null || _runtimeState is null)
        {
            MessageBox.Show(
                "FileOrganixr startup dependencies are not initialized. Ensure startup uses FileOrganixr.UI.Program.",
                "FileOrganixr Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
            return;
        }

        AppSettings? settings = null;

        try
        {
            settings = _settingsResolver.ResolveSettings();
            _runtimeState.SetConfigPath(settings.ConfigFilePath);
            await _applicationHost.StartAsync(CancellationToken.None);
            _runtimeState.SetHostState(_applicationHost.IsRunning, settings.ConfigFilePath);

            MainWindow = _mainWindow;
            _mainWindow.Show();
        }
        catch (Exception ex)
        {
            _runtimeState.SetHostState(false, settings?.ConfigFilePath);

            MessageBox.Show(
                BuildStartupFailureMessage(ex, settings),
                "FileOrganixr Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _applicationHost?.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            _runtimeState?.SetHostState(false);
        }
        catch
        {

        }
        finally
        {
            base.OnExit(e);
        }
    }

    private string BuildStartupFailureMessage(Exception exception, AppSettings? settings)
    {
        var configPath = settings?.ConfigFilePath ?? "Unavailable (settings resolution failed).";

        return """
               FileOrganixr failed to start.

               Startup error:
               """ + exception.Message + """

               Effective config path:
               """ + configPath + """

               Environment override:
               FILEORGANIXR_CONFIG_PATH
               """;
    }

    private void EnsureDependencies()
    {
        if (_mainWindow is not null &&
            _applicationHost is not null &&
            _settingsResolver is not null &&
            _runtimeState is not null)
        {
            return;
        }

        var services = Services;
        if (services is null)
        {
            return;
        }

        _mainWindow ??= services.GetService<MainWindow>();
        _applicationHost ??= services.GetService<IApplicationHost>();
        _settingsResolver ??= services.GetService<ISettingsResolver>();
        _runtimeState ??= services.GetService<RuntimeStateViewModel>();
    }
}
