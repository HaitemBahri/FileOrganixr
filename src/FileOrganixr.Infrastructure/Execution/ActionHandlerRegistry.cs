


using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Execution;

namespace FileOrganixr.Infrastructure.Execution;
public sealed class ActionHandlerRegistry : IActionHandlerRegistry
{

    private readonly Dictionary<string, IActionHandler> _handlers;

    public ActionHandlerRegistry(IEnumerable<IActionHandler> handlers)
    {

        ArgumentNullException.ThrowIfNull(handlers);


        _handlers = new Dictionary<string, IActionHandler>(StringComparer.OrdinalIgnoreCase);


        foreach (var handler in handlers)
        {

            if (handler is null) continue;


            _handlers[handler.SupportedActionType] = handler;
        }
    }

    public IActionHandler Resolve(IActionDefinition actionDefinition)
    {

        ArgumentNullException.ThrowIfNull(actionDefinition);


        if (string.IsNullOrWhiteSpace(actionDefinition.Type))

            throw new ArgumentException("ActionDefinition.Type must be provided.", nameof(actionDefinition));


        if (_handlers.TryGetValue(actionDefinition.Type, out var handler))

            return handler;


        throw new NotSupportedException($"No handler registered for action type '{actionDefinition.Type}'.");
    }
}
