using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Tests.Execution;
public sealed class ExecutionOrchestratorTests
{
    [Fact]
    public void OnRuleMatched_QueuesRequest_WhenApprovalIsNotRequired()
    {
        var queue = new RecordingExecutionQueue();
        var approvalGate = new RecordingApprovalGate();
        var updater = new ActionRequestUpdater();
        var orchestrator = CreateOrchestrator(
            requiresApproval: false,
            approvalGate,
            queue,
            updater);
        var request = CreateRuleMatchedRequest(updater, userApproval: false);

        orchestrator.OnRuleMatched(request);

        Assert.Equal(ActionRequestStatus.Queued, request.CurrentStatus);
        Assert.Equal([request], queue.Items);
        Assert.Empty(approvalGate.Registered);
    }

    [Fact]
    public void OnRuleMatched_MarksPendingAndRegisters_WhenApprovalIsRequired()
    {
        var queue = new RecordingExecutionQueue();
        var approvalGate = new RecordingApprovalGate();
        var updater = new ActionRequestUpdater();
        var orchestrator = CreateOrchestrator(
            requiresApproval: true,
            approvalGate,
            queue,
            updater);
        var request = CreateRuleMatchedRequest(updater, userApproval: true);

        orchestrator.OnRuleMatched(request);

        Assert.Equal(ActionRequestStatus.PendingApproval, request.CurrentStatus);
        Assert.Equal([request], approvalGate.Registered);
        Assert.Empty(queue.Items);
    }

    [Fact]
    public void OnRuleMatched_IgnoresRequest_WhenStatusIsNotRuleMatched()
    {
        var queue = new RecordingExecutionQueue();
        var approvalGate = new RecordingApprovalGate();
        var updater = new ActionRequestUpdater();
        var orchestrator = CreateOrchestrator(
            requiresApproval: false,
            approvalGate,
            queue,
            updater);
        var request = CreateDetectedRequest(updater);

        orchestrator.OnRuleMatched(request);

        Assert.Equal(ActionRequestStatus.Detected, request.CurrentStatus);
        Assert.Empty(queue.Items);
        Assert.Empty(approvalGate.Registered);
    }

    [Fact]
    public void OnApprovalOutcome_QueuesOnlyApprovedRequests()
    {
        var queue = new RecordingExecutionQueue();
        var approvalGate = new RecordingApprovalGate();
        var updater = new ActionRequestUpdater();
        var orchestrator = CreateOrchestrator(
            requiresApproval: false,
            approvalGate,
            queue,
            updater);
        var approved = CreateApprovedRequest(updater);
        var rejected = CreateRejectedRequest(updater);

        orchestrator.OnApprovalOutcome(approved);
        orchestrator.OnApprovalOutcome(rejected);

        Assert.Equal(ActionRequestStatus.Queued, approved.CurrentStatus);
        Assert.Equal(ActionRequestStatus.Rejected, rejected.CurrentStatus);
        Assert.Equal([approved], queue.Items);
    }

    private static ExecutionOrchestrator CreateOrchestrator(
        bool requiresApproval,
        IApprovalGate approvalGate,
        IExecutionQueue queue,
        IActionRequestUpdater updater)
    {
        return new ExecutionOrchestrator(
            new StubApprovalPolicy(requiresApproval),
            approvalGate,
            queue,
            new NoopConcurrencyLimiter(),
            new NoopActionExecutor(),
            updater);
    }

    private static ActionRequest CreateDetectedRequest(ActionRequestUpdater updater)
    {
        var request = CreateRequest();
        updater.AddStatus(request, ActionRequestStatus.Detected);
        return request;
    }

    private static ActionRequest CreateRuleMatchedRequest(ActionRequestUpdater updater, bool userApproval)
    {
        var request = CreateRequest();
        request.RuleSnapshot = new RuleDefinition
        {
            Name = "Rule",
            Action = new DeleteActionDefinition(),
            Query = new RegexFileNameQueryDefinition { Pattern = ".*" },
            UserApproval = userApproval
        };

        updater.AddStatus(request, ActionRequestStatus.Detected);
        updater.AddStatus(request, ActionRequestStatus.RuleMatched);

        return request;
    }

    private static ActionRequest CreateApprovedRequest(ActionRequestUpdater updater)
    {
        var request = CreateRuleMatchedRequest(updater, userApproval: true);
        updater.AddStatus(request, ActionRequestStatus.PendingApproval);
        updater.AddStatus(request, ActionRequestStatus.Approved);
        return request;
    }

    private static ActionRequest CreateRejectedRequest(ActionRequestUpdater updater)
    {
        var request = CreateRuleMatchedRequest(updater, userApproval: true);
        updater.AddStatus(request, ActionRequestStatus.PendingApproval);
        updater.AddStatus(request, ActionRequestStatus.Rejected);
        return request;
    }

    private static ActionRequest CreateRequest()
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
                SizeBytes = 10
            }
        };
    }

    private sealed class StubApprovalPolicy : IUserApprovalPolicy
    {
        private readonly bool _requiresApproval;

        public StubApprovalPolicy(bool requiresApproval)
        {
            _requiresApproval = requiresApproval;
        }

        public bool RequiresApproval(ActionRequest request)
        {
            return _requiresApproval;
        }
    }

    private sealed class RecordingApprovalGate : IApprovalGate
    {
        public List<ActionRequest> Registered { get; } = [];

        public void RegisterPending(ActionRequest request)
        {
            Registered.Add(request);
        }

        public ActionRequest? Approve(Guid requestId)
        {
            return null;
        }

        public ActionRequest? Reject(Guid requestId)
        {
            return null;
        }
    }

    private sealed class RecordingExecutionQueue : IExecutionQueue
    {
        public List<ActionRequest> Items { get; } = [];

        public void Enqueue(ActionRequest request)
        {
            Items.Add(request);
        }

        public Task<ActionRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class NoopConcurrencyLimiter : IConcurrencyLimiter
    {
        public Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IDisposable>(new NoopDisposable());
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class NoopActionExecutor : IActionExecutor
    {
        public Task ExecuteAsync(ActionRequest request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
