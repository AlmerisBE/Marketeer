using Marketeer.Core;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Greeting.Commands;
using Marketeer.Features.Greeting.Contracts;
using Marketeer.Features.Greeting.Providers;
using Marketeer.Features.Greeting.Services;
using Marketeer.Features.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Greeting;

public class GreetingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IGreetingService, GreetingService>();
        services.AddSingleton<ICommand, GreetingCommandAction>();
        services.AddSingleton<ILocalizationProvider, GreetingLocalizationProvider>();
    }
}