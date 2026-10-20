using Marketeer.Core.Configuration.Models;
using System;

namespace Marketeer.Core.Configuration.Contracts;

public interface IConfigurationService {
    event Action? ConfigurationSaved;
    PluginConfiguration GetConfig();
    void Save();
}