using BasePlugin.Core;
using BasePlugin.Features.Logging.Contracts;
using BasePlugin.Features.Logging.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BasePlugin.Features.Logging;

public class LoggingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILoggerService, LoggerService>();
    }
}