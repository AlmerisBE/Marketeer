using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.Framework;

public interface IFeatureModule {
    void RegisterServices(IServiceCollection services);

    void Initialize(IServiceProvider provider) { }
}