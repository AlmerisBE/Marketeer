using Marketeer.Core;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.Logging.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Logging;

public class LoggingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILoggerService, LoggerService>();
    }
}