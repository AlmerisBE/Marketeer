using Marketeer.API.Features;
using Marketeer.API.GameData.Contracts;
using Marketeer.Core.GameData.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.GameData;

public class GameDataFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IWorldDataPresenter, WorldDataPresenter>();
    }
}