using Marketeer.API.Command.Contracts;
using Marketeer.API.Features;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.Core.UiInterop.Providers;
using Marketeer.Core.UiInterop.Services;
using Marketeer.UI.UiInterop.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.UiInterop;

public class UiInteropFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IWindowHierarchyProvider, WindowHierarchyProvider>();

        services.AddSingleton<INativeWindowService, NativeWindowService>();
        services.AddSingleton<IWindowTrackerService, WindowTrackerService>();

        services.AddSingleton<ICommand, NativeDevCommand>();
    }

    public void Initialize(IServiceProvider provider) {
        var windowTracker = provider.GetRequiredService<IWindowTrackerService>();
        windowTracker.EnableTracking();
    }
}