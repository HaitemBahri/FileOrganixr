using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FileOrganixr.Core.Watchers.FileEvents;
public sealed class FileEventDispatcher : IFileEventDispatcher, IDisposable
{

    private readonly Channel<FileEvent> _channel;


    private readonly Task _consumer;


    private readonly CancellationTokenSource _cts = new();


    private readonly IFileEventHandler _handler;


    private bool _disposed;

    public FileEventDispatcher(IFileEventHandler handler)
    {

        _handler = handler;


        _channel = Channel.CreateBounded<FileEvent>(new BoundedChannelOptions(5_000)
        {

            SingleReader = true,

            SingleWriter = false,

            FullMode = BoundedChannelFullMode.Wait
        });


        _consumer = Task.Run(ConsumeAsync);
    }

    public void Dispatch(FileEvent fileEvent)
    {
        ArgumentNullException.ThrowIfNull(fileEvent);


        if (_disposed)

            throw new ObjectDisposedException(nameof(FileEventDispatcher));


        if (!_channel.Writer.TryWrite(fileEvent))

            try
            {
                _channel.Writer.WriteAsync(fileEvent, _cts.Token).AsTask().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {

            }
            catch (ChannelClosedException) when (_disposed)
            {

            }
            catch (Exception ex)
            {
                Trace.TraceError($"FileEventDispatcher failed to enqueue event: {ex}");
            }
    }

    public void Dispose()
    {

        if (_disposed)

            return;


        _disposed = true;


        _cts.Cancel();


        _channel.Writer.TryComplete();

        try
        {

            _consumer.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {

        }


        _cts.Dispose();
    }

    private async Task ConsumeAsync()
    {
        try
        {

            while (await _channel.Reader.WaitToReadAsync(_cts.Token))
            {

                while (_channel.Reader.TryRead(out var fileEvent))
                    try
                    {

                        await _handler.HandleAsync(fileEvent, _cts.Token);
                    }
                    catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                    {

                    }
                    catch (Exception ex)
                    {

                        Trace.TraceError($"FileEventDispatcher handler failed while processing an event: {ex}");
                    }
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {

        }
        catch (Exception ex)
        {
            Trace.TraceError($"FileEventDispatcher consumer loop terminated due to an unexpected exception: {ex}");
        }
    }
}
