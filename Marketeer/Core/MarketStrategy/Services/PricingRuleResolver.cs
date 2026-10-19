using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Configuration.Models;
using Marketeer.Core.MarketPricing.Contracts;

namespace Marketeer.Core.MarketStrategy.Services;

public class PricingRuleResolver : ICompetitionRuleProvider, IPriceRuleProvider {
    private IConfigurationService configService;
    private IDataManager dataManager;

    public PricingRuleResolver(IConfigurationService configService, IDataManager dataManager) {
        this.configService = configService;
        this.dataManager = dataManager;
    }

    public PricingRule GetRuleForItem(uint itemId) {
        var config = this.configService.GetConfig();

        var effectiveRule = new PricingRule {
            UndercutMode = config.UndercutMode,
            UndercutAmount = config.UndercutAmount,
            UndercutRelativePercentage = config.UndercutRelativePercentage,
            HqPremiumPercentage = config.GlobalHqPremiumPercentage,
            IgnoreHq = config.GlobalIgnoreHq
        };

        PricingRule? categoryRule = null;
        PricingRule? itemRule = null;

        var baseItemId = itemId > 1000000u ? itemId - 1000000u : itemId;

        uint categoryId = this.GetItemCategory(baseItemId);

        if (categoryId > 0 && config.CategoryProfileMappings.TryGetValue(categoryId, out var catProfileId)) {
            if (config.PricingProfiles.TryGetValue(catProfileId, out var catProfile)) {
                categoryRule = catProfile.Rule;
            }
        }

        if (config.ItemProfileMappings.TryGetValue(baseItemId, out var itemProfileId)) {
            if (config.PricingProfiles.TryGetValue(itemProfileId, out var itmProfile)) {
                itemRule = itmProfile.Rule;
            }
        }

        this.ApplyOverride(effectiveRule, categoryRule);
        this.ApplyOverride(effectiveRule, itemRule);

        return effectiveRule;
    }

    protected virtual uint GetItemCategory(uint baseItemId) {
        var itemSheet = this.dataManager.GetExcelSheet<Item>();
        if (itemSheet != null && itemSheet.HasRow(baseItemId)) {
            var itemData = itemSheet.GetRow(baseItemId);
            return itemData.ItemSearchCategory.RowId;
        }
        return 0;
    }

    private void ApplyOverride(PricingRule target, PricingRule? source) {
        if (source == null) return;

        if (source.UndercutMode.HasValue) target.UndercutMode = source.UndercutMode.Value;
        if (source.UndercutAmount.HasValue) target.UndercutAmount = source.UndercutAmount.Value;
        if (source.UndercutRelativePercentage.HasValue) target.UndercutRelativePercentage = source.UndercutRelativePercentage.Value;
        if (source.HqPremiumPercentage.HasValue) target.HqPremiumPercentage = source.HqPremiumPercentage.Value;
        if (source.IgnoreHq.HasValue) target.IgnoreHq = source.IgnoreHq.Value;
    }
}