using BasePlugin.Features.Configuration.Models;

namespace BasePlugin.Features.Configuration.Contracts;

public interface IConfigurationService {
    PluginConfiguration GetConfig();
    void Save();
}