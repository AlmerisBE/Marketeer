using Marketeer.Core.Configuration.Models;

namespace Marketeer.Core.Configuration.Contracts;

public interface IConfigurationService {
    PluginConfiguration GetConfig();
    void Save();
}