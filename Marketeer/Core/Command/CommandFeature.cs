using Marketeer.API.Features;
using Marketeer.Core.Command.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.Command;

public class CommandFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<CommandDispatcher>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<CommandDispatcher>();
    }
}