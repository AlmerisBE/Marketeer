using Marketeer.Core;
using Marketeer.Features.Command.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Command;

public class CommandFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<CommandDispatcher>();
    }
}