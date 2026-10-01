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
using System.Threading;
using System.Threading.Tasks;

namespace Marketeer.Core.MarketPricing.Services;

public class MarketPriceCacheService : IMarketPriceCacheService, IDisposable {
    private IUniversalisClient universalisClient;
    private IUniversalisUpdateMutator updateMutator;
    private IConfigurationService configService;
    private IFramework framework;
    private ILoggerService logger;

    private ConcurrentDictionary<string, CachedPriceData> cache = new();
    private SemaphoreSlim fetchSemaphore = new SemaphoreSlim(1, 1);

    public event Action<uint, IEnumerable<uint>>? PricesUpdated;

    public MarketPriceCacheService(
        IUniversalisClient universalisClient,
        IUniversalisUpdateMutator updateMutator,
        IConfigurationService configService,
        IFramework framework,
        ILoggerService logger) {

        this.universalisClient = universalisClient;
        this.updateMutator = updateMutator;
        this.configService = configService;
        this.framework = framework;
        this.logger = logger;
    }

    public async Task<MarketItemPricing?> GetPricingAsync(uint itemId, uint worldId, bool bypassCache = false) {
        var results = await this.GetPricingsAsync(new[] { itemId }, worldId, bypassCache);
        return results.FirstOrDefault();
    }

    public async Task ForceRefreshAsync(IEnumerable<uint> itemIds, uint worldId) {
        await this.GetPricingsAsync(itemIds, worldId, true);
    }

    public async Task<IReadOnlyList<MarketItemPricing>> GetPricingsAsync(IEnumerable<uint> itemIds, uint worldId, bool bypassCache = false) {
        var results = new List<MarketItemPricing>();
        var distinctIds = itemIds.Distinct().ToList();
        var idsToFetch = new List<uint>();
        var cacheDuration = TimeSpan.FromMinutes(this.configService.GetConfig().UniversalisCacheMinutes);

        // Phase 1: Optimistic read (No locks, extremely fast)
        foreach (var id in distinctIds) {
            var key = $"{worldId}_{id}";
            if (!bypassCache && this.cache.TryGetValue(key, out var cached) && (DateTime.UtcNow - cached.LastUpdated) < cacheDuration) {
                results.Add(cached.Pricing);
            }
            else idsToFetch.Add(id);
        }

        if (idsToFetch.Count > 0) {
            await this.fetchSemaphore.WaitAsync();
            try {
                // Phase 2: Double-Checked Locking
                var missingIds = new List<uint>();
                foreach (var id in idsToFetch) {
                    var key = $"{worldId}_{id}";
                    if (!bypassCache && this.cache.TryGetValue(key, out var cached) && (DateTime.UtcNow - cached.LastUpdated) < cacheDuration) {
                        results.Add(cached.Pricing);
                    }
                    else missingIds.Add(id);
                }

                if (missingIds.Count > 0) {
                    this.updateMutator.SetUpdating(true);

                    try {
                        var baseItemIds = missingIds.Select(id => id > 1000000u ? id - 1000000u : id).Distinct().ToList();
                        var fetchedData = await this.universalisClient.FetchDataAsync(baseItemIds, worldId);

                        foreach (var data in fetchedData) {
                            var nqId = data.BaseItemId;
                            var hqId = data.BaseItemId + 1000000u;
                            var nqKey = $"{worldId}_{nqId}";
                            var hqKey = $"{worldId}_{hqId}";

                            if (!this.cache.TryGetValue(nqKey, out var nqExisting) || nqExisting.Source != PriceSourceType.LocalScanner || (DateTime.UtcNow - nqExisting.LastUpdated).TotalMinutes >= 10) {
                                var nqPricing = new MarketItemPricing { ItemId = nqId, Listings = data.Listings.Where(l => !l.IsHq).ToList(), AverageSalePrice = data.AveragePriceNq, SalesPerDay = data.NqSaleVelocity };
                                this.cache[nqKey] = new CachedPriceData { ItemId = nqId, Pricing = nqPricing, LastUpdated = DateTime.UtcNow, Source = PriceSourceType.Universalis };
                            }
                            else {
                                nqExisting.Pricing.AverageSalePrice = data.AveragePriceNq;
                                nqExisting.Pricing.SalesPerDay = data.NqSaleVelocity;
                            }

                            if (!this.cache.TryGetValue(hqKey, out var hqExisting) || hqExisting.Source != PriceSourceType.LocalScanner || (DateTime.UtcNow - hqExisting.LastUpdated).TotalMinutes >= 10) {
                                var hqPricing = new MarketItemPricing { ItemId = hqId, Listings = data.Listings.Where(l => l.IsHq).ToList(), AverageSalePrice = data.AveragePriceHq, SalesPerDay = data.HqSaleVelocity };
                                this.cache[hqKey] = new CachedPriceData { ItemId = hqId, Pricing = hqPricing, LastUpdated = DateTime.UtcNow, Source = PriceSourceType.Universalis };
                            }
                            else {
                                hqExisting.Pricing.AverageSalePrice = data.AveragePriceHq;
                                hqExisting.Pricing.SalesPerDay = data.HqSaleVelocity;
                            }
                        }

                        foreach (var id in missingIds) {
                            var key = $"{worldId}_{id}";
                            if (this.cache.TryGetValue(key, out var cached)) results.Add(cached.Pricing);
                            else {
                                var emptyPricing = new MarketItemPricing { ItemId = id };
                                this.cache[key] = new CachedPriceData { ItemId = id, Pricing = emptyPricing, LastUpdated = DateTime.UtcNow, Source = PriceSourceType.Universalis };
                                results.Add(emptyPricing);
                            }
                        }

                        if (fetchedData.Count > 0) _ = this.framework.RunOnFrameworkThread(() => this.PricesUpdated?.Invoke(worldId, missingIds));

                        this.updateMutator.RecordSuccessfulUpdate();
                    }
                    catch (Exception ex) {
                        this.logger.Warning($"Failed to fetch fallback API prices for world {worldId}. Reason: {ex.Message}");
                        this.updateMutator.SetUpdating(false);

                        // Fallback: Populate missing IDs with empty pricing objects so caller receives a valid collection
                        foreach (var id in missingIds) results.Add(new MarketItemPricing { ItemId = id });
                    }
                }
            }
            finally {
                this.fetchSemaphore.Release();
            }
        }

        return results;
    }

    public void UpdateLocalPrices(uint itemId, uint worldId, IReadOnlyList<LowestPriceResult> prices) {
        var key = $"{worldId}_{itemId}";
        uint existingAvg = 0;

        if (this.cache.TryGetValue(key, out var existing)) existingAvg = existing.Pricing.AverageSalePrice;

        this.cache[key] = new CachedPriceData {
            ItemId = itemId,
            Pricing = new MarketItemPricing { ItemId = itemId, Listings = prices.ToList(), AverageSalePrice = existingAvg },
            LastUpdated = DateTime.UtcNow,
            Source = PriceSourceType.LocalScanner
        };

        _ = this.framework.RunOnFrameworkThread(() => this.PricesUpdated?.Invoke(worldId, new[] { itemId }));

        this.logger.Debug($"[MarketPriceCache] Local cache updated instantly for item {itemId} with {prices.Count} live entries.");
    }

    public void Dispose() {
        this.fetchSemaphore.Dispose();
    }
}