using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Definitions.Actions;

namespace FileOrganixr.Core.Configuration.Definitions.Registries;
public sealed class ActionDefinitionRegistry : IActionDefinitionRegistry
{
    public IActionDefinition Create(string type, IReadOnlyDictionary<string, object?> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Action type must be provided.", nameof(type));

        var normalizedType = type.Trim();

        if (normalizedType.Equals("move", StringComparison.OrdinalIgnoreCase))
            return new MoveActionDefinition { DestinationPath = RequireString(args, "DestinationPath") };

        if (normalizedType.Equals("delete", StringComparison.OrdinalIgnoreCase))
            return new DeleteActionDefinition();

        if (normalizedType.Equals("rename", StringComparison.OrdinalIgnoreCase))
            return new RenameActionDefinition { Pattern = RequireString(args, "Pattern") };

        throw new NotSupportedException($"Unknown action type '{normalizedType}'.");
    }

    private static string RequireString(IReadOnlyDictionary<string, object?> args, string key)
    {
        var value = args.GetValueOrDefault(key);
        if (value == null)
            throw new InvalidOperationException($"Action argument '{key}' does not exist.");

        var valueString = value as string ?? value.ToString();
        if (string.IsNullOrWhiteSpace(valueString))
            throw new InvalidOperationException($"Action argument '{key}' cannot be empty.");

        return valueString!;
    }
}
