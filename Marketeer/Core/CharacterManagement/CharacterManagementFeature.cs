using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.CharacterManagement.Services;
using Marketeer.Core.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.CharacterManagement;

public class CharacterManagementFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Core Tracking
        services.AddSingleton<ICharacterTrackerService, CharacterTrackerService>();
        services.AddSingleton<IRetainerTrackerService, RetainerTrackerService>();
    }
}