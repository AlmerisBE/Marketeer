using Marketeer.API.Universalis.Models;
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
    private IPriceRuleProvider ruleProvider;
    private IPriceCrashProtector crashProtector;

    public PriceCalculationService(
        IConfigurationService configService,
        IItemResolverService itemResolver,
        IPriceRuleProvider ruleProvider,
        IPriceCrashProtector crashProtector) {

        this.configService = configService;
        this.itemResolver = itemResolver;
        this.ruleProvider = ruleProvider;
        this.crashProtector = crashProtector;
    }

    public PriceCalculationResult CalculateTargetPrice(uint itemId, uint currentPrice, MarketItemPricing pricing, bool isHq) {
        var config = this.configService.GetConfig();
        var rule = this.ruleProvider.GetRuleForItem(itemId);
        var result = new PriceCalculationResult { Action = PricingAction.DecreasePrice, CalculatedPrice = currentPrice, Reason = PricingReason.Unknown };

        if (config.EnableStorageProtection && currentPrice >= config.StorageProtectionThreshold) {
            result.CalculatedPrice = currentPrice;
            result.Action = PricingAction.KeepPrice;
            result.Reason = PricingReason.StorageProtection;
            return result;
        }

        var vendorSellPrice = this.itemResolver.ResolveVendorPrice(itemId);
        var vendorBuyPrice = this.itemResolver.ResolveVendorBuyPrice(itemId);

        var ownRetainers = this.GetOwnRetainers(config);
        var whitelist = config.CompetitorWhitelist ?? new List<string>();

        // Dynamic relative outlier filter using user configuration
        uint maxSanePrice = pricing.AverageSalePrice > 0 ? Math.Max(config.OutlierMinimumFloor, (uint)(pricing.AverageSalePrice * config.OutlierMultiplier)) : uint.MaxValue;

        var validCompetitors = pricing.Listings
            .Where(l => !ownRetainers.Contains(l.RetainerName))
            .Where(l => !(config.CompetitorWhitelistBehavior == WhitelistBehavior.Ignore && this.IsWhitelisted(l.RetainerName, whitelist, ownRetainers, config)))
            .Where(l => !config.EnableStorageProtection || l.Price < config.StorageProtectionThreshold)
            .Where(l => l.Price <= maxSanePrice)
            .OrderBy(l => l.Price)
            .ToList();

        this.crashProtector.ApplyProtection(pricing, isHq, validCompetitors, out bool holdPrice);

        // Fill competitor data early so UI can display who we are matching/crashing against
        var cheapestValid = validCompetitors.FirstOrDefault(l => l.IsHq == isHq) ?? validCompetitors.FirstOrDefault();
        if (cheapestValid != null) {
            result.CompetitorPrice = cheapestValid.Price;
            result.ContextData = cheapestValid.RetainerName;
        }

        if (holdPrice) {
            if (currentPrice == 0) {
                result.CalculatedPrice = pricing.AverageSalePrice > 0 ? pricing.AverageSalePrice : this.CalculateFallbackPrice(pricing, vendorSellPrice, vendorBuyPrice, config);
                result.Action = PricingAction.IncreasePrice;
            }
            else {
                result.CalculatedPrice = currentPrice;
                result.Action = PricingAction.KeepPrice;
            }
            result.Reason = PricingReason.MarketCrashProtection;
            return result;
        }

        if (validCompetitors.Count == 0) {
            if (currentPrice > 0) {
                result.CalculatedPrice = currentPrice;
                result.Action = PricingAction.KeepPrice;
                result.Reason = PricingReason.AlreadyLowest;
            }
            else {
                if (pricing.Listings.Count > 0) {
                    result.CalculatedPrice = pricing.Listings.Min(l => l.Price);
                    result.Reason = PricingReason.AlreadyLowest;
                }
                else {
                    result.CalculatedPrice = this.CalculateFallbackPrice(pricing, vendorSellPrice, vendorBuyPrice, config);
                    result.Reason = PricingReason.NoCompetition;
                }

                if (result.CalculatedPrice == currentPrice) result.Action = PricingAction.KeepPrice;
                else if (result.CalculatedPrice > currentPrice) result.Action = PricingAction.IncreasePrice;
                else result.Action = PricingAction.DecreasePrice;

                if (this.EvaluateLoss(result.CalculatedPrice, vendorSellPrice, currentPrice, config, out var lossAction)) {
                    result.Action = lossAction;
                    if (lossAction == PricingAction.KeepPrice) result.CalculatedPrice = currentPrice;
                    result.Reason = PricingReason.VendorMinimumReached;
                }
            }
            return result;
        }

        bool ignoreHq = rule.IgnoreHq ?? false;
        double premium = rule.HqPremiumPercentage ?? 0.0;
        var undercutMode = rule.UndercutMode ?? UndercutMode.Absolute;
        uint undercutAmount = rule.UndercutAmount ?? 1;
        double undercutPct = rule.UndercutRelativePercentage ?? 1.0;

        LowestPriceResult? bestCompetitor = null;

        if (ignoreHq) bestCompetitor = validCompetitors.FirstOrDefault();
        else {
            if (isHq) {
                bestCompetitor = validCompetitors.FirstOrDefault(l => l.IsHq);
                if (bestCompetitor == null) bestCompetitor = validCompetitors.FirstOrDefault(l => !l.IsHq);
            }
            else bestCompetitor = validCompetitors.FirstOrDefault();
        }

        if (bestCompetitor == null) {
            result.CalculatedPrice = this.CalculateFallbackPrice(pricing, vendorSellPrice, vendorBuyPrice, config);
            result.Reason = PricingReason.NoCompetition;

            if (result.CalculatedPrice == currentPrice) result.Action = PricingAction.KeepPrice;
            else if (result.CalculatedPrice > currentPrice) result.Action = PricingAction.IncreasePrice;
            else result.Action = PricingAction.DecreasePrice;

            if (this.EvaluateLoss(result.CalculatedPrice, vendorSellPrice, currentPrice, config, out var lossAction)) {
                result.Action = lossAction;
                if (lossAction == PricingAction.KeepPrice) result.CalculatedPrice = currentPrice;
                result.Reason = PricingReason.VendorMinimumReached;
            }
            return result;
        }

        uint targetPrice = currentPrice;
        bool isWhitelistedCompetitor = this.IsWhitelisted(bestCompetitor.RetainerName, whitelist, ownRetainers, config);

        result.ContextData = bestCompetitor.RetainerName;
        result.CompetitorPrice = bestCompetitor.Price;

        if (!ignoreHq && isHq && !bestCompetitor.IsHq) {
            targetPrice = (uint)Math.Floor(bestCompetitor.Price * (1.0 + (premium / 100.0)));
            result.Reason = PricingReason.UndercutCompetitor;
        }
        else {
            if (isWhitelistedCompetitor && config.CompetitorWhitelistBehavior == WhitelistBehavior.MatchPrice) {
                targetPrice = bestCompetitor.Price;
                result.Reason = PricingReason.MatchedWhitelistedCompetitor;
            }
            else {
                if (undercutMode == UndercutMode.Absolute) targetPrice = (uint)Math.Max(1, (int)bestCompetitor.Price - (int)undercutAmount);
                else targetPrice = (uint)Math.Max(1, bestCompetitor.Price - (bestCompetitor.Price * (undercutPct / 100.0)));

                result.Reason = PricingReason.UndercutCompetitor;
            }
        }

        result.CalculatedPrice = targetPrice;

        if (targetPrice == currentPrice) {
            result.Action = PricingAction.KeepPrice;
            if (result.Reason == PricingReason.UndercutCompetitor) result.Reason = PricingReason.AlreadyLowest;
        }
        else if (targetPrice > currentPrice) result.Action = PricingAction.IncreasePrice;
        else result.Action = PricingAction.DecreasePrice;

        if (this.EvaluateLoss(targetPrice, vendorSellPrice, currentPrice, config, out var evaluatedAction)) {
            result.Action = evaluatedAction;
            if (evaluatedAction == PricingAction.KeepPrice) result.CalculatedPrice = currentPrice;
            result.Reason = PricingReason.VendorMinimumReached;
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

    private bool IsWhitelisted(string retainerName, List<string> whitelist, HashSet<string> ownRetainers, PluginConfiguration config) {
        return whitelist.Contains(retainerName, StringComparer.InvariantCultureIgnoreCase) ||
               (config.AutoWhitelistOwnRetainers && ownRetainers.Contains(retainerName));
    }

    private bool EvaluateLoss(uint targetPrice, uint vendorSellPrice, uint currentPrice, PluginConfiguration config, out PricingAction action) {
        action = targetPrice > currentPrice ? PricingAction.IncreasePrice : PricingAction.DecreasePrice;
        if (!config.EnforceVendorPriceMinimum || vendorSellPrice == 0 || targetPrice >= vendorSellPrice) return false;

        action = config.LossBehavior switch {
            MinimumPriceBehavior.KeepCurrentPrice => PricingAction.KeepPrice,
            MinimumPriceBehavior.CancelToInventory => PricingAction.CancelListing,
            _ => targetPrice > currentPrice ? PricingAction.IncreasePrice : PricingAction.DecreasePrice
        };
        return true;
    }

    private uint CalculateFallbackPrice(MarketItemPricing pricing, uint vendorSell, uint vendorBuy, PluginConfiguration config) {
        return config.EmptyMarketFallbackMode switch {
            FallbackPricingMode.AverageListingPrice => pricing.AverageSalePrice > 0 ? pricing.AverageSalePrice : (uint)(vendorSell * config.EmptyMarketFallbackMultiplier),
            FallbackPricingMode.MaxListingPrice => pricing.Listings.Count > 0 ? pricing.Listings.Max(l => l.Price) : (pricing.AverageSalePrice > 0 ? pricing.AverageSalePrice : (uint)(vendorSell * config.EmptyMarketFallbackMultiplier)),
            FallbackPricingMode.VendorBuyMultiple => (uint)(vendorBuy * config.EmptyMarketFallbackMultiplier),
            _ => (uint)(vendorSell * config.EmptyMarketFallbackMultiplier)
        };
    }
}