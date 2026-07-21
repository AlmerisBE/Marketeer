using Marketeer.API.Features;
using Marketeer.API.Universalis.Contracts;
using Marketeer.Core.Universalis.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace Marketeer.Core.Universalis;

public class UniversalisFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IServerPriceProvider, UniversalisClientService>();
    }
}