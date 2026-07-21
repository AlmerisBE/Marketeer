using Marketeer.API.Features;
using Marketeer.Core.Command.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Command;

public class CommandFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<CommandDispatcher>();
    }
}