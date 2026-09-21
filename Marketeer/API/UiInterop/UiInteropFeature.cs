using Marketeer.API.UiInterop.Commands;
using Marketeer.API.UiInterop.Contracts;
using Marketeer.API.UiInterop.Providers;
using Marketeer.API.UiInterop.Services;
using Marketeer.Core.Framework;
using Marketeer.UI.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.API.UiInterop;

public class UiInteropFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IWindowHierarchyProvider, WindowHierarchyProvider>();

        services.AddSingleton<INativeWindowService, NativeWindowService>();
        services.AddSingleton<IWindowTrackerService, WindowTrackerService>();

        services.AddSingleton<ICommand, NativeDevCommand>();
    }

    public void Initialize(IServiceProvider provider) {
        //var windowTracker = provider.GetRequiredService<IWindowTrackerService>();
        //windowTracker.EnableTracking();
    }
}