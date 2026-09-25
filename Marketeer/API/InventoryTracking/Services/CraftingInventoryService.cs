using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.CraftingProfit.Contracts;
using System.Linq;

namespace Marketeer.API.InventoryTracking.Services;

public class CraftingInventoryService : ICraftingInventoryService {
    private IConfigurationService configService;

    public CraftingInventoryService(IConfigurationService configService) {
        this.configService = configService;
    }

    public uint GetTotalOwnedQuantity(uint itemId) {
        var config = this.configService.GetConfig();
        uint total = 0;

        uint nqItemId = itemId > 1000000u ? itemId - 1000000u : itemId;
        uint hqItemId = nqItemId + 1000000u;

        foreach (var snapshot in config.InventorySnapshots.Values) {
            if (snapshot.Items != null) {
                total += (uint)snapshot.Items.Where(i => i.ItemId == nqItemId || i.ItemId == hqItemId).Sum(i => i.Quantity);
            }
        }

        if (config.RetainerInventorySnapshots != null) {
            foreach (var snapshot in config.RetainerInventorySnapshots.Values) {
                if (snapshot.Items != null) {
                    total += (uint)snapshot.Items.Where(i => i.ItemId == nqItemId || i.ItemId == hqItemId).Sum(i => i.Quantity);
                }
            }
        }

        return total;
    }
}