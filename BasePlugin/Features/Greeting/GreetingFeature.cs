using BasePlugin.Core;
using BasePlugin.Features.Command.Contracts;
using BasePlugin.Features.Greeting.Commands;
using BasePlugin.Features.Greeting.Contracts;
using BasePlugin.Features.Greeting.Providers;
using BasePlugin.Features.Greeting.Services;
using BasePlugin.Features.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace BasePlugin.Features.Greeting;

public class GreetingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IGreetingService, GreetingService>();
        services.AddSingleton<ICommand, GreetingCommandAction>();
        services.AddSingleton<ILocalizationProvider, GreetingLocalizationProvider>();
    }
}