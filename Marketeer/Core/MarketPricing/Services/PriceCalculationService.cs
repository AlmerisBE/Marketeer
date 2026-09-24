using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.SalesHistory.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.MarketPricing.Services;

public class PriceCalculationService : IPriceCalculationService {
    private IConfigurationService configService;
    private IItemResolverService itemResolver;

    public PriceCalculationService(IConfigurationService configService, IItemResolverService itemResolver) {
        this.configService = configService;
        this.itemResolver = itemResolver;
    }

    public PriceCalculationResult CalculateTargetPrice(uint itemId, uint currentPrice, MarketItemPricing pricing) {
        var config = this.configService.GetConfig();
        var result = new PriceCalculationResult { Action = PricingAction.UpdatePrice, CalculatedPrice = currentPrice };

        var vendorSellPrice = this.itemResolver.ResolveVendorPrice(itemId);
        var vendorBuyPrice = this.itemResolver.ResolveVendorBuyPrice(itemId);

        var ownRetainers = this.GetOwnRetainers(config);
        var competitors = pricing.Listings.Where(l => !ownRetainers.Contains(l.RetainerName)).OrderBy(l => l.Price).ToList();

        if (competitors.Count == 0) {
            if (currentPrice > 0) {
                result.CalculatedPrice = currentPrice;
                result.Action = PricingAction.KeepPrice;
            }
            else if (pricing.Listings.Count > 0) {
                result.CalculatedPrice = pricing.Listings.Min(l => l.Price);
            }
            else {
                result.CalculatedPrice = this.CalculateFallbackPrice(pricing, vendorSellPrice, vendorBuyPrice, config);
                if (this.EvaluateLoss(result.CalculatedPrice, vendorSellPrice, currentPrice, config, out var lossAction)) {
                    result.Action = lossAction;
                    if (lossAction == PricingAction.KeepPrice) result.CalculatedPrice = currentPrice;
                }
            }
            return result;
        }

        uint targetPrice = currentPrice;
        bool priceFound = false;
        var whitelist = config.CompetitorWhitelist ?? new List<string>();

        foreach (var comp in competitors) {
            bool isWhitelisted = whitelist.Contains(comp.RetainerName, StringComparer.InvariantCultureIgnoreCase) ||
                                 (config.AutoWhitelistOwnRetainers && ownRetainers.Contains(comp.RetainerName));

            if (isWhitelisted) {
                if (config.CompetitorWhitelistBehavior == WhitelistBehavior.Ignore) continue;
                targetPrice = comp.Price;
                priceFound = true;
                break;
            }

            targetPrice = config.UndercutMode == UndercutMode.Absolute
                ? (uint)Math.Max(1, (int)comp.Price - (int)config.UndercutAmount)
                : (uint)Math.Max(1, comp.Price - (comp.Price * (config.UndercutRelativePercentage / 100.0)));

            priceFound = true;
            break;
        }

        if (!priceFound) {
            result.CalculatedPrice = currentPrice;
            result.Action = PricingAction.KeepPrice;
            return result;
        }

        result.CalculatedPrice = targetPrice;
        if (this.EvaluateLoss(targetPrice, vendorSellPrice, currentPrice, config, out var action)) {
            result.Action = action;
            if (action == PricingAction.KeepPrice) result.CalculatedPrice = currentPrice;
        }

        return result;
    }

    private HashSet<string> GetOwnRetainers(PluginConfiguration config) {
        var retainers = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var cData in config.FinancialRecords.Values) {
            foreach (var rData in cData.Retainers.Values) retainers.Add(rData.Name);
        }
        return retainers;
    }

    private bool EvaluateLoss(uint targetPrice, uint vendorSellPrice, uint currentPrice, PluginConfiguration config, out PricingAction action) {
        action = PricingAction.UpdatePrice;
        if (!config.EnforceVendorPriceMinimum || vendorSellPrice == 0 || targetPrice >= vendorSellPrice) return false;

        action = config.LossBehavior switch {
            MinimumPriceBehavior.KeepCurrentPrice => PricingAction.KeepPrice,
            MinimumPriceBehavior.CancelToInventory => PricingAction.CancelListing,
            _ => PricingAction.UpdatePrice
        };
        return true;
    }

    private uint CalculateFallbackPrice(MarketItemPricing pricing, uint vendorSell, uint vendorBuy, PluginConfiguration config) {
        return config.EmptyMarketFallbackMode switch {
            // Apply the actual Universalis average sale price as the priority fallback when the market is empty
            FallbackPricingMode.AverageListingPrice => pricing.AverageSalePrice > 0 ? pricing.AverageSalePrice : (uint)(vendorSell * config.EmptyMarketFallbackMultiplier),
            FallbackPricingMode.MaxListingPrice => pricing.Listings.Count > 0 ? pricing.Listings.Max(l => l.Price) : (pricing.AverageSalePrice > 0 ? pricing.AverageSalePrice : (uint)(vendorSell * config.EmptyMarketFallbackMultiplier)),
            FallbackPricingMode.VendorBuyMultiple => (uint)(vendorBuy * config.EmptyMarketFallbackMultiplier),
            _ => (uint)(vendorSell * config.EmptyMarketFallbackMultiplier)
        };
    }
}