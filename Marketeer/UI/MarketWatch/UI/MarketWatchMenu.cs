using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models;
using Marketeer.Core.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Models;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.UI.MarketWatch.UI;

public class MarketWatchMenu : INavigationNode {
    private IMarketWatchRepository repository;
    private ILocalizationService localization;
    private IItemResolverService itemResolver;
    private IMarketItemSearchProvider searchProvider;

    private string searchInput = string.Empty;
    private ItemSearchResult? selectedItem;
    private bool isHqSearch = false;

    public string GroupName => this.localization.Translate("Group_MarketWatch") ?? "Market Watch";
    public string Name => this.localization.Translate("Menu_Watchlist") ?? "Watchlist";
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
            var item = new WatchedItem {
                ItemId = this.selectedItem.ItemId,
                IsHighQuality = this.isHqSearch
            };
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
        ImGui.Checkbox("HQ", ref this.isHqSearch);

        ImGui.SameLine();
        if (ImGui.Button(this.localization.Translate("MarketWatch_BtnAdd"))) {
            this.AddSelectedItem();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var items = this.repository.GetAllWatchedItems()
            .OrderBy(i => this.itemResolver.ResolveItemName(i.ItemId))
            .ThenByDescending(i => i.IsHighQuality)
            .ToList();

        if (items.Count == 0) {
            ImGui.TextDisabled(this.localization.Translate("MarketWatch_NoItems"));
            return;
        }

        if (ImGui.BeginTable("MarketWatchTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColItem"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColTargetBuy"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColTargetSell"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColNotify"), ImGuiTableColumnFlags.WidthFixed, 50f);
            ImGui.TableSetupColumn(this.localization.Translate("MarketWatch_ColActions"), ImGuiTableColumnFlags.WidthFixed, 35f);
            ImGui.TableHeadersRow();

            foreach (var item in items) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                string hqSymbol = item.IsHighQuality ? " \uE03C" : "";
                ImGui.TextUnformatted($"{this.itemResolver.ResolveItemName(item.ItemId)}{hqSymbol}");

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                int buyPrice = item.TargetBuyPrice.HasValue ? (int)item.TargetBuyPrice.Value : 0;
                if (ImGui.InputInt($"##buyPrice_{item.Key}", ref buyPrice, 0, 0)) {
                    item.TargetBuyPrice = buyPrice > 0 ? (uint)buyPrice : null;
                    this.repository.AddOrUpdateItem(item);
                }

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                int sellPrice = item.TargetSellPrice.HasValue ? (int)item.TargetSellPrice.Value : 0;
                if (ImGui.InputInt($"##sellPrice_{item.Key}", ref sellPrice, 0, 0)) {
                    item.TargetSellPrice = sellPrice > 0 ? (uint)sellPrice : null;
                    this.repository.AddOrUpdateItem(item);
                }

                ImGui.TableNextColumn();
                bool notify = item.EnableNotifications;
                if (ImGui.Checkbox($"##notify_{item.Key}", ref notify)) {
                    item.EnableNotifications = notify;
                    this.repository.AddOrUpdateItem(item);
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip(this.localization.Translate("MarketWatch_TooltipNotify"));
                }

                ImGui.TableNextColumn();
                ImGui.PushID(item.Key);
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash)) {
                    this.repository.RemoveItem(item.ItemId, item.IsHighQuality);
                }

                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip(this.localization.Translate("MarketWatch_BtnRemove"));
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }
}