using System;
using System.Threading;

namespace FileOrganixr.Core.Configuration.Stores;
public class ConfigurationStore : IConfigurationStore
{
    private readonly Lock _myGate = new();

    public ConfigurationRoot Current
    {
        get
        {
            lock (_myGate)
            {
                if (field is null)
                    throw new InvalidOperationException("Configuration has not been set.");

                return field;
            }
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            lock (_myGate)
            {
                if (field is not null)
                    throw new InvalidOperationException("Configuration has already been set.");

                field = value;
            }
        }
    }
}
