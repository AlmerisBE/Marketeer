using Marketeer.Core.Framework;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Universalis.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.UI.Universalis;

public class UniversalisUiFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, UniversalisLocalizationProvider>();
    }
}