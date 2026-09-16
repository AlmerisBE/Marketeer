using Marketeer.Core.Framework;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.InventoryBrowser.Providers;
using Marketeer.UI.InventoryBrowser.UI;
using Marketeer.UI.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.UI.InventoryBrowser;

public class InventoryBrowserFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, InventoryBrowserLocalizationProvider>();

        services.AddSingleton<InventoryMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<InventoryMenu>());

        services.AddSingleton<InventoryView>();
    }
}