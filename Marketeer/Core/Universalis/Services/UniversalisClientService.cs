using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Marketeer.Core.Universalis.Services;

public class UniversalisClientService : IServerPriceProvider, IDisposable {
    public event Action<uint, IEnumerable<uint>>? PricesUpdated;

    private HttpClient httpClient;
    private ILoggerService logger;
    private IConfigurationService configService;
    private IFramework framework;

    private ConcurrentDictionary<string, (IReadOnlyList<LowestPriceResult> Results, DateTime FetchTime)> cache = new();
    private DateTime lastRefreshTime;
    private bool isRefreshing;

    public UniversalisClientService(HttpClient httpClient, ILoggerService logger, IConfigurationService configService, IFramework framework) {
        this.httpClient = httpClient;
        this.logger = logger;
        this.configService = configService;
        this.framework = framework;

        if (this.httpClient.BaseAddress == null) {
            this.httpClient.BaseAddress = new Uri("https://universalis.app/api/v2/");
        }

        this.lastRefreshTime = DateTime.UtcNow;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void Dispose() {
        this.framework.Update -= this.OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (this.isRefreshing) {
            return;
        }

        var cacheDuration = TimeSpan.FromMinutes(this.configService.GetConfig().UniversalisCacheMinutes);
        if (DateTime.UtcNow - this.lastRefreshTime >= cacheDuration) {
            _ = this.RefreshAllCachedItemsAsync();
        }
    }

    private async Task RefreshAllCachedItemsAsync() {
        this.isRefreshing = true;
        this.lastRefreshTime = DateTime.UtcNow;

        try {
            var itemsByWorld = new Dictionary<uint, List<uint>>();

            foreach (var key in this.cache.Keys) {
                var parts = key.Split('_');
                if (parts.Length == 2 && uint.TryParse(parts[0], out var worldId) && uint.TryParse(parts[1], out var itemId)) {
                    if (!itemsByWorld.ContainsKey(worldId)) {
                        itemsByWorld[worldId] = new List<uint>();
                    }
                    itemsByWorld[worldId].Add(itemId);
                }
            }

            foreach (var kvp in itemsByWorld) {
                await this.FetchAndCachePricesAsync(kvp.Value, kvp.Key);
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, "Failed to refresh Universalis cache in background.");
        }
        finally {
            this.isRefreshing = false;
        }
    }

    public async Task ForceRefreshAsync(IEnumerable<uint> itemIds, uint worldId) {
        await this.FetchAndCachePricesAsync(itemIds, worldId);
    }

    public async Task<LowestPriceResult?> GetLowestPriceAsync(uint itemId, uint worldId, bool bypassCache = false) {
        var results = await this.GetLowestPricesAsync(new[] { itemId }, worldId, bypassCache);
        return results.OrderBy(r => r.Price).FirstOrDefault();
    }

    public async Task<IReadOnlyList<LowestPriceResult>> GetLowestPricesAsync(IEnumerable<uint> itemIds, uint worldId, bool bypassCache = false) {
        var results = new List<LowestPriceResult>();
        var idsToFetch = new List<uint>();
        var cacheDuration = TimeSpan.FromMinutes(this.configService.GetConfig().UniversalisCacheMinutes);

        foreach (var id in itemIds.Distinct()) {
            var cacheKey = $"{worldId}_{id}";
            if (!bypassCache && this.cache.TryGetValue(cacheKey, out var cachedData) && (DateTime.UtcNow - cachedData.FetchTime) < cacheDuration) {
                results.AddRange(cachedData.Results);
            }
            else {
                idsToFetch.Add(id);
            }
        }

        if (idsToFetch.Count > 0) {
            var fetchedResults = await this.FetchAndCachePricesAsync(idsToFetch, worldId);
            results.AddRange(fetchedResults);
        }

        return results;
    }

    private async Task<List<LowestPriceResult>> FetchAndCachePricesAsync(IEnumerable<uint> itemIds, uint worldId) {
        var results = new List<LowestPriceResult>();
        var idsList = itemIds.Distinct().ToList();

        if (idsList.Count == 0) {
            return results;
        }

        try {
            for (int i = 0; i < idsList.Count; i += 100) {
                var batch = idsList.Skip(i).Take(100).ToList();
                var idsString = string.Join(",", batch);
                var response = await this.httpClient.GetAsync($"{worldId}/{idsString}");

                if (!response.IsSuccessStatusCode) {
                    this.logger.Warning($"Universalis API returned {response.StatusCode} for batch on world {worldId}.");
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync();

                if (batch.Count == 1) {
                    var data = JsonSerializer.Deserialize<UniversalisResponse>(content);
                    this.ExtractAndCacheLowestPrices(data, batch[0], worldId, results);
                }
                else {
                    var multiData = JsonSerializer.Deserialize<UniversalisMultiResponse>(content);
                    if (multiData?.Items != null) {
                        foreach (var id in batch) {
                            if (multiData.Items.TryGetValue(id, out var data)) {
                                this.ExtractAndCacheLowestPrices(data, id, worldId, results);
                            }
                            else {
                                this.CacheEmptyResult(id, worldId);
                            }
                        }
                    }
                }
            }

            if (results.Count > 0) {
                // Ensure thread safety and safely await the framework task to resolve CS4014
                await this.framework.RunOnFrameworkThread(() => {
                    this.PricesUpdated?.Invoke(worldId, idsList);
                });
            }

        }
        catch (Exception ex) {
            this.logger.Error(ex, $"Failed to fetch multi-item data from Universalis for world {worldId}.");
        }

        return results;
    }

    private void ExtractAndCacheLowestPrices(UniversalisResponse? data, uint itemId, uint worldId, List<LowestPriceResult> results) {
        var overviews = new List<LowestPriceResult>();

        if (data != null && data.Listings != null && data.Listings.Count > 0) {
            var lowestNq = data.Listings.Where(l => !l.IsHq).OrderBy(l => l.PricePerUnit).FirstOrDefault();
            var lowestHq = data.Listings.Where(l => l.IsHq).OrderBy(l => l.PricePerUnit).FirstOrDefault();

            if (lowestNq != null) {
                overviews.Add(new LowestPriceResult { ItemId = itemId, Price = lowestNq.PricePerUnit, RetainerName = lowestNq.RetainerName, IsHq = false });
            }

            if (lowestHq != null) {
                overviews.Add(new LowestPriceResult { ItemId = itemId, Price = lowestHq.PricePerUnit, RetainerName = lowestHq.RetainerName, IsHq = true });
            }
        }

        var cacheKey = $"{worldId}_{itemId}";
        this.cache[cacheKey] = (overviews.AsReadOnly(), DateTime.UtcNow);
        results.AddRange(overviews);
    }

    private void CacheEmptyResult(uint itemId, uint worldId) {
        var cacheKey = $"{worldId}_{itemId}";
        this.cache[cacheKey] = (new List<LowestPriceResult>().AsReadOnly(), DateTime.UtcNow);
    }
}