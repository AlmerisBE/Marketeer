using Dalamud.Plugin.Services;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.UndercutTracking.Services;

public class RetainerStateService : IRetainerStateService {
    private IConfigurationService configurationService;
    private IObjectTable objectTable;
    private List<RetainerListing> currentListings;

    public event System.Action<IEnumerable<RetainerListing>>? ListingsUpdated;

    public RetainerStateService(IConfigurationService configurationService, IObjectTable objectTable) {
        this.configurationService = configurationService;
        this.objectTable = objectTable;
        this.currentListings = new List<RetainerListing>();
    }

    public IReadOnlyList<RetainerListing> GetCurrentListings() {
        // Returns the active session listings if they have been updated recently
        if (this.currentListings.Any()) {
            return this.currentListings;
        }

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.HomeWorld.RowId == 0) {
            return new List<RetainerListing>();
        }

        // Reconstruct the configuration key based on the local player
        var playerName = localPlayer.Name.TextValue;
        var worldId = localPlayer.HomeWorld.RowId;
        var characterKey = $"{playerName}_{worldId}";

        var config = this.configurationService.GetConfig();
        if (!config.FinancialRecords.TryGetValue(characterKey, out var characterData)) {
            return new List<RetainerListing>();
        }

        var extractedListings = new List<RetainerListing>();

        // Extract all current listings from the configuration data
        foreach (var retainer in characterData.Retainers.Values) {
            foreach (var listingKvp in retainer.MarketListings) {
                extractedListings.Add(new RetainerListing {
                    SlotIndex = listingKvp.Key,
                    ItemId = listingKvp.Value.ItemId,
                    RetainerName = retainer.Name,
                    CurrentPrice = listingKvp.Value.PricePerUnit
                });
            }
        }
        return extractedListings;
    }

    public void UpdateListings(IEnumerable<RetainerListing> listings) {
        this.currentListings = listings.ToList();
        this.ListingsUpdated?.Invoke(this.currentListings);
    }

    public IReadOnlyList<CharacterMarketData> GetAllCharactersListings() {
        var config = this.configurationService.GetConfig();
        var result = new List<CharacterMarketData>();

        foreach (var charData in config.FinancialRecords.Values) {
            var marketData = new CharacterMarketData {
                CharacterName = charData.CharacterName,
                HomeWorldId = charData.HomeWorldId
            };

            foreach (var retainer in charData.Retainers.Values) {
                foreach (var listingKvp in retainer.MarketListings) {
                    marketData.Listings.Add(new RetainerListing {
                        SlotIndex = listingKvp.Key,
                        ItemId = listingKvp.Value.ItemId,
                        RetainerName = retainer.Name,
                        CurrentPrice = listingKvp.Value.PricePerUnit
                    });
                }
            }

            result.Add(marketData);
        }

        return result;
    }
}