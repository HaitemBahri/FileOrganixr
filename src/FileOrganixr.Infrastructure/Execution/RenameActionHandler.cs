


using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Infrastructure.Execution;
public sealed class RenameActionHandler : IActionHandler
{



    public string SupportedActionType => "rename";

    public Task ExecuteAsync(ActionRequest request, IActionDefinition actionDefinition,
        CancellationToken cancellationToken)
    {

        ArgumentNullException.ThrowIfNull(request);


        ArgumentNullException.ThrowIfNull(actionDefinition);


        if (actionDefinition is not RenameActionDefinition rename)
            throw new ArgumentException("ActionDefinition was not a RenameActionDefinition.", nameof(actionDefinition));


        if (string.IsNullOrWhiteSpace(request.File.FullPath))
            throw new InvalidOperationException("Request file path is missing.");


        if (string.IsNullOrWhiteSpace(rename.Pattern))
            throw new InvalidOperationException("Rename NewFileName is missing.");




        cancellationToken.ThrowIfCancellationRequested();

        var sourceFilePath = request.File.FullPath;

        var newFileName = rename.Pattern;


        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException("Source file path must be provided.", nameof(sourceFilePath));


        if (string.IsNullOrWhiteSpace(newFileName))
            throw new ArgumentException("New file name must be provided.", nameof(newFileName));


        var destinationFilePath = Path.Combine(Path.GetDirectoryName(sourceFilePath) ?? string.Empty, newFileName);


        File.Move(sourceFilePath, destinationFilePath);


        return Task.CompletedTask;
    }
}
