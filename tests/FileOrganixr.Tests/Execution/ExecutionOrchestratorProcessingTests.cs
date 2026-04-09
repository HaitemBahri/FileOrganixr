using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Tests.Execution;
public sealed class ExecutionOrchestratorProcessingTests
{
    [Fact]
    public async Task Start_OnRuleMatched_ProcessesQueuedRequestToCompleted()
    {
        var queue = new ExecutionQueue();
        var updater = new ActionRequestUpdater();
        var limiter = new RecordingConcurrencyLimiter();
        var executor = new RecordingActionExecutor();
        var orchestrator = new ExecutionOrchestrator(
            new NeverApprovalPolicy(),
            new NoopApprovalGate(),
            queue,
            limiter,
            executor,
            updater);
        var request = CreateRuleMatchedRequest(updater);

        orchestrator.Start();
        orchestrator.OnRuleMatched(request);
        await WaitForStatusAsync(request, ActionRequestStatus.Completed, TimeSpan.FromSeconds(2));
        orchestrator.Stop();

        Assert.Equal(ActionRequestStatus.Completed, request.CurrentStatus);
        Assert.Equal(ActionRequestStatus.Queued, request.History[^3].Status);
        Assert.Equal(ActionRequestStatus.Processing, request.History[^2].Status);
        Assert.Equal(ActionRequestStatus.Completed, request.History[^1].Status);
        Assert.Single(executor.ExecutedRequests);
        Assert.Same(request, executor.ExecutedRequests[0]);
        Assert.Equal(1, limiter.AcquireCalls);
    }

    [Fact]
    public async Task Start_WhenActionExecutorThrows_MarksRequestFailed()
    {
        var queue = new ExecutionQueue();
        var updater = new ActionRequestUpdater();
        var limiter = new RecordingConcurrencyLimiter();
        var executor = new RecordingActionExecutor { Failure = new InvalidOperationException("execution failed") };
        var orchestrator = new ExecutionOrchestrator(
            new NeverApprovalPolicy(),
            new NoopApprovalGate(),
            queue,
            limiter,
            executor,
            updater);
        var request = CreateRuleMatchedRequest(updater);

        orchestrator.Start();
        orchestrator.OnRuleMatched(request);
        await WaitForStatusAsync(request, ActionRequestStatus.Failed, TimeSpan.FromSeconds(2));
        orchestrator.Stop();

        Assert.Equal(ActionRequestStatus.Failed, request.CurrentStatus);
        Assert.Equal(ActionRequestStatus.Queued, request.History[^3].Status);
        Assert.Equal(ActionRequestStatus.Processing, request.History[^2].Status);
        Assert.Equal(ActionRequestStatus.Failed, request.History[^1].Status);
        Assert.Equal("execution failed", request.History[^1].Note);
    }

    [Fact]
    public async Task OnApprovalOutcome_ApprovedRequest_IsProcessedToCompleted()
    {
        var queue = new ExecutionQueue();
        var updater = new ActionRequestUpdater();
        var limiter = new RecordingConcurrencyLimiter();
        var executor = new RecordingActionExecutor();
        var orchestrator = new ExecutionOrchestrator(
            new NeverApprovalPolicy(),
            new NoopApprovalGate(),
            queue,
            limiter,
            executor,
            updater);
        var request = CreateApprovedRequest(updater);

        orchestrator.Start();
        orchestrator.OnApprovalOutcome(request);
        await WaitForStatusAsync(request, ActionRequestStatus.Completed, TimeSpan.FromSeconds(2));
        orchestrator.Stop();

        Assert.Equal(ActionRequestStatus.Completed, request.CurrentStatus);
        Assert.Equal(ActionRequestStatus.Queued, request.History[^3].Status);
        Assert.Equal(ActionRequestStatus.Processing, request.History[^2].Status);
        Assert.Equal(ActionRequestStatus.Completed, request.History[^1].Status);
    }

    private static ActionRequest CreateRuleMatchedRequest(ActionRequestUpdater updater)
    {
        var request = CreateRequest();
        request.RuleSnapshot = new RuleDefinition
        {
            Name = "Rule",
            Action = new DeleteActionDefinition(),
            Query = new RegexFileNameQueryDefinition { Pattern = ".*" },
            UserApproval = false
        };

        updater.AddStatus(request, ActionRequestStatus.Detected);
        updater.AddStatus(request, ActionRequestStatus.RuleMatched);
        return request;
    }

    private static ActionRequest CreateApprovedRequest(ActionRequestUpdater updater)
    {
        var request = CreateRuleMatchedRequest(updater);
        request.RuleSnapshot = new RuleDefinition
        {
            Name = "Rule",
            Action = new DeleteActionDefinition(),
            Query = new RegexFileNameQueryDefinition { Pattern = ".*" },
            UserApproval = true
        };
        updater.AddStatus(request, ActionRequestStatus.PendingApproval);
        updater.AddStatus(request, ActionRequestStatus.Approved);
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

    private static async Task WaitForStatusAsync(ActionRequest request, ActionRequestStatus expectedStatus, TimeSpan timeout)
    {
        var delay = Task.Delay(timeout);

        while (!delay.IsCompleted)
        {
            if (request.CurrentStatus == expectedStatus)
            {
                return;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException(
            $"Timed out waiting for status '{expectedStatus}'. Last observed status: '{request.CurrentStatus}'.");
    }

    private sealed class NeverApprovalPolicy : IUserApprovalPolicy
    {
        public bool RequiresApproval(ActionRequest request)
        {
            return false;
        }
    }

    private sealed class NoopApprovalGate : IApprovalGate
    {
        public void RegisterPending(ActionRequest request)
        {
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

    private sealed class RecordingConcurrencyLimiter : IConcurrencyLimiter
    {
        public int AcquireCalls { get; private set; }

        public Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
        {
            AcquireCalls++;
            return Task.FromResult<IDisposable>(new NoopDisposable());
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class RecordingActionExecutor : IActionExecutor
    {
        public Exception? Failure { get; init; }

        public List<ActionRequest> ExecutedRequests { get; } = [];

        public Task ExecuteAsync(ActionRequest request, CancellationToken cancellationToken)
        {
            ExecutedRequests.Add(request);
            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.CompletedTask;
        }
    }
}
