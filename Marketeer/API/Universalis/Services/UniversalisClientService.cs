using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.Logging.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            this.httpClient.DefaultRequestHeaders.Add("User-Agent", "Marketeer/0.0.0.1 (Dalamud Plugin)");
            this.httpClient.Timeout = TimeSpan.FromSeconds(15);
        }
    }

    public async Task<IReadOnlyList<UniversalisItemData>> FetchDataAsync(IEnumerable<uint> baseItemIds, uint worldId) {
        var results = new List<UniversalisItemData>();
        var idsList = baseItemIds.Distinct().ToList();

        if (idsList.Count == 0) return results;

        int batchSize = 50;
        int maxRetries = 3;

        try {
            for (int i = 0; i < idsList.Count; i += batchSize) {
                var batch = idsList.Skip(i).Take(batchSize).ToList();
                var idsString = string.Join(",", batch);

                HttpResponseMessage? response = null;
                bool success = false;
                bool is404 = false;
                int attemptsUsed = 0;

                for (int attempt = 1; attempt <= maxRetries; attempt++) {
                    attemptsUsed = attempt;
                    try {
                        // Limiting entries reduces Universalis DB strain and drastically cuts down 504 timeouts
                        response = await this.httpClient.GetAsync($"{worldId}/{idsString}?entries=10");

                        if (response.IsSuccessStatusCode) {
                            success = true;
                            break;
                        }

                        if (response.StatusCode == HttpStatusCode.NotFound) {
                            is404 = true;
                            success = true;
                            break;
                        }

                        if (this.IsTransientError(response.StatusCode)) {
                            this.logger.Warning($"Universalis API returned {response.StatusCode} for batch on world {worldId} (Attempt {attempt}/{maxRetries}). Retrying in {attempt * 2}s...");
                            await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                        }
                        else {
                            this.logger.Warning($"Universalis API returned {response.StatusCode} for batch on world {worldId}. Skipping batch.");
                            break;
                        }
                    }
                    catch (TaskCanceledException) {
                        this.logger.Warning($"Universalis API request timed out for batch on world {worldId} (Attempt {attempt}/{maxRetries}). Retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                    }
                    catch (Exception ex) {
                        this.logger.Error(ex, $"Exception during Universalis API call for batch on world {worldId} (Attempt {attempt}/{maxRetries}).");
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                    }
                }

                if (!success) {
                    this.logger.Error($"Failed to fetch data from Universalis for batch on world {worldId} after {attemptsUsed} attempts.");
                    continue;
                }

                if (is404 || response == null) continue;

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
            this.logger.Error(ex, $"Critical failure while processing Universalis API data for world {worldId}.");
        }

        return results;
    }

    private bool IsTransientError(HttpStatusCode statusCode) {
        return statusCode == HttpStatusCode.GatewayTimeout ||
               statusCode == HttpStatusCode.BadGateway ||
               statusCode == HttpStatusCode.ServiceUnavailable ||
               statusCode == HttpStatusCode.TooManyRequests ||
               statusCode == HttpStatusCode.RequestTimeout;
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