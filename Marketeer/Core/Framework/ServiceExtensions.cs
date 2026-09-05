using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Marketeer.Core.Framework;

public static class ServiceExtensions {
    public static IServiceCollection AddPluginFeatures(this IServiceCollection services) {
        var featureModuleType = typeof(IFeatureModule);

        var modules = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => featureModuleType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(Activator.CreateInstance)
            .Cast<IFeatureModule>();

        foreach (var module in modules) {
            module.RegisterServices(services);
            services.AddSingleton(typeof(IFeatureModule), module);
        }

        return services;
    }
}