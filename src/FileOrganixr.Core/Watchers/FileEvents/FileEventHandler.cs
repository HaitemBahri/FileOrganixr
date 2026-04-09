using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration.Stores;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;
using FileOrganixr.Core.Runtime.Rules;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Core.Watchers.FileEvents;
public sealed class FileEventHandler : IFileEventHandler
{
    private readonly IConfigurationStore _configurationStore;
    private readonly IExecutionOrchestrator _executionOrchestrator;
    private readonly IFileContextFactory _fileContextFactory;
    private readonly IFolderDefinitionResolver _folderResolver;
    private readonly IActionRequestUpdater _actionRequestUpdater;
    private readonly IActionRequestStore _requestStore;
    private readonly IRuleMatcher _ruleMatcher;

    public FileEventHandler(
        IConfigurationStore configurationStore,
        IFolderDefinitionResolver folderResolver,
        IFileContextFactory fileContextFactory,
        IRuleMatcher ruleMatcher,
        IActionRequestUpdater actionRequestUpdater,
        IExecutionOrchestrator executionOrchestrator,
        IActionRequestStore requestStore)
    {
        _configurationStore = configurationStore;
        _folderResolver = folderResolver;
        _fileContextFactory = fileContextFactory;
        _ruleMatcher = ruleMatcher;
        _actionRequestUpdater = actionRequestUpdater;
        _executionOrchestrator = executionOrchestrator;
        _requestStore = requestStore;
    }

    public Task HandleAsync(FileEvent fileEvent, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
        if (!IsDirectChildFileEvent(fileEvent)) return Task.CompletedTask;

        _ = _configurationStore.Current;

        var folder = _folderResolver.Resolve(fileEvent.WatchedFolderPath);

        var fileContext = _fileContextFactory.Create(fileEvent);

        var request = new ActionRequest
        {
            Id = Guid.NewGuid(),

            Folder = folder,

            File = fileContext
        };

        _actionRequestUpdater.AddStatus(request, ActionRequestStatus.Detected, $"Event: {fileEvent.Type}");

        var matchedRule = _ruleMatcher.FindFirstMatch(folder, fileContext);

        if (matchedRule is null)
        {
            _actionRequestUpdater.AddStatus(request, ActionRequestStatus.NoRuleMatched);

            _requestStore.Add(request);

            return Task.CompletedTask;
        }

        request.RuleSnapshot = matchedRule;

        _actionRequestUpdater.AddStatus(request, ActionRequestStatus.RuleMatched, matchedRule.Name);

        _requestStore.Add(request);

        _executionOrchestrator.OnRuleMatched(request);

        return Task.CompletedTask;
    }

    private static bool IsDirectChildFileEvent(FileEvent fileEvent)
    {
        if (string.IsNullOrWhiteSpace(fileEvent.WatchedFolderPath) || string.IsNullOrWhiteSpace(fileEvent.FullPath))
        {
            return false;
        }

        var fullPath = NormalizePath(fileEvent.FullPath);
        if (Directory.Exists(fullPath))
        {
            return false;
        }

        var watchedFolderPath = NormalizePath(fileEvent.WatchedFolderPath);
        var parent = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(parent))
        {
            return false;
        }

        parent = NormalizePath(parent);
        return string.Equals(parent, watchedFolderPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
