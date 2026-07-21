using Marketeer.API.Features;
using Marketeer.API.Logging.Contracts;
using Marketeer.Core.Logging.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Logging;

public class LoggingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILoggerService, LoggerService>();
    }
}