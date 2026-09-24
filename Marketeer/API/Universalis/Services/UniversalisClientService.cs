using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Logging.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Marketeer.API.Universalis.Services;

public class UniversalisClientService : IUniversalisClient {
    private HttpClient httpClient;
    private ILoggerService logger;

    public UniversalisClientService(HttpClient httpClient, ILoggerService logger) {
        this.httpClient = httpClient;
        this.logger = logger;

        if (this.httpClient.BaseAddress == null) {
            this.httpClient.BaseAddress = new Uri("https://universalis.app/api/v2/");
        }
    }

    public async Task<IReadOnlyList<UniversalisItemData>> FetchDataAsync(IEnumerable<uint> baseItemIds, uint worldId) {
        var results = new List<UniversalisItemData>();
        var idsList = baseItemIds.Distinct().ToList();

        if (idsList.Count == 0) return results;

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
                    this.ExtractData(data, batch[0], results);
                }
                else {
                    var multiData = JsonSerializer.Deserialize<UniversalisMultiResponse>(content);
                    if (multiData?.Items != null) {
                        foreach (var id in batch) {
                            if (multiData.Items.TryGetValue(id, out var data)) this.ExtractData(data, id, results);
                        }
                    }
                }
            }
        }
        catch (Exception ex) {
            this.logger.Error(ex, $"Failed to fetch data from Universalis for world {worldId}.");
        }

        return results;
    }

    private void ExtractData(UniversalisResponse? data, uint baseItemId, List<UniversalisItemData> results) {
        if (data != null) {
            var itemData = new UniversalisItemData {
                BaseItemId = baseItemId,
                AveragePriceNq = (uint)Math.Round(data.AveragePriceNq),
                AveragePriceHq = (uint)Math.Round(data.AveragePriceHq)
            };

            if (data.Listings != null) {
                foreach (var listing in data.Listings) {
                    itemData.Listings.Add(new LowestPriceResult {
                        ItemId = listing.IsHq ? baseItemId + 1000000u : baseItemId,
                        Price = listing.PricePerUnit,
                        RetainerName = listing.RetainerName ?? string.Empty,
                        IsHq = listing.IsHq
                    });
                }
            }
            results.Add(itemData);
        }
    }
}