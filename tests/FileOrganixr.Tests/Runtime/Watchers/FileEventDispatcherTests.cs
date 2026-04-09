using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Tests.Runtime.Watchers;
public sealed class FileEventDispatcherTests
{
    [Fact]
    public async Task Dispatch_ForwardsEventToHandler()
    {
        var handler = new RecordingFileEventHandler();
        using var dispatcher = new FileEventDispatcher(handler);
        var fileEvent = CreateEvent("/tmp/file-organixr/inbox/a.txt");

        dispatcher.Dispatch(fileEvent);
        await handler.WaitForCallCountAsync(1, TimeSpan.FromSeconds(2));

        Assert.Single(handler.Events);
        Assert.Equal(fileEvent.FullPath, handler.Events[0].FullPath);
    }

    [Fact]
    public async Task Dispatch_ContinuesWithNextEvent_WhenHandlerThrows()
    {
        var handler = new RecordingFileEventHandler(throwOnCallNumbers: new HashSet<int> { 1 });
        using var dispatcher = new FileEventDispatcher(handler);

        dispatcher.Dispatch(CreateEvent("/tmp/file-organixr/inbox/first.txt"));
        dispatcher.Dispatch(CreateEvent("/tmp/file-organixr/inbox/second.txt"));
        await handler.WaitForCallCountAsync(2, TimeSpan.FromSeconds(2));

        Assert.Equal(2, handler.Events.Count);
        Assert.Equal("/tmp/file-organixr/inbox/first.txt", handler.Events[0].FullPath);
        Assert.Equal("/tmp/file-organixr/inbox/second.txt", handler.Events[1].FullPath);
    }

    [Fact]
    public void Dispatch_ThrowsObjectDisposedException_WhenDisposed()
    {
        var handler = new RecordingFileEventHandler();
        var dispatcher = new FileEventDispatcher(handler);
        dispatcher.Dispose();

        Assert.Throws<ObjectDisposedException>(() => dispatcher.Dispatch(CreateEvent("/tmp/file-organixr/inbox/a.txt")));
    }

    [Fact]
    public void Dispose_CanBeCalledMoreThanOnce()
    {
        var handler = new RecordingFileEventHandler();
        var dispatcher = new FileEventDispatcher(handler);

        dispatcher.Dispose();
        dispatcher.Dispose();
    }

    private static FileEvent CreateEvent(string fullPath)
    {
        return new FileEvent(
            WatchedFolderPath: "/tmp/file-organixr/inbox",
            Type: FileEventType.Created,
            FullPath: fullPath,
            OldFullPath: null,
            TimestampUtc: DateTimeOffset.UtcNow);
    }

    private sealed class RecordingFileEventHandler : IFileEventHandler
    {
        private readonly HashSet<int> _throwOnCallNumbers;
        private readonly object _gate = new();
        private int _calls;

        public RecordingFileEventHandler(ISet<int>? throwOnCallNumbers = null)
        {
            _throwOnCallNumbers = throwOnCallNumbers is null ? [] : new HashSet<int>(throwOnCallNumbers);
        }

        public List<FileEvent> Events { get; } = [];

        public Task HandleAsync(FileEvent fileEvent, CancellationToken cancellationToken)
        {
            int callNumber;

            lock (_gate)
            {
                _calls++;
                callNumber = _calls;
                Events.Add(fileEvent);
            }

            if (_throwOnCallNumbers.Contains(callNumber))
            {
                throw new InvalidOperationException($"Handler failed on call {callNumber}.");
            }

            return Task.CompletedTask;
        }

        public async Task WaitForCallCountAsync(int expectedCount, TimeSpan timeout)
        {
            var startedAt = DateTimeOffset.UtcNow;

            while (DateTimeOffset.UtcNow - startedAt < timeout)
            {
                lock (_gate)
                {
                    if (_calls >= expectedCount)
                    {
                        return;
                    }
                }

                await Task.Delay(10);
            }

            lock (_gate)
            {
                throw new TimeoutException($"Expected at least {expectedCount} calls but observed {_calls}.");
            }
        }
    }
}
