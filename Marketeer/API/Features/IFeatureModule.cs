using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.API.Features;

public interface IFeatureModule {
    void RegisterServices(IServiceCollection services);
}