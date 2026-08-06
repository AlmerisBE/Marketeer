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

public class UniversalisClientService : IServerPriceProvider {
    private HttpClient httpClient;
    private ILoggerService logger;
    private IConfigurationService configService;

    // Cache is now storing a list of results (up to 2: Lowest NQ and Lowest HQ)
    private ConcurrentDictionary<uint, (IReadOnlyList<LowestPriceResult> Results, DateTime FetchTime)> cache = new();

    public UniversalisClientService(HttpClient httpClient, ILoggerService logger, IConfigurationService configService) {
        this.httpClient = httpClient;
        this.logger = logger;
        this.configService = configService;

        if (this.httpClient.BaseAddress == null) {
            this.httpClient.BaseAddress = new Uri("https://universalis.app/api/v2/");
        }
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
            // Si bypassCache est vrai, on force l'ajout à idsToFetch
            if (!bypassCache && this.cache.TryGetValue(id, out var cachedData) && (DateTime.UtcNow - cachedData.FetchTime) < cacheDuration) {
                results.AddRange(cachedData.Results);
            }
            else {
                idsToFetch.Add(id);
            }
        }

        if (idsToFetch.Count == 0) {
            return results;
        }

        try {
            for (int i = 0; i < idsToFetch.Count; i += 100) {
                var batch = idsToFetch.Skip(i).Take(100).ToList();
                var idsString = string.Join(",", batch);
                var response = await this.httpClient.GetAsync($"{worldId}/{idsString}");

                if (!response.IsSuccessStatusCode) {
                    this.logger.Warning($"Universalis API returned {response.StatusCode} for batch on world {worldId}.");
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync();

                if (batch.Count == 1) {
                    var data = JsonSerializer.Deserialize<UniversalisResponse>(content);
                    this.ExtractAndCacheLowestPrices(data, batch[0], results);
                }
                else {
                    var multiData = JsonSerializer.Deserialize<UniversalisMultiResponse>(content);
                    if (multiData?.Items != null) {
                        foreach (var id in batch) {
                            if (multiData.Items.TryGetValue(id, out var data)) {
                                this.ExtractAndCacheLowestPrices(data, id, results);
                            }
                            else {
                                this.CacheEmptyResult(id);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, $"Failed to fetch multi-item data from Universalis for world {worldId}.");
        }

        return results;
    }

    private void ExtractAndCacheLowestPrices(UniversalisResponse? data, uint itemId, List<LowestPriceResult> results) {
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

        this.cache[itemId] = (overviews.AsReadOnly(), DateTime.UtcNow);
        results.AddRange(overviews);
    }

    private void CacheEmptyResult(uint itemId) {
        this.cache[itemId] = (new List<LowestPriceResult>().AsReadOnly(), DateTime.UtcNow);
    }
}