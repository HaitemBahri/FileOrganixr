


using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileOrganixr.Core.Execution;
public sealed class SemaphoreConcurrencyLimiter : IConcurrencyLimiter
{

    private readonly SemaphoreSlim _semaphore;

    public SemaphoreConcurrencyLimiter(int maxParallelism)
    {

        if (maxParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelism), "Max parallelism must be greater than zero.");


        _semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {

        await _semaphore.WaitAsync(cancellationToken);


        return new Releaser(_semaphore);
    }

    private sealed class Releaser : IDisposable
    {

        private readonly SemaphoreSlim _semaphore;


        private bool _disposed;

        public Releaser(SemaphoreSlim semaphore)
        {

            _semaphore = semaphore;
        }

        public void Dispose()
        {

            if (_disposed) return;


            _disposed = true;


            _semaphore.Release();
        }
    }
}
