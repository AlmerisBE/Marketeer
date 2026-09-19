using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketeer.Core.MarketPricing.Services;

public class MarketPriceCacheService : IMarketPriceCacheService {
    private IUniversalisClient universalisClient;
    private IConfigurationService configService;
    private IFramework framework;
    private ILoggerService logger;

    private ConcurrentDictionary<string, CachedPriceData> cache = new();

    public event Action<uint, IEnumerable<uint>>? PricesUpdated;

    public MarketPriceCacheService(
        IUniversalisClient universalisClient,
        IConfigurationService configService,
        IFramework framework,
        ILoggerService logger) {

        this.universalisClient = universalisClient;
        this.configService = configService;
        this.framework = framework;
        this.logger = logger;
    }

    public async Task<LowestPriceResult?> GetLowestPriceAsync(uint itemId, uint worldId, bool bypassCache = false) {
        var results = await this.GetLowestPricesAsync(new[] { itemId }, worldId, bypassCache);
        return results.OrderBy(r => r.Price).FirstOrDefault();
    }

    public async Task ForceRefreshAsync(IEnumerable<uint> itemIds, uint worldId) {
        await this.GetLowestPricesAsync(itemIds, worldId, true);
    }

    public async Task<IReadOnlyList<LowestPriceResult>> GetLowestPricesAsync(IEnumerable<uint> itemIds, uint worldId, bool bypassCache = false) {
        var results = new List<LowestPriceResult>();
        var idsToFetch = new List<uint>();
        var cacheDuration = TimeSpan.FromMinutes(this.configService.GetConfig().UniversalisCacheMinutes);

        foreach (var id in itemIds.Distinct()) {
            var key = $"{worldId}_{id}";
            if (!bypassCache && this.cache.TryGetValue(key, out var cached) && (DateTime.UtcNow - cached.LastUpdated) < cacheDuration) {
                results.AddRange(cached.Prices);
            }
            else idsToFetch.Add(id);
        }

        if (idsToFetch.Count > 0) {
            try {
                var fetchedPrices = await this.universalisClient.FetchPricesAsync(idsToFetch, worldId);
                var groupedPrices = fetchedPrices.GroupBy(p => p.ItemId).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var id in idsToFetch) {
                    var key = $"{worldId}_{id}";
                    if (groupedPrices.TryGetValue(id, out var prices)) {
                        // Protect fresh local market data (e.g., < 10 mins old) from being overwritten by older Universalis data
                        if (this.cache.TryGetValue(key, out var existing) && existing.Source == PriceSourceType.LocalScanner && (DateTime.UtcNow - existing.LastUpdated).TotalMinutes < 10) {
                            results.AddRange(existing.Prices);
                        }
                        else {
                            this.cache[key] = new CachedPriceData { ItemId = id, Prices = prices, LastUpdated = DateTime.UtcNow, Source = PriceSourceType.Universalis };
                            results.AddRange(prices);
                        }
                    }
                    else {
                        // Cache empty response to prevent spamming the API
                        this.cache[key] = new CachedPriceData { ItemId = id, LastUpdated = DateTime.UtcNow, Source = PriceSourceType.Universalis };
                    }
                }

                if (fetchedPrices.Count > 0) {
                    _ = this.framework.RunOnFrameworkThread(() => {
                        this.PricesUpdated?.Invoke(worldId, idsToFetch);
                    });
                }
            }
            catch (Exception ex) {
                this.logger.Error(ex, $"Failed to fetch fallback API prices for world {worldId}.");
            }
        }

        return results;
    }

    public void UpdateLocalPrices(uint itemId, uint worldId, IReadOnlyList<LowestPriceResult> prices) {
        var key = $"{worldId}_{itemId}";

        this.cache[key] = new CachedPriceData {
            ItemId = itemId,
            Prices = prices.ToList(),
            LastUpdated = DateTime.UtcNow,
            Source = PriceSourceType.LocalScanner
        };

        _ = this.framework.RunOnFrameworkThread(() => {
            this.PricesUpdated?.Invoke(worldId, new[] { itemId });
        });

        this.logger.Debug($"[MarketPriceCache] Local cache updated instantly for item {itemId} with {prices.Count} live entries.");
    }
}