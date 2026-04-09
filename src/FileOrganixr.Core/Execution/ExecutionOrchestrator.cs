


using System;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Core.Execution;
public sealed class ExecutionOrchestrator : IExecutionOrchestrator, IDisposable
{

    private readonly IActionExecutor _actionExecutor;


    private readonly IApprovalGate _approvalGate;


    private readonly IUserApprovalPolicy _approvalPolicy;


    private readonly IConcurrencyLimiter _concurrencyLimiter;


    private readonly IExecutionQueue _executionQueue;


    private readonly object _gate = new();


    private readonly IActionRequestUpdater _requestUpdater;


    private Task? _consumerTask;


    private CancellationTokenSource? _cts;


    private bool _isDisposed;


    private bool _isRunning;

    public ExecutionOrchestrator(
        IUserApprovalPolicy approvalPolicy,
        IApprovalGate approvalGate,
        IExecutionQueue executionQueue,
        IConcurrencyLimiter concurrencyLimiter,
        IActionExecutor actionExecutor,
        IActionRequestUpdater requestUpdater)
    {

        _approvalPolicy = approvalPolicy;


        _approvalGate = approvalGate;


        _executionQueue = executionQueue;


        _concurrencyLimiter = concurrencyLimiter;


        _actionExecutor = actionExecutor;


        _requestUpdater = requestUpdater;
    }

    public void Dispose()
    {

        lock (_gate)
        {

            if (_isDisposed)

                return;


            _isDisposed = true;
        }


        Stop();
    }

    public void OnApprovalOutcome(ActionRequest request)
    {

        ArgumentNullException.ThrowIfNull(request);


        if (request.CurrentStatus == ActionRequestStatus.Rejected)

            return;


        if (request.CurrentStatus != ActionRequestStatus.Approved)

            return;


        _requestUpdater.AddStatus(request, ActionRequestStatus.Queued);


        _executionQueue.Enqueue(request);
    }

    public void OnRuleMatched(ActionRequest request)
    {

        ArgumentNullException.ThrowIfNull(request);


        if (request.CurrentStatus != ActionRequestStatus.RuleMatched)

            return;


        var requiresApproval = _approvalPolicy.RequiresApproval(request);


        if (requiresApproval)
        {

            _requestUpdater.AddStatus(request, ActionRequestStatus.PendingApproval);


            _approvalGate.RegisterPending(request);


            return;
        }


        _requestUpdater.AddStatus(request, ActionRequestStatus.Queued);


        _executionQueue.Enqueue(request);
    }

    public void Start()
    {

        lock (_gate)
        {

            if (_isDisposed)

                throw new ObjectDisposedException(nameof(ExecutionOrchestrator));


            if (_isRunning)

                return;


            _cts = new CancellationTokenSource();


            _consumerTask = Task.Run(() => ConsumeAsync(_cts.Token));


            _isRunning = true;
        }
    }

    public void Stop()
    {

        lock (_gate)
        {

            if (!_isRunning)

                return;


            _cts?.Cancel();


            _isRunning = false;
        }

        try
        {

            _consumerTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {

        }
        finally
        {

            _cts?.Dispose();


            _cts = null;


            _consumerTask = null;
        }
    }

    private async Task ConsumeAsync(CancellationToken ct)
    {

        while (!ct.IsCancellationRequested)
        {
            ActionRequest request;

            try
            {

                request = await _executionQueue.DequeueAsync(ct);
            }
            catch (OperationCanceledException)
            {

                return;
            }


            _ = ExecuteOneAsync(request, ct);
        }
    }

    private async Task ExecuteOneAsync(ActionRequest request, CancellationToken ct)
    {

        using var slot = await _concurrencyLimiter.AcquireAsync(ct);

        try
        {

            _requestUpdater.AddStatus(request, ActionRequestStatus.Processing);


            await _actionExecutor.ExecuteAsync(request, ct);


            _requestUpdater.AddStatus(request, ActionRequestStatus.Completed);
        }
        catch (OperationCanceledException)
        {

            _requestUpdater.AddStatus(request, ActionRequestStatus.Failed, "Cancelled");
        }
        catch (Exception ex)
        {

            _requestUpdater.AddStatus(request, ActionRequestStatus.Failed, ex.Message);
        }
    }
}
