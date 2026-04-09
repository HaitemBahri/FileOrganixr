using System.Threading;
using System.Threading.Tasks;

namespace FileOrganixr.Core.Configuration.Providers;
public interface IConfigurationProvider
{
    Task<ConfigurationRoot> LoadAsync(CancellationToken cancellationToken);
}
