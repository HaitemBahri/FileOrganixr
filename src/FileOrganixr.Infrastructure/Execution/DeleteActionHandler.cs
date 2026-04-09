


using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Infrastructure.Execution;
public sealed class DeleteActionHandler : IActionHandler
{



    public string SupportedActionType => "delete";

    public Task ExecuteAsync(ActionRequest request, IActionDefinition actionDefinition,
        CancellationToken cancellationToken)
    {

        ArgumentNullException.ThrowIfNull(request);


        ArgumentNullException.ThrowIfNull(actionDefinition);


        if (string.IsNullOrWhiteSpace(request.File.FullPath))
            throw new InvalidOperationException("Request file path is missing.");




        cancellationToken.ThrowIfCancellationRequested();

        var filePath = request.File.FullPath;


        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must be provided.", nameof(filePath));


        if (!File.Exists(filePath)) return Task.CompletedTask;


        File.Delete(filePath);


        return Task.CompletedTask;
    }
}
