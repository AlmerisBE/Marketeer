using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Services;
using Marketeer.Core.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Configuration;

public class ConfigurationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IConfigurationService, ConfigurationService>();
    }
}