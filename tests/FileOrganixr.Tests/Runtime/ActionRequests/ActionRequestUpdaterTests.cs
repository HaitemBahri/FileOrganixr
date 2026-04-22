using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;

namespace FileOrganixr.Tests.Runtime.ActionRequests;
public sealed class ActionRequestUpdaterTests
{
    [Fact]
    public void AddStatus_RaisesStatusChanged_ForSuccessfulTransitions()
    {
        var updater = new ActionRequestUpdater();
        var request = CreateRequest();
        var transitions = new List<ActionRequestStatus>();

        updater.StatusChanged += (_, updated) => transitions.Add(updated.CurrentStatus);

        updater.AddStatus(request, ActionRequestStatus.Detected);
        updater.AddStatus(request, ActionRequestStatus.NoRuleMatched);

        Assert.Equal(
            [ActionRequestStatus.Detected, ActionRequestStatus.NoRuleMatched],
            transitions);
    }

    [Fact]
    public void AddStatus_DoesNotRaiseStatusChanged_WhenTransitionIsInvalid()
    {
        var updater = new ActionRequestUpdater();
        var request = CreateRequest();
        var eventCount = 0;

        updater.StatusChanged += (_, _) => eventCount++;

        updater.AddStatus(request, ActionRequestStatus.Detected);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            updater.AddStatus(request, ActionRequestStatus.Processing));

        Assert.Contains("Invalid ActionRequest transition", exception.Message);
        Assert.Equal(1, eventCount);
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
}
