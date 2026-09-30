using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Services;
using Marketeer.Core.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.API.Universalis;

public class UniversalisFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IUniversalisClient, UniversalisClientService>();
    }
}