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
    private IMarketItemSearchProvider searchProvider;

    private string searchInput = string.Empty;
    private ItemSearchResult? selectedItem;

    public string GroupName => this.localization.Translate("Group_MarketWatch");
    public string Name => this.localization.Translate("MarketWatch_TabName");
    public int Priority => 50;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public MarketWatchMenu(
        IMarketWatchRepository repository,
        ILocalizationService localization,
        IItemResolverService itemResolver,
        IMarketItemSearchProvider searchProvider) {

        this.repository = repository;
        this.localization = localization;
        this.itemResolver = itemResolver;
        this.searchProvider = searchProvider;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void AddSelectedItem() {
        if (this.selectedItem != null) {
            var item = new WatchedItem { ItemId = this.selectedItem.ItemId };
            this.repository.AddOrUpdateItem(item);
            this.selectedItem = null;
            this.searchInput = string.Empty;
        }
    }

    public void DrawContent() {
        ImGui.TextUnformatted(this.localization.Translate("MarketWatch_Header"));
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(250f);
        string comboPreview = this.selectedItem != null ? this.selectedItem.Name : this.localization.Translate("MarketWatch_SelectAnItem");

        if (ImGui.BeginCombo("##itemCombo", comboPreview)) {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##searchInput", "...", ref this.searchInput, 100);

            var results = this.searchProvider.SearchMarketableItems(this.searchInput);

            foreach (var res in results) {
                if (ImGui.Selectable(res.Name)) {
                    this.selectedItem = res;
                    this.searchInput = string.Empty;
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();

        if (ImGui.Button(this.localization.Translate("MarketWatch_BtnAdd"))) {
            this.AddSelectedItem();
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

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(this.itemResolver.ResolveItemName(item.ItemId));

                ImGui.TableNextColumn();
                bool buyEnabled = item.IsBuyWatchEnabled;
                if (ImGui.Checkbox($"##buyToggle_{item.ItemId}", ref buyEnabled)) {
                    item.IsBuyWatchEnabled = buyEnabled;
                    this.repository.AddOrUpdateItem(item);
                }

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                int buyPrice = item.TargetBuyPrice.HasValue ? (int)item.TargetBuyPrice.Value : 0;
                if (ImGui.InputInt($"##buyPrice_{item.ItemId}", ref buyPrice, 0, 0)) {
                    item.TargetBuyPrice = buyPrice > 0 ? (uint)buyPrice : null;
                    this.repository.AddOrUpdateItem(item);
                }

                ImGui.TableNextColumn();
                bool sellEnabled = item.IsSellWatchEnabled;
                if (ImGui.Checkbox($"##sellToggle_{item.ItemId}", ref sellEnabled)) {
                    item.IsSellWatchEnabled = sellEnabled;
                    this.repository.AddOrUpdateItem(item);
                }

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                int sellPrice = item.TargetSellPrice.HasValue ? (int)item.TargetSellPrice.Value : 0;
                if (ImGui.InputInt($"##sellPrice_{item.ItemId}", ref sellPrice, 0, 0)) {
                    item.TargetSellPrice = sellPrice > 0 ? (uint)sellPrice : null;
                    this.repository.AddOrUpdateItem(item);
                }

                ImGui.TableNextColumn();
                if (ImGui.Button($"{this.localization.Translate("MarketWatch_BtnRemove")}##{item.ItemId}", new Vector2(-1, 0))) {
                    this.repository.RemoveItem(item.ItemId);
                }
            }

            ImGui.EndTable();
        }
    }
}