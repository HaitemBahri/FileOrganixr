using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileOrganixr.Infrastructure.Configuration;
public sealed class UserSettingsResolver : ISettingsResolver
{
    public const string ConfigPathEnvironmentVariable = "FILEORGANIXR_CONFIG_PATH";

    private const string SettingsFileName = "settings.json";
    private const string SettingsFolderName = "FileOrganixr";
    private const string SettingsKeyConfigFilePath = "configFilePath";

    public AppSettings ResolveSettings()
    {
        var envPath = Environment.GetEnvironmentVariable(ConfigPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return new AppSettings
            {
                ConfigFilePath = NormalizePath(envPath)
            };
        }

        var settingsPath = GetSettingsFilePath();
        var settings = LoadOrCreateSettings(settingsPath);

        if (!string.IsNullOrWhiteSpace(settings.ConfigFilePath))
        {
            return new AppSettings
            {
                ConfigFilePath = NormalizePath(settings.ConfigFilePath)
            };
        }

        var defaultConfigFilePath = GetDefaultConfigFilePath();
        EnsureDefaultConfigExists(defaultConfigFilePath);
        return new AppSettings
        {
            ConfigFilePath = NormalizePath(defaultConfigFilePath)
        };
    }

    private static string GetDefaultConfigFilePath()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documents))
        {
            documents = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(documents, SettingsFolderName, "config.yaml");
    }

    private static string GetSettingsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(appData, SettingsFolderName, SettingsFileName);
    }

    private static SettingsFileModel LoadOrCreateSettings(string settingsFilePath)
    {
        if (!File.Exists(settingsFilePath))
        {
            var model = CreateDefaultSettingsModel();

            WriteSettings(settingsFilePath, model);
            return model;
        }

        var json = File.ReadAllText(settingsFilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            var model = CreateDefaultSettingsModel();

            WriteSettings(settingsFilePath, model);
            return model;
        }

        try
        {
            var model = JsonSerializer.Deserialize<SettingsFileModel>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            return model ?? new SettingsFileModel();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse settings file '{settingsFilePath}'. " +
                $"Ensure it contains valid JSON with '{SettingsKeyConfigFilePath}'.",
                ex);
        }
    }

    private static string NormalizePath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
        return Path.GetFullPath(expanded);
    }

    private static SettingsFileModel CreateDefaultSettingsModel()
    {
        var defaultConfigFilePath = GetDefaultConfigFilePath();
        EnsureDefaultConfigExists(defaultConfigFilePath);

        return new SettingsFileModel
        {
            ConfigFilePath = defaultConfigFilePath
        };
    }

    private static void EnsureDefaultConfigExists(string configFilePath)
    {
        if (File.Exists(configFilePath)) return;

        var directoryPath = Path.GetDirectoryName(configFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documents))
        {
            documents = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        var safeFolderPath = documents.Replace("'", "''");
        var yaml = $"""
                    SchemaVersion: 1
                    Folders:
                      - Name: Inbox
                        Path: '{safeFolderPath}'
                        Rules: []
                    """;

        File.WriteAllText(configFilePath, yaml);
    }

    private static void WriteSettings(string settingsFilePath, SettingsFileModel model)
    {
        var directoryPath = Path.GetDirectoryName(settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(
            model,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(settingsFilePath, json);
    }

    private sealed class SettingsFileModel
    {
        [JsonPropertyName("configFilePath")]
        public string ConfigFilePath { get; init; } = string.Empty;
    }
}
