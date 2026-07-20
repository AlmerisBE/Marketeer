using Marketeer.Core;
using Marketeer.Features.SalesScanner.Contracts;
using Marketeer.Features.SalesScanner.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.SalesScanner;

public class SalesScannerFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ISalesScannerService, SalesScannerService>();
    }
}