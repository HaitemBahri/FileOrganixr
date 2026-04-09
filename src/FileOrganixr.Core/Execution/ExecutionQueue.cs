


using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public sealed class ExecutionQueue : IExecutionQueue
{

    private readonly Channel<ActionRequest> _channel;

    public ExecutionQueue()
    {

        _channel = Channel.CreateUnbounded<ActionRequest>(new UnboundedChannelOptions
        {

            SingleWriter = false,


            SingleReader = true
        });
    }

    public async Task<ActionRequest> DequeueAsync(CancellationToken cancellationToken)
    {

        var item = await _channel.Reader.ReadAsync(cancellationToken);


        return item;
    }

    public void Enqueue(ActionRequest request)
    {

        ArgumentNullException.ThrowIfNull(request);


        if (!_channel.Writer.TryWrite(request))

            throw new InvalidOperationException("Failed to enqueue ActionRequest.");
    }
}
