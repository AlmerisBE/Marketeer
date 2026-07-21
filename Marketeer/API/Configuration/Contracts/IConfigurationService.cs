using Marketeer.API.Configuration.Models;

namespace Marketeer.API.Configuration.Contracts;

public interface IConfigurationService {
    PluginConfiguration GetConfig();
    void Save();
}