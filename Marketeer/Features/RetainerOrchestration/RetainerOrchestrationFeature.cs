using Marketeer.Core;
using Marketeer.Features.RetainerOrchestration.Contracts;
using Marketeer.Features.RetainerOrchestration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.RetainerOrchestration;

public class RetainerOrchestrationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IRetainerOrchestratorService, RetainerOrchestratorService>();
    }
}