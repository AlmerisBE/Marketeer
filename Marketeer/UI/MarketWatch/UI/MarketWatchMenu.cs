using Dalamud.Bindings.ImGui;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.API.MarketWatch.Models;
using Marketeer.API.SalesHistory.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.MarketWatch.UI;

public class MarketWatchMenu : INavigationNode {
    private IMarketWatchRepository repository;
    private ILocalizationService localization;
    private IItemResolverService itemResolver;
    private string newItemName = string.Empty;

    public string GroupName => this.localization.Translate("Group_MarketWatch");
    public string Name => this.localization.Translate("MarketWatch_TabName");
    public int Priority => 50;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public MarketWatchMenu(
        IMarketWatchRepository repository,
        ILocalizationService localization,
        IItemResolverService itemResolver) {

        this.repository = repository;
        this.localization = localization;
        this.itemResolver = itemResolver;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        ImGui.TextUnformatted(this.localization.Translate("MarketWatch_Header"));
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(250f);
        ImGui.InputText("##newItemInput", ref this.newItemName, 100);
        ImGui.SameLine();

        if (ImGui.Button(this.localization.Translate("MarketWatch_BtnAdd"))) {
            var itemId = this.itemResolver.ResolveItemId(this.newItemName.Trim());
            if (itemId > 0) {
                var item = new WatchedItem { ItemId = itemId };
                this.repository.AddOrUpdateItem(item);
                this.newItemName = string.Empty;
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var items = this.repository.GetAllWatchedItems();

        if (items.Count == 0) {
            ImGui.TextDisabled(this.localization.Translate("MarketWatch_NoItems"));
            return;
        }

        if (ImGui.BeginTable("MarketWatchTable", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColItem"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColBuyWatch"), ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColTargetBuy"), ImGuiTableColumnFlags.WidthFixed, 120f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColSellWatch"), ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColTargetSell"), ImGuiTableColumnFlags.WidthFixed, 120f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColActions"), ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableHeadersRow();

            foreach (var item in items) {
                ImGui.TableNextRow();

                // Column 1: Item Name
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(this.itemResolver.ResolveItemName(item.ItemId));

                // Column 2: Buy Watch Toggle
                ImGui.TableNextColumn();
                bool buyEnabled = item.IsBuyWatchEnabled;
                if (ImGui.Checkbox($"##buyToggle_{item.ItemId}", ref buyEnabled)) {
                    item.IsBuyWatchEnabled = buyEnabled;
                    this.repository.AddOrUpdateItem(item);
                }

                // Column 3: Target Buy Price
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                int buyPrice = item.TargetBuyPrice.HasValue ? (int)item.TargetBuyPrice.Value : 0;
                if (ImGui.InputInt($"##buyPrice_{item.ItemId}", ref buyPrice, 0, 0)) {
                    item.TargetBuyPrice = buyPrice > 0 ? (uint)buyPrice : null;
                    this.repository.AddOrUpdateItem(item);
                }

                // Column 4: Sell Watch Toggle
                ImGui.TableNextColumn();
                bool sellEnabled = item.IsSellWatchEnabled;
                if (ImGui.Checkbox($"##sellToggle_{item.ItemId}", ref sellEnabled)) {
                    item.IsSellWatchEnabled = sellEnabled;
                    this.repository.AddOrUpdateItem(item);
                }

                // Column 5: Target Sell Price
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                int sellPrice = item.TargetSellPrice.HasValue ? (int)item.TargetSellPrice.Value : 0;
                if (ImGui.InputInt($"##sellPrice_{item.ItemId}", ref sellPrice, 0, 0)) {
                    item.TargetSellPrice = sellPrice > 0 ? (uint)sellPrice : null;
                    this.repository.AddOrUpdateItem(item);
                }

                // Column 6: Actions
                ImGui.TableNextColumn();
                if (ImGui.Button($"{this.localization.Translate("MarketWatch_BtnRemove")}##{item.ItemId}", new Vector2(-1, 0))) {
                    this.repository.RemoveItem(item.ItemId);
                }
            }

            ImGui.EndTable();
        }
    }
}