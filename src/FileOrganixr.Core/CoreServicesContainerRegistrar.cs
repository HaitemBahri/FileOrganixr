using FileOrganixr.Core.Configuration.Definitions.Registries;
using FileOrganixr.Core.Configuration.Stores;
using FileOrganixr.Core.Configuration.Validators;
using FileOrganixr.Core.Configuration.Validators.ActionValidators;
using FileOrganixr.Core.Configuration.Validators.FolderValidators;
using FileOrganixr.Core.Configuration.Validators.QueryValidator;
using FileOrganixr.Core.Configuration.Validators.RuleValidators;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.Rules;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;
using FileOrganixr.Core.Runtime.Hosting;
using FileOrganixr.Core.Runtime.Queries;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;
using Microsoft.Extensions.DependencyInjection;

namespace FileOrganixr.Core;
public static class CoreServicesContainerRegistrar
{
    public static IServiceCollection RegisterCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<ActionDefinitionRegistry>();
        services.AddSingleton<IActionDefinitionRegistry>(sp => sp.GetRequiredService<ActionDefinitionRegistry>());

        services.AddSingleton<QueryDefinitionRegistry>();
        services.AddSingleton<IQueryDefinitionRegistry>(sp => sp.GetRequiredService<QueryDefinitionRegistry>());

        services.AddSingleton<IConfigurationStore, ConfigurationStore>();

        services.AddSingleton<IConfigurationRootValidator, ConfigurationRootValidator>();

        services.AddSingleton<IFolderDefinitionValidator, FolderDefinitionValidator>();

        services.AddSingleton<IRuleDefinitionValidator, RuleDefinitionValidator>();

        services.AddSingleton<IActionDefinitionValidatorRegistry, ActionDefinitionValidatorRegistry>();

        services.AddSingleton<IQueryDefinitionValidatorRegistry, QueryDefinitionValidatorRegistry>();

        services.AddSingleton<IActionDefinitionValidator, MoveActionDefinitionValidator>();
        services.AddSingleton<IActionDefinitionValidator, DeleteActionDefinitionValidator>();
        services.AddSingleton<IActionDefinitionValidator, RenameActionDefinitionValidator>();

        services.AddSingleton<IQueryDefinitionValidator, RegexFileNameQueryDefinitionValidator>();
        services.AddSingleton<IQueryDefinitionValidator, FileSizeQueryDefinitionValidator>();

        services.AddSingleton<IQueryMatcher, RegexFileNameQueryMatcher>();
        services.AddSingleton<IQueryMatcher, FileSizeQueryMatcher>();
        services.AddSingleton<IQueryMatcherRegistry>(sp =>
        {
            var registry = new QueryMatcherRegistry();

            foreach (var matcher in sp.GetServices<IQueryMatcher>()) registry.Register(matcher);

            return registry;
        });
        services.AddSingleton<IQueryMatcherEngine, QueryMatcherEngine>();

        services.AddSingleton<IFolderDefinitionResolver, FolderDefinitionResolver>();
        services.AddSingleton<IFileContextFactory, FileContextFactory>();
        services.AddSingleton<IRuleMatcher, RuleMatcher>();

        services.AddSingleton<IActionRequestStore, ActionRequestStore>();
        services.AddSingleton<IActionRequestUpdater, ActionRequestUpdater>();

        services.AddSingleton<IFileEventHandler, FileEventHandler>();
        services.AddSingleton<IFileEventDispatcher, FileEventDispatcher>();
        services.AddSingleton<IWatcherErrorHandler, DefaultWatcherErrorHandler>();
        services.AddSingleton<IFileWatcherService, FileWatcherService>();

        services.AddSingleton<IUserApprovalPolicy, UserApprovalPolicy>();
        services.AddSingleton<IApprovalGate, ApprovalGate>();
        services.AddSingleton<IExecutionQueue, ExecutionQueue>();
        services.AddSingleton<IConcurrencyLimiter>(_ => new SemaphoreConcurrencyLimiter(4));
        services.AddSingleton<IActionExecutor, ActionExecutor>();
        services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();
        services.AddSingleton<IApprovalWorkflow, ApprovalWorkflow>();
        services.AddSingleton<IApplicationHost, ApplicationHost>();

        return services;
    }
}
