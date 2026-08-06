using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.API.Features;

public interface IFeatureModule {
    void RegisterServices(IServiceCollection services);

    void Initialize(IServiceProvider provider) { }
}