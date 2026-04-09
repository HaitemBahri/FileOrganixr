namespace FileOrganixr.Core.Configuration.Stores;
public interface IConfigurationStore
{
    ConfigurationRoot Current { get; set; }
}
