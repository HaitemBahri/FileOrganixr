


using System;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Runtime.ActionRequests;

namespace FileOrganixr.Core.Execution;
public sealed class ActionExecutor : IActionExecutor
{

    private readonly IActionHandlerRegistry _handlerRegistry;

    public ActionExecutor(IActionHandlerRegistry handlerRegistry)
    {

        ArgumentNullException.ThrowIfNull(handlerRegistry);


        _handlerRegistry = handlerRegistry;
    }

    public Task ExecuteAsync(ActionRequest request, CancellationToken cancellationToken)
    {

        ArgumentNullException.ThrowIfNull(request);


        if (request.RuleSnapshot is null)

            throw new InvalidOperationException("Cannot execute because RuleSnapshot is null.");


        var actionDefinition = request.RuleSnapshot.Action;


        if (actionDefinition is null)

            throw new InvalidOperationException("Cannot execute because Action definition is null.");


        var handler = _handlerRegistry.Resolve(actionDefinition);


        return handler.ExecuteAsync(request, actionDefinition, cancellationToken);
    }
}
