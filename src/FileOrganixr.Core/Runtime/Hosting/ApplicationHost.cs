using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileOrganixr.Core.Configuration;
using FileOrganixr.Core.Configuration.Providers;
using FileOrganixr.Core.Configuration.Stores;
using FileOrganixr.Core.Configuration.Validators;
using FileOrganixr.Core.Execution;
using FileOrganixr.Core.Watchers.FileEvents;
using FileOrganixr.Core.Watchers.FolderWatchers;

namespace FileOrganixr.Core.Runtime.Hosting;
public sealed class ApplicationHost : IApplicationHost, IDisposable
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly IConfigurationStore _configurationStore;
    private readonly IExecutionOrchestrator _executionOrchestrator;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly IConfigurationRootValidator _rootValidator;
    private readonly IFileWatcherService _watcherService;
    private bool _isDisposed;
    private bool _isInitialized;
    private bool _isRunning;

    public ApplicationHost(
        IConfigurationProvider configurationProvider,
        IConfigurationRootValidator rootValidator,
        IConfigurationStore configurationStore,
        IExecutionOrchestrator executionOrchestrator,
        IFileWatcherService watcherService)
    {
        ArgumentNullException.ThrowIfNull(configurationProvider);
        ArgumentNullException.ThrowIfNull(rootValidator);
        ArgumentNullException.ThrowIfNull(configurationStore);
        ArgumentNullException.ThrowIfNull(executionOrchestrator);
        ArgumentNullException.ThrowIfNull(watcherService);

        _configurationProvider = configurationProvider;
        _rootValidator = rootValidator;
        _configurationStore = configurationStore;
        _executionOrchestrator = executionOrchestrator;
        _watcherService = watcherService;
    }

    public bool IsRunning => _isRunning;

    public void Dispose()
    {
        if (_isDisposed) return;

        StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        _isDisposed = true;
        _lifecycleGate.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();

            if (_isRunning) return;

            if (!_isInitialized)
            {
                var root = await _configurationProvider.LoadAsync(cancellationToken);
                var validation = _rootValidator.Validate(root);

                if (validation.Errors.Count > 0)
                {
                    throw new InvalidOperationException(BuildValidationMessage(validation));
                }

                _configurationStore.Current = root;
                _isInitialized = true;
            }

            _executionOrchestrator.Start();
            try
            {
                _watcherService.Start();
            }
            catch
            {
                _executionOrchestrator.Stop();
                throw;
            }

            _isRunning = true;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (!_isRunning) return;

            _watcherService.Stop();
            _executionOrchestrator.Stop();

            _isRunning = false;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private static string BuildValidationMessage(ValidationResult validation)
    {
        var sb = new StringBuilder("Configuration validation failed:");

        foreach (var error in validation.Errors)
        {
            sb.AppendLine();
            sb.Append("- ");
            sb.Append(error.Path);
            sb.Append(": ");
            sb.Append(error.Message);
        }

        return sb.ToString();
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(ApplicationHost));
    }
}
