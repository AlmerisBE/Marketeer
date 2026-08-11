using Marketeer.API.Configuration.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.API.MarketListings.Models;
using Marketeer.API.SalesHistory.Contracts;
using System.Collections.Generic;

namespace Marketeer.Core.MarketListings.Services;

public class ListingOptimizationService : IListingOptimizationService {
    private readonly IConfigurationService configService;
    private readonly IItemResolverService itemResolver;

    public ListingOptimizationService(
        IConfigurationService configService,
        IItemResolverService itemResolver) {

        this.configService = configService;
        this.itemResolver = itemResolver;
    }

    public IReadOnlyList<SuboptimalListing> GetVendorPricedListings() {
        var results = new List<SuboptimalListing>();
        var config = this.configService.GetConfig();

        foreach (var charData in config.FinancialRecords.Values) {
            foreach (var retainer in charData.Retainers.Values) {
                foreach (var listing in retainer.MarketListings.Values) {
                    uint baseItemId = listing.ItemId > 1000000u ? listing.ItemId - 1000000u : listing.ItemId;
                    uint vendorPrice = this.itemResolver.ResolveVendorPrice(baseItemId);

                    if (vendorPrice > 0 && listing.PricePerUnit <= vendorPrice) {
                        results.Add(new SuboptimalListing {
                            ItemId = listing.ItemId,
                            ItemName = this.itemResolver.ResolveItemName(listing.ItemId),
                            CharacterName = charData.CharacterName,
                            RetainerName = retainer.Name,
                            Price = listing.PricePerUnit,
                            Quantity = listing.Quantity,
                            VendorPrice = vendorPrice
                        });
                    }
                }
            }
        }

        return results;
    }
}