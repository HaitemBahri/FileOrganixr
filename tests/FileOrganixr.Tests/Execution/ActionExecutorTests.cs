using System;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Tests.Execution;
public sealed class ActionExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ResolvesHandler_AndDelegatesExecution()
    {
        var handler = new RecordingActionHandler();
        var registry = new RecordingActionHandlerRegistry(handler);
        var executor = new ActionExecutor(registry);
        var request = CreateRequest(new DeleteActionDefinition());
        using var cts = new CancellationTokenSource();

        await executor.ExecuteAsync(request, cts.Token);

        Assert.NotNull(registry.ResolvedAction);
        Assert.Equal("Delete", registry.ResolvedAction!.Type);
        Assert.Same(request, handler.ReceivedRequest);
        Assert.Same(request.RuleSnapshot!.Action, handler.ReceivedActionDefinition);
        Assert.Equal(cts.Token, handler.ReceivedCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenRuleSnapshotIsMissing()
    {
        var handler = new RecordingActionHandler();
        var registry = new RecordingActionHandlerRegistry(handler);
        var executor = new ActionExecutor(registry);
        var request = CreateRequest(new DeleteActionDefinition());
        request.RuleSnapshot = null;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteAsync(request, CancellationToken.None));

        Assert.Contains("RuleSnapshot is null", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenActionDefinitionIsMissing()
    {
        var handler = new RecordingActionHandler();
        var registry = new RecordingActionHandlerRegistry(handler);
        var executor = new ActionExecutor(registry);
        var request = CreateRequest(new DeleteActionDefinition());
        request.RuleSnapshot = new RuleDefinition
        {
            Name = "Rule",
            Action = null!,
            Query = new RegexFileNameQueryDefinition { Pattern = ".*" }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteAsync(request, CancellationToken.None));

        Assert.Contains("Action definition is null", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenRequestIsNull()
    {
        var handler = new RecordingActionHandler();
        var registry = new RecordingActionHandlerRegistry(handler);
        var executor = new ActionExecutor(registry);

        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync(null!, CancellationToken.None));
    }

    private static ActionRequest CreateRequest(IActionDefinition actionDefinition)
    {
        return new ActionRequest
        {
            Id = Guid.NewGuid(),
            Folder = new FolderDefinition
            {
                Name = "Inbox",
                Path = @"C:\Inbox",
                Rules = []
            },
            File = new FileContext
            {
                FullPath = @"C:\Inbox\sample.txt",
                FileName = "sample.txt",
                FileNameWithoutExtension = "sample",
                Extension = ".txt",
                SizeBytes = 42
            },
            RuleSnapshot = new RuleDefinition
            {
                Name = "Rule",
                Action = actionDefinition,
                Query = new RegexFileNameQueryDefinition { Pattern = ".*" }
            }
        };
    }

    private sealed class RecordingActionHandlerRegistry : IActionHandlerRegistry
    {
        private readonly IActionHandler _handler;

        public RecordingActionHandlerRegistry(IActionHandler handler)
        {
            _handler = handler;
        }

        public IActionDefinition? ResolvedAction { get; private set; }

        public IActionHandler Resolve(IActionDefinition actionDefinition)
        {
            ResolvedAction = actionDefinition;
            return _handler;
        }
    }

    private sealed class RecordingActionHandler : IActionHandler
    {
        public string SupportedActionType => "Delete";

        public IActionDefinition? ReceivedActionDefinition { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public ActionRequest? ReceivedRequest { get; private set; }

        public Task ExecuteAsync(ActionRequest request, IActionDefinition actionDefinition, CancellationToken cancellationToken)
        {
            ReceivedRequest = request;
            ReceivedActionDefinition = actionDefinition;
            ReceivedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
