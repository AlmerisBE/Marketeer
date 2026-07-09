using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace BasePlugin.Core;

public static class ServiceExtensions {
    public static IServiceCollection AddPluginFeatures(this IServiceCollection services) {
        var featureModuleType = typeof(IFeatureModule);

        // Scan the current assembly for any class implementing IFeatureModule
        var modules = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => featureModuleType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(Activator.CreateInstance)
            .Cast<IFeatureModule>();

        // Let each feature register its own dependencies
        foreach (var module in modules) {
            module.RegisterServices(services);
        }

        return services;
    }
}