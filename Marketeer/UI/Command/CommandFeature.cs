using Marketeer.Core.Framework;
using Marketeer.UI.Command.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.UI.Command;

public class CommandFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<CommandDispatcher>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<CommandDispatcher>();
    }
}