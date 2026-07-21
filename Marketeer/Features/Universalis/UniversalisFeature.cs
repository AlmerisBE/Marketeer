using Marketeer.Core;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.Universalis.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace Marketeer.Features.Universalis;

public class UniversalisFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Registering a shared Singleton HttpClient prevents socket exhaustion
        // while avoiding the heavy Microsoft.Extensions.Http package dependency.
        services.AddSingleton<HttpClient>();

        // Register the Universalis service that will consume the HttpClient
        services.AddSingleton<IServerPriceProvider, UniversalisClientService>();
    }
}