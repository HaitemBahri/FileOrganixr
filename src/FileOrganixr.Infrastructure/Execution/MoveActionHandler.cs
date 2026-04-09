


using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Infrastructure.Execution;
public sealed class MoveActionHandler : IActionHandler
{



    public string SupportedActionType => "move";

    public Task ExecuteAsync(ActionRequest request, IActionDefinition actionDefinition,
        CancellationToken cancellationToken)
    {

        ArgumentNullException.ThrowIfNull(request);


        ArgumentNullException.ThrowIfNull(actionDefinition);


        if (actionDefinition is not MoveActionDefinition move)
            throw new ArgumentException("ActionDefinition was not a MoveActionDefinition.", nameof(actionDefinition));


        if (string.IsNullOrWhiteSpace(request.File.FullPath))
            throw new InvalidOperationException("Request file path is missing.");


        if (string.IsNullOrWhiteSpace(move.DestinationPath))
            throw new InvalidOperationException("Move destination folder path is missing.");




        var sourceFilePath = request.File.FullPath;
        var destinationFolderPath = move.DestinationPath;

        cancellationToken.ThrowIfCancellationRequested();


        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException("Source file path must be provided.", nameof(sourceFilePath));


        if (string.IsNullOrWhiteSpace(destinationFolderPath))
            throw new ArgumentException("Destination folder path must be provided.", nameof(destinationFolderPath));


        Directory.CreateDirectory(destinationFolderPath);


        var destinationFilePath = Path.Combine(destinationFolderPath, Path.GetFileName(sourceFilePath));


        File.Move(sourceFilePath, destinationFilePath);


        return Task.CompletedTask;
    }
}
