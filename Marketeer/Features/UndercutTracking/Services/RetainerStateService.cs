using Dalamud.Plugin.Services;
using Marketeer.Features.Configuration.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.UndercutTracking.Services;

public class RetainerStateService : IRetainerStateService {
    private IConfigurationService configService;
    private IObjectTable objectTable;
    private List<RetainerListing> manualListings = new();

    public RetainerStateService(IConfigurationService configService, IObjectTable objectTable) {
        this.configService = configService;
        this.objectTable = objectTable;
    }

    public IReadOnlyList<RetainerListing> GetCurrentListings() {
        if (this.manualListings.Count > 0) {
            return this.manualListings.AsReadOnly();
        }

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null) {
            return System.Array.Empty<RetainerListing>();
        }

        var characterName = localPlayer.Name.TextValue;
        var worldId = localPlayer.HomeWorld.RowId;
        var storageKey = $"{characterName}_{worldId}";

        var config = this.configService.GetConfig();
        if (!config.FinancialRecords.TryGetValue(storageKey, out var characterData)) {
            return System.Array.Empty<RetainerListing>();
        }

        var listings = new List<RetainerListing>();

        foreach (var retainer in characterData.Retainers.Values) {
            foreach (var listing in retainer.MarketListings.Values) {
                listings.Add(new RetainerListing {
                    ItemId = listing.ItemId,
                    RetainerName = retainer.Name,
                    CurrentPrice = listing.PricePerUnit
                });
            }
        }

        return listings.AsReadOnly();
    }

    public void UpdateListings(IEnumerable<RetainerListing> newListings) {
        this.manualListings = new List<RetainerListing>(newListings);
    }
}