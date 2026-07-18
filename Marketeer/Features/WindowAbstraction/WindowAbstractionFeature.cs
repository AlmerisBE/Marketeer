using Marketeer.Core;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.WindowAbstraction.Commands;
using Marketeer.Features.WindowAbstraction.Contracts;
using Marketeer.Features.WindowAbstraction.Providers;
using Marketeer.Features.WindowAbstraction.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.WindowAbstraction;

public class WindowAbstractionFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Providers
        services.AddSingleton<IWindowHierarchyProvider, WindowHierarchyProvider>();

        // Services
        services.AddSingleton<INativeWindowService, NativeWindowService>();
        services.AddSingleton<IWindowTrackerService, WindowTrackerService>();

        // Commands
        services.AddSingleton<ICommand, NativeDevCommand>();
    }
}