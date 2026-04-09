using FileOrganixr.Core;
using FileOrganixr.Infrastructure;
using FileOrganixr.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Threading;

namespace FileOrganixr.UI;
public static class Program
{
    [STAThread]
    public static void Main()
    {
        var services = new ServiceCollection();

        services.RegisterCoreServices();
        services.RegisterInfrastructureServices();
        services.AddSingleton(_ => Dispatcher.CurrentDispatcher);

        services.AddSingleton<RequestsViewModel>();
        services.AddSingleton<RequestDetailsViewModel>();
        services.AddSingleton<ApprovalActionsViewModel>();
        services.AddSingleton<RuntimeStateViewModel>();
        services.AddSingleton<SettingsPanelViewModel>();
        services.AddSingleton<NotificationBannerViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<App>();

        using var serviceProvider = services.BuildServiceProvider();
        App.Services = serviceProvider;
        var app = serviceProvider.GetRequiredService<App>();

        app.Run();
    }
}
