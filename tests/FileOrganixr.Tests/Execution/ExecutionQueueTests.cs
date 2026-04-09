using System;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Tests.Execution;
public sealed class ExecutionQueueTests
{
    [Fact]
    public async Task DequeueAsync_ReturnsItemsInFifoOrder()
    {
        var queue = new ExecutionQueue();
        var first = CreateRequest("first.txt");
        var second = CreateRequest("second.txt");

        queue.Enqueue(first);
        queue.Enqueue(second);

        var dequeuedFirst = await queue.DequeueAsync(CancellationToken.None);
        var dequeuedSecond = await queue.DequeueAsync(CancellationToken.None);

        Assert.Same(first, dequeuedFirst);
        Assert.Same(second, dequeuedSecond);
    }

    [Fact]
    public async Task DequeueAsync_ThrowsWhenCancellationIsRequested()
    {
        var queue = new ExecutionQueue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.DequeueAsync(cts.Token));
    }

    [Fact]
    public void Enqueue_ThrowsWhenRequestIsNull()
    {
        var queue = new ExecutionQueue();

        Assert.Throws<ArgumentNullException>(() => queue.Enqueue(null!));
    }

    private static ActionRequest CreateRequest(string fileName)
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
                FullPath = $@"C:\Inbox\{fileName}",
                FileName = fileName,
                FileNameWithoutExtension = fileName.Replace(".txt", string.Empty, StringComparison.Ordinal),
                Extension = ".txt",
                SizeBytes = 1
            }
        };

        request.AddStatus(ActionRequestStatus.Detected);
        request.AddStatus(ActionRequestStatus.RuleMatched);
        request.AddStatus(ActionRequestStatus.Queued);
        return request;
    }
}
