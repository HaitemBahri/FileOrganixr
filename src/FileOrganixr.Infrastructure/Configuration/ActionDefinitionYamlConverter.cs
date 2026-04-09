using System;
using System.Collections.Generic;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Registries;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace FileOrganixr.Infrastructure.Configuration;
public sealed class ActionDefinitionYamlConverter : IYamlTypeConverter
{
    private readonly ActionDefinitionRegistry _registry;

    public ActionDefinitionYamlConverter(ActionDefinitionRegistry registry)
    {
        _registry = registry;
    }

    public bool Accepts(Type type)
    {
        return type == typeof(IActionDefinition);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer _)
    {
        parser.Consume<MappingStart>();

        var map = ReadScalarMap(parser);

        if (!map.TryGetValue("type", out var rawType) || rawType is null)
            throw new YamlException("Action definition must include a 'type' field (e.g., type: Move).");

        var discriminator = rawType as string ?? rawType.ToString() ?? string.Empty;

        map.Remove("type");

        return _registry.Create(discriminator, map);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        throw new NotSupportedException("Serialising IActionDefinition to YAML is not supported in v1.");
    }

    private static Dictionary<string, object?> ReadScalarMap(IParser parser)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>().Value;

            if (!parser.TryConsume<Scalar>(out var scalar))
                throw new YamlException("Action arguments must be scalar values in v1.");

            map[key] = scalar.Value;
        }

        return map;
    }
}
