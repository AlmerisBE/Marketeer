using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core;

public interface IFeatureModule {
    void RegisterServices(IServiceCollection services);
}