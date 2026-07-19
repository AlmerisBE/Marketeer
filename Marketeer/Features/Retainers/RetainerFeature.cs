using Marketeer.Core;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Retainers.Commands;
using Marketeer.Features.Retainers.Contracts;
using Marketeer.Features.Retainers.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Retainers;

public class RetainerFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IRetainerService, RetainerService>();
        services.AddSingleton<ICommand, RetainerCommandAction>();
        services.AddSingleton<ICommand, RetainerMenuCommandAction>();
    }
}