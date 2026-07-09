using BasePlugin.Core;
using BasePlugin.Features.Command.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BasePlugin.Features.Command;

public class CommandFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<CommandDispatcher>();
    }
}