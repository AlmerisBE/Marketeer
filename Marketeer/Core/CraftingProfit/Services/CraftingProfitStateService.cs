using Dalamud.Plugin.Services;
using Marketeer.Core.CraftingProfit.Contracts;
using Marketeer.Core.CraftingProfit.Models;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.CraftingProfit.Services;

public class CraftingProfitStateService : ICraftingProfitStateService, IDisposable {
    private ICraftingProfitRepository repository;
    private ICraftingCostEvaluator evaluator;
    private IMarketPriceCacheService priceProvider;
    private IObjectTable objectTable;
    private IClientState clientState;
    private IFramework framework;
    private ILoggerService logger;

    private ConcurrentDictionary<uint, CraftingProfitResult> evaluations = new();
    private bool isEvaluating;

    public IReadOnlyDictionary<uint, CraftingProfitResult> Evaluations => this.evaluations;

    public CraftingProfitStateService(
        ICraftingProfitRepository repository,
        ICraftingCostEvaluator evaluator,
        IMarketPriceCacheService priceProvider,
        IObjectTable objectTable,
        IClientState clientState,
        IFramework framework,
        ILoggerService logger) {

        this.repository = repository;
        this.evaluator = evaluator;
        this.priceProvider = priceProvider;
        this.objectTable = objectTable;
        this.clientState = clientState;
        this.framework = framework;
        this.logger = logger;

        this.priceProvider.PricesUpdated += this.OnPricesUpdated;
        this.clientState.Login += this.OnLogin;

        // Schedule initial evaluation safely on the framework thread after startup completes
        this.framework.RunOnFrameworkThread(() => {
            if (this.clientState.IsLoggedIn) {
                _ = this.EvaluateAllAsync();
            }
        });
    }

    public void Dispose() {
        this.priceProvider.PricesUpdated -= this.OnPricesUpdated;
        this.clientState.Login -= this.OnLogin;
    }

    private void OnLogin() {
        _ = this.EvaluateAllAsync();
    }

    private void OnPricesUpdated(uint worldId, IEnumerable<uint> updatedItemIds) {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.CurrentWorld.RowId != worldId) {
            return;
        }

        _ = this.EvaluateAllAsync();
    }

    public async Task EvaluateItemAsync(uint itemId) {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null) {
            return;
        }

        var config = this.repository.GetConfig(itemId);
        if (config == null) {
            return;
        }

        var worldId = localPlayer.CurrentWorld.RowId;
        if (worldId == 0) {
            return;
        }

        try {
            var result = await this.evaluator.EvaluateAsync(config, worldId);
            this.evaluations[itemId] = result;
        }
        catch (Exception ex) {
            this.logger.Error(ex, $"Failed to evaluate crafting profit for item {itemId}");
        }
    }

    public async Task EvaluateAllAsync() {
        if (this.isEvaluating) {
            return;
        }

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null) {
            return;
        }

        this.isEvaluating = true;

        try {
            var configs = this.repository.GetAllConfigs();
            var tasks = configs.Select(c => this.EvaluateItemAsync(c.ItemId));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to complete full crafting profit evaluation pass.");
        }
        finally {
            this.isEvaluating = false;
        }
    }
}