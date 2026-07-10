using Marketeer.Core;
using Marketeer.Features.MemoryInterop.Providers;
using Marketeer.Features.RetainerTracking.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.MemoryInterop;

public class MemoryInteropFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IRetainerProvider, RetainerProvider>();
    }
}