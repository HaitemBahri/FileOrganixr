using System;
using System.Collections.Generic;
using System.Threading;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Stores;

namespace FileOrganixr.Core.Watchers.FolderWatchers;
public sealed class FolderDefinitionResolver : IFolderDefinitionResolver
{
    private readonly IConfigurationStore _configurationStore;
    private readonly Lock _gate = new();
    private Dictionary<string, FolderDefinition>? _byPath;

    public FolderDefinitionResolver(IConfigurationStore configurationStore)
    {
        ArgumentNullException.ThrowIfNull(configurationStore);
        _configurationStore = configurationStore;
    }

    public FolderDefinition Resolve(string watchedFolderPath)
    {
        if (string.IsNullOrWhiteSpace(watchedFolderPath))
            throw new ArgumentException("Watched folder path must be provided.", nameof(watchedFolderPath));

        var key = NormaliseFolderPath(watchedFolderPath);
        var byPath = EnsureFolderLookup();

        if (byPath.TryGetValue(key, out var folder)) return folder;

        throw new KeyNotFoundException($"No FolderDefinition is configured for watched path '{watchedFolderPath}'.");
    }

    private Dictionary<string, FolderDefinition> EnsureFolderLookup()
    {
        lock (_gate)
        {
            if (_byPath is not null)
            {
                return _byPath;
            }

            var config = _configurationStore.Current;
            var byPath = new Dictionary<string, FolderDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in config.Folders ?? [])
            {
                if (folder is null) continue;
                if (string.IsNullOrWhiteSpace(folder.Path)) continue;

                var normalised = NormaliseFolderPath(folder.Path);
                byPath[normalised] = folder;
            }

            _byPath = byPath;
            return _byPath;
        }
    }

    private static string NormaliseFolderPath(string path)
    {
        var trimmed = path.Trim();

        var slashesNormalised = trimmed.Replace('/', '\\');

        var withoutTrailing = slashesNormalised.TrimEnd('\\');

        return withoutTrailing;
    }
}
