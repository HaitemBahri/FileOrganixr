using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Tests.Execution;
public sealed class ApprovalWorkflowTests
{
    [Fact]
    public void Approve_MovesPendingRequestToQueued_AndEnqueuesIt()
    {
        var updater = new ActionRequestUpdater();
        var gate = new ApprovalGate(updater);
        var queue = new RecordingExecutionQueue();
        var orchestrator = new ExecutionOrchestrator(
            new NeverApprovalPolicy(),
            gate,
            queue,
            new NoopConcurrencyLimiter(),
            new NoopActionExecutor(),
            updater);
        var workflow = new ApprovalWorkflow(gate, orchestrator);

        var request = CreatePendingRequest(updater);
        gate.RegisterPending(request);

        var updated = workflow.Approve(request.Id);

        Assert.NotNull(updated);
        Assert.Equal(ActionRequestStatus.Queued, updated.CurrentStatus);
        Assert.Single(queue.Items);
        Assert.Same(updated, queue.Items[0]);
        Assert.Equal(ActionRequestStatus.Approved, updated.History[^2].Status);
        Assert.Equal(ActionRequestStatus.Queued, updated.History[^1].Status);
    }

    [Fact]
    public void Reject_MovesPendingRequestToRejected_WithoutQueueing()
    {
        var updater = new ActionRequestUpdater();
        var gate = new ApprovalGate(updater);
        var queue = new RecordingExecutionQueue();
        var orchestrator = new ExecutionOrchestrator(
            new NeverApprovalPolicy(),
            gate,
            queue,
            new NoopConcurrencyLimiter(),
            new NoopActionExecutor(),
            updater);
        var workflow = new ApprovalWorkflow(gate, orchestrator);

        var request = CreatePendingRequest(updater);
        gate.RegisterPending(request);

        var updated = workflow.Reject(request.Id);

        Assert.NotNull(updated);
        Assert.Equal(ActionRequestStatus.Rejected, updated.CurrentStatus);
        Assert.Empty(queue.Items);
        Assert.Equal(ActionRequestStatus.Rejected, updated.History[^1].Status);
    }

    [Fact]
    public void Approve_ReturnsNull_WhenRequestIdIsUnknown()
    {
        var updater = new ActionRequestUpdater();
        var gate = new ApprovalGate(updater);
        var queue = new RecordingExecutionQueue();
        var orchestrator = new ExecutionOrchestrator(
            new NeverApprovalPolicy(),
            gate,
            queue,
            new NoopConcurrencyLimiter(),
            new NoopActionExecutor(),
            updater);
        var workflow = new ApprovalWorkflow(gate, orchestrator);

        var updated = workflow.Approve(Guid.NewGuid());

        Assert.Null(updated);
        Assert.Empty(queue.Items);
    }

    [Fact]
    public void Reject_ReturnsNull_WhenRequestIdIsUnknown()
    {
        var updater = new ActionRequestUpdater();
        var gate = new ApprovalGate(updater);
        var queue = new RecordingExecutionQueue();
        var orchestrator = new ExecutionOrchestrator(
            new NeverApprovalPolicy(),
            gate,
            queue,
            new NoopConcurrencyLimiter(),
            new NoopActionExecutor(),
            updater);
        var workflow = new ApprovalWorkflow(gate, orchestrator);

        var updated = workflow.Reject(Guid.NewGuid());

        Assert.Null(updated);
        Assert.Empty(queue.Items);
    }

    [Fact]
    public void Approve_ReturnsNull_WhenDecidingSameRequestTwice()
    {
        var updater = new ActionRequestUpdater();
        var gate = new ApprovalGate(updater);
        var queue = new RecordingExecutionQueue();
        var orchestrator = new ExecutionOrchestrator(
            new NeverApprovalPolicy(),
            gate,
            queue,
            new NoopConcurrencyLimiter(),
            new NoopActionExecutor(),
            updater);
        var workflow = new ApprovalWorkflow(gate, orchestrator);

        var request = CreatePendingRequest(updater);
        gate.RegisterPending(request);

        var first = workflow.Approve(request.Id);
        var second = workflow.Approve(request.Id);

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Equal(ActionRequestStatus.Queued, request.CurrentStatus);
        Assert.Single(queue.Items);
    }

    [Fact]
    public void Reject_ReturnsNull_WhenDecidingSameRequestTwice()
    {
        var updater = new ActionRequestUpdater();
        var gate = new ApprovalGate(updater);
        var queue = new RecordingExecutionQueue();
        var orchestrator = new ExecutionOrchestrator(
            new NeverApprovalPolicy(),
            gate,
            queue,
            new NoopConcurrencyLimiter(),
            new NoopActionExecutor(),
            updater);
        var workflow = new ApprovalWorkflow(gate, orchestrator);

        var request = CreatePendingRequest(updater);
        gate.RegisterPending(request);

        var first = workflow.Reject(request.Id);
        var second = workflow.Reject(request.Id);

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Equal(ActionRequestStatus.Rejected, request.CurrentStatus);
        Assert.Empty(queue.Items);
    }

    private static ActionRequest CreatePendingRequest(ActionRequestUpdater updater)
    {
        var request = new ActionRequest
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
                SizeBytes = 1
            }
        };

        updater.AddStatus(request, ActionRequestStatus.Detected);
        updater.AddStatus(request, ActionRequestStatus.RuleMatched);
        updater.AddStatus(request, ActionRequestStatus.PendingApproval);

        return request;
    }

    private sealed class NeverApprovalPolicy : IUserApprovalPolicy
    {
        public bool RequiresApproval(ActionRequest request)
        {
            return false;
        }
    }

    private sealed class RecordingExecutionQueue : IExecutionQueue
    {
        public List<ActionRequest> Items { get; } = [];

        public Task<ActionRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public void Enqueue(ActionRequest request)
        {
            Items.Add(request);
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
