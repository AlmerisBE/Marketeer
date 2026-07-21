using Marketeer.Core;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.UndercutTracking.Commands;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Providers;
using Marketeer.Features.UndercutTracking.Services;
using Marketeer.Features.UndercutTracking.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.UndercutTracking;

public class UndercutTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, UndercutLocalizationProvider>();
        services.AddSingleton<IRetainerStateService, RetainerStateService>();
        services.AddSingleton<ICompetitionStateService, CompetitionStateService>();
        services.AddSingleton<IUndercutMonitorService, UndercutMonitorService>();

        // Single UI Tab registration preserving CompetitionTab
        services.AddSingleton<CompetitionTab>();
        services.AddSingleton<IDashboardTab>(provider => provider.GetRequiredService<CompetitionTab>());

        // Commands
        services.AddSingleton<ICommand, PriceCommand>();
    }
}