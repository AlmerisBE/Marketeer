using Marketeer.API.Command.Contracts;
using Marketeer.API.Features;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.UiInterop.Providers;
using Marketeer.Core.UiInterop.Services;
using Marketeer.UI.UiInterop.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.UiInterop;

public class UiInteropFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Providers
        services.AddSingleton<IWindowHierarchyProvider, WindowHierarchyProvider>();

        // Core Services
        services.AddSingleton<INativeWindowService, NativeWindowService>();
        services.AddSingleton<IWindowTrackerService, WindowTrackerService>();

        // UI Commands
        services.AddSingleton<ICommand, NativeDevCommand>();
    }
}