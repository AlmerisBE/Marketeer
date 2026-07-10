using Marketeer.Features.Configuration.Models;

namespace Marketeer.Features.Configuration.Contracts;

public interface IConfigurationService {
    PluginConfiguration GetConfig();
    void Save();
}