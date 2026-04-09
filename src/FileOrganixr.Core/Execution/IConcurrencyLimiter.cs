


using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileOrganixr.Core.Execution;
public interface IConcurrencyLimiter
{

    Task<IDisposable> AcquireAsync(CancellationToken cancellationToken);
}
