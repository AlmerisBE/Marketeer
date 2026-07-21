using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using Marketeer.Features.Universalis.Models;
using System;
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

        // Ensure the base address is configured for the injected HttpClient
        if (this.httpClient.BaseAddress == null) {
            this.httpClient.BaseAddress = new Uri("https://universalis.app/api/v2/");
        }
    }

    public async Task<LowestPriceResult?> GetLowestPriceAsync(uint itemId, uint worldId) {
        try {
            var response = await this.httpClient.GetAsync($"{worldId}/{itemId}");

            if (!response.IsSuccessStatusCode) {
                this.logger.Warning($"Universalis API returned {response.StatusCode} for item {itemId} on world {worldId}.");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<UniversalisResponse>(content);

            if (data == null || data.Listings == null || data.Listings.Count == 0) {
                return null;
            }

            // Universalis usually returns listings sorted by price, but we order it explicitly to be absolutely sure
            var lowestListing = data.Listings.OrderBy(l => l.PricePerUnit).First();

            return new LowestPriceResult {
                ItemId = itemId,
                Price = lowestListing.PricePerUnit,
                RetainerName = lowestListing.RetainerName
            };
        }
        catch (Exception ex) {
            this.logger.Error(ex, $"Failed to fetch data from Universalis for item {itemId} on world {worldId}.");
            return null;
        }
    }
}