using Marketeer.Core;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.GameData.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.GameData;

public class GameDataFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IWorldDataPresenter, WorldDataPresenter>();
    }
}