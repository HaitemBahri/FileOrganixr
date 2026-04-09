using FileOrganixr.Core.Configuration.Providers;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;
using FileOrganixr.Infrastructure.Configuration;
using FileOrganixr.Infrastructure.Execution;
using FileOrganixr.Infrastructure.Watchers;
using Microsoft.Extensions.DependencyInjection;
using YamlDotNet.Serialization;

namespace FileOrganixr.Infrastructure;
public static class InfrastructureServicesRegistrar
{
    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationProvider, YamlFileConfigurationProvider>();
        services.AddSingleton<ISettingsResolver, UserSettingsResolver>();
        services.AddSingleton<IDeserializer, YamlDeserializer>();

        services.AddSingleton<IFolderWatcherFactory, FolderWatcherFactory>();

        services.AddSingleton<IActionHandler, MoveActionHandler>();
        services.AddSingleton<IActionHandler, DeleteActionHandler>();
        services.AddSingleton<IActionHandler, RenameActionHandler>();
        services.AddSingleton<IActionHandlerRegistry, ActionHandlerRegistry>();

        return services;
    }
}
