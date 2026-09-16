using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Financials.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.InventoryBrowser.Components;
using Marketeer.UI.Localization.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.InventoryBrowser.UI;

public class InventoryMenu : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localization;
    private IItemResolverService itemResolver;
    private ITextureProvider textureProvider;

    public string GroupName => this.localization.Translate("Group_Inventory");
    public string Name => this.localization.Translate("InventoryTab_Title");
    public int Priority => 15;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public InventoryMenu(
        IConfigurationService configService,
        ILocalizationService localization,
        IItemResolverService itemResolver,
        ITextureProvider textureProvider) {

        this.configService = configService;
        this.localization = localization;
        this.itemResolver = itemResolver;
        this.textureProvider = textureProvider;
    }

    public void DrawContent() {
        var config = this.configService.GetConfig();
        if (config.InventorySnapshots.Count == 0 && config.FinancialRecords.Count == 0) {
            ImGui.TextUnformatted(this.localization.Translate("InventoryTab_NoData"));
            return;
        }

        if (ImGui.BeginTabBar("InventoryCharacterTabs")) {
            foreach (var snapshotKvp in config.InventorySnapshots) {
                var snapshot = snapshotKvp.Value;
                if (ImGui.BeginTabItem(snapshot.CharacterName)) {
                    this.DrawPlayerInventory(snapshot);

                    var charKey = $"{snapshot.CharacterName}_{snapshot.HomeWorldId}";
                    if (config.FinancialRecords.TryGetValue(charKey, out var financialData)) {
                        foreach (var retainer in financialData.Retainers.Values) {
                            this.DrawRetainerInventory(retainer);
                        }
                    }

                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawPlayerInventory(InventorySnapshot snapshot) {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), this.localization.Translate("InventoryTab_PlayerInventory", snapshot.CharacterName));
        ImGui.TextDisabled(this.localization.Translate("InventoryTab_LastUpdated", snapshot.Timestamp.ToString("g")));
        ImGui.Separator();

        InventoryTablePresenter.DrawTable($"PlayerInv_{snapshot.CharacterName}", snapshot.Items, this.localization, this.itemResolver, this.textureProvider);
    }

    private void DrawRetainerInventory(RetainerFinancialData retainer) {
        ImGui.Spacing();
        if (ImGui.CollapsingHeader(this.localization.Translate("InventoryTab_RetainerInventory", retainer.Name))) {
            if (retainer.MarketListings.Count == 0) {
                ImGui.TextDisabled(this.localization.Translate("InventoryTab_NoRetainerData"));
                return;
            }

            var items = retainer.MarketListings.Values.Select(l => new TrackedItem { ItemId = l.ItemId, Quantity = l.Quantity, ContainerId = 0, SlotIndex = 0 }).ToList();
            InventoryTablePresenter.DrawTable($"RetainerInv_{retainer.RetainerId}", items, this.localization, this.itemResolver, this.textureProvider);
        }
    }
}