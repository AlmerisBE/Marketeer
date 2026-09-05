using Marketeer.Core.Framework;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.UiInterop.Commands;
using Marketeer.UI.UiInterop.Contracts;
using Marketeer.UI.UiInterop.Providers;
using Marketeer.UI.UiInterop.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.UI.UiInterop;

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