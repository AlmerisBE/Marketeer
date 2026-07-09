using Microsoft.Extensions.DependencyInjection;

namespace BasePlugin.Core;

public interface IFeatureModule {
    void RegisterServices(IServiceCollection services);
}