using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration;
using FileOrganixr.Core.Configuration.Providers;
using YamlDotNet.Serialization;

namespace FileOrganixr.Infrastructure.Configuration;
public sealed class YamlFileConfigurationProvider : IConfigurationProvider
{
    private readonly IDeserializer _deserializer;
    private readonly ISettingsResolver _settingsResolver;

    public YamlFileConfigurationProvider(IDeserializer deserializer, ISettingsResolver settingsResolver)
    {
        ArgumentNullException.ThrowIfNull(deserializer);
        ArgumentNullException.ThrowIfNull(settingsResolver);

        _deserializer = deserializer;
        _settingsResolver = settingsResolver;
    }

    public async Task<ConfigurationRoot> LoadAsync(CancellationToken cancellationToken)
    {
        var settings = _settingsResolver.ResolveSettings();
        var configFilePath = settings.ConfigFilePath;

        if (string.IsNullOrWhiteSpace(configFilePath))
            throw new InvalidOperationException("Resolved settings does not contain a valid config file path.");

        if (!File.Exists(configFilePath))
            throw new FileNotFoundException("Configuration file was not found.", configFilePath);

        var yamlText = await File.ReadAllTextAsync(configFilePath, cancellationToken);

        var config = _deserializer.Deserialize<ConfigurationRoot>(yamlText);

        return config;
    }
}
