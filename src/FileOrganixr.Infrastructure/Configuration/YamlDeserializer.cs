using System;
using System.IO;
using FileOrganixr.Core.Configuration.Definitions.Registries;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace FileOrganixr.Infrastructure.Configuration;
public class YamlDeserializer : IDeserializer
{
    private readonly IDeserializer _deserializer;

    public YamlDeserializer(ActionDefinitionRegistry actionRegistry, QueryDefinitionRegistry queryRegistry)
    {
        _deserializer = new DeserializerBuilder()
            .WithTypeConverter(new ActionDefinitionYamlConverter(actionRegistry))
            .WithTypeConverter(new QueryDefinitionYamlConverter(queryRegistry))
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public T Deserialize<T>(string input)
    {
        return _deserializer.Deserialize<T>(input);
    }

    public T Deserialize<T>(TextReader input)
    {
        return _deserializer.Deserialize<T>(input);
    }

    public T Deserialize<T>(IParser parser)
    {
        return _deserializer.Deserialize<T>(parser);
    }

    public object? Deserialize(string input)
    {
        return _deserializer.Deserialize(input);
    }

    public object? Deserialize(TextReader input)
    {
        return _deserializer.Deserialize(input);
    }

    public object? Deserialize(IParser parser)
    {
        return _deserializer.Deserialize(parser);
    }

    public object? Deserialize(string input, Type type)
    {
        return _deserializer.Deserialize(input, type);
    }

    public object? Deserialize(TextReader input, Type type)
    {
        return _deserializer.Deserialize(input, type);
    }

    public object? Deserialize(IParser parser, Type type)
    {
        return _deserializer.Deserialize(parser, type);
    }
}
