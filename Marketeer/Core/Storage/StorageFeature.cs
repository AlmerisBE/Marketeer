using Marketeer.Core.Framework;
using Marketeer.Core.Storage.Contracts;
using Marketeer.Core.Storage.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.Storage;

public class StorageFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IDatabaseService, DatabaseService>();
    }

    public void Initialize(IServiceProvider provider) {
        var dbService = provider.GetRequiredService<IDatabaseService>();
        dbService.InitializeDatabase();
    }
}