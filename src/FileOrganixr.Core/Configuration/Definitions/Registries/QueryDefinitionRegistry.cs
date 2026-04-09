using System;
using System.Collections.Generic;
using System.Globalization;
using FileOrganixr.Core.Configuration.Definitions.Queries;

namespace FileOrganixr.Core.Configuration.Definitions.Registries;
public sealed class QueryDefinitionRegistry : IQueryDefinitionRegistry
{
    public IQueryDefinition Create(string type, IReadOnlyDictionary<string, object?> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Query type must be provided.", nameof(type));

        var normalizedType = type.Trim();

        if (normalizedType.Equals("regexfilename", StringComparison.OrdinalIgnoreCase))
            return new RegexFileNameQueryDefinition
            {
                Pattern = RequireString(args, "Pattern"),
                IgnoreCase = ReadBoolOrDefault(args, "IgnoreCase", false)
            };

        if (normalizedType.Equals("filesize", StringComparison.OrdinalIgnoreCase))
            return new FileSizeQueryDefinition
            {
                MinSize = ReadDecimalOrDefault(args, "MinSize", 0m),
                MaxSize = ReadDecimalOrDefault(args, "MaxSize", decimal.MaxValue)
            };

        throw new NotSupportedException($"Unknown query type '{normalizedType}'.");
    }

    private static string RequireString(IReadOnlyDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
            throw new InvalidOperationException($"Query argument '{key}' is required.");

        var s = value as string ?? value.ToString();

        if (string.IsNullOrWhiteSpace(s))
            throw new InvalidOperationException($"Query argument '{key}' cannot be empty.");

        return s!;
    }

    private static bool ReadBoolOrDefault(IReadOnlyDictionary<string, object?> args, string key, bool defaultValue)
    {
        if (!args.TryGetValue(key, out var rawValue) || rawValue is null) return defaultValue;

        var text = rawValue as string ?? rawValue.ToString();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException($"Query argument '{key}' cannot be empty.");

        if (bool.TryParse(text, out var parsedValue)) return parsedValue;

        throw new InvalidOperationException($"Query argument '{key}' must be 'true' or 'false'.");
    }

    private static decimal ReadDecimalOrDefault(
        IReadOnlyDictionary<string, object?> args,
        string key,
        decimal defaultValue)
    {
        if (!args.TryGetValue(key, out var rawValue) || rawValue is null) return defaultValue;

        var text = rawValue as string ?? rawValue.ToString();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException($"Query argument '{key}' cannot be empty.");

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue))
            return parsedValue;

        throw new InvalidOperationException(
            $"Query argument '{key}' must be a decimal number using invariant format.");
    }
}
