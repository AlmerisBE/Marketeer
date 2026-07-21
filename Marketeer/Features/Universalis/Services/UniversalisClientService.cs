using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using Marketeer.Features.Universalis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Marketeer.Features.Universalis.Services;

public class UniversalisClientService : IServerPriceProvider {
    private HttpClient httpClient;
    private ILoggerService logger;

    public UniversalisClientService(HttpClient httpClient, ILoggerService logger) {
        this.httpClient = httpClient;
        this.logger = logger;

        if (this.httpClient.BaseAddress == null) {
            this.httpClient.BaseAddress = new Uri("https://universalis.app/api/v2/");
        }
    }

    public async Task<LowestPriceResult?> GetLowestPriceAsync(uint itemId, uint worldId) {
        var results = await this.GetLowestPricesAsync(new[] { itemId }, worldId);
        return results.FirstOrDefault();
    }

    public async Task<IReadOnlyList<LowestPriceResult>> GetLowestPricesAsync(IEnumerable<uint> itemIds, uint worldId) {
        var results = new List<LowestPriceResult>();
        var idsList = itemIds.Distinct().ToList();

        if (idsList.Count == 0) {
            return results;
        }

        try {
            // Universalis handles up to 100 items efficiently per request
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
                    // Single item queries return the object directly
                    var data = JsonSerializer.Deserialize<UniversalisResponse>(content);
                    this.ExtractAndAddLowestPrice(data, results);
                }
                else {
                    // Multi-item queries return a dictionary wrapper
                    var multiData = JsonSerializer.Deserialize<UniversalisMultiResponse>(content);
                    if (multiData != null && multiData.Items != null) {
                        foreach (var kvp in multiData.Items) {
                            this.ExtractAndAddLowestPrice(kvp.Value, results);
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

    private void ExtractAndAddLowestPrice(UniversalisResponse? data, List<LowestPriceResult> results) {
        if (data != null && data.Listings != null && data.Listings.Count > 0) {
            var lowest = data.Listings.OrderBy(l => l.PricePerUnit).First();

            results.Add(new LowestPriceResult {
                ItemId = data.ItemId,
                Price = lowest.PricePerUnit,
                RetainerName = lowest.RetainerName
            });
        }
    }
}