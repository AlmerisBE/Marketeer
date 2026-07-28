using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Financials.Models;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.InventoryTracking.UI;

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
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), this.localization.Translate("InventoryTab_PlayerInventory"));
        ImGui.TextDisabled(this.localization.Translate("InventoryTab_LastUpdated", snapshot.Timestamp.ToString("g")));
        ImGui.Separator();

        if (ImGui.BeginTable($"PlayerInv_{snapshot.CharacterName}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 300f))) {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 24f);
            ImGui.TableSetupColumn(this.localization.Translate("InventoryTab_ColName"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localization.Translate("InventoryTab_ColQuantity"), ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableHeadersRow();

            foreach (var item in snapshot.Items.OrderBy(i => i.ContainerId).ThenBy(i => i.SlotIndex)) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                this.DrawIcon(this.itemResolver.ResolveIconId(item.ItemId));

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(this.itemResolver.ResolveItemName(item.ItemId));

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.Quantity.ToString("N0"));
            }
            ImGui.EndTable();
        }
    }

    private void DrawRetainerInventory(RetainerFinancialData retainer) {
        ImGui.Spacing();
        if (ImGui.CollapsingHeader(this.localization.Translate("InventoryTab_RetainerInventory", retainer.Name))) {
            if (retainer.MarketListings.Count == 0) {
                ImGui.TextDisabled(this.localization.Translate("InventoryTab_NoRetainerData"));
                return;
            }

            if (ImGui.BeginTable($"RetainerInv_{retainer.RetainerId}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 200f))) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 24f);
                ImGui.TableSetupColumn(this.localization.Translate("InventoryTab_ColName"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.localization.Translate("InventoryTab_ColQuantity"), ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableHeadersRow();

                foreach (var listing in retainer.MarketListings.Values) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    this.DrawIcon(this.itemResolver.ResolveIconId(listing.ItemId));

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(this.itemResolver.ResolveItemName(listing.ItemId));

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(listing.Quantity.ToString("N0"));
                }
                ImGui.EndTable();
            }
        }
    }

    private void DrawIcon(uint iconId) {
        if (iconId == 0) {
            return;
        }

        var iconWrap = this.textureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrDefault();
        if (iconWrap != null) {
            ImGui.Image(iconWrap.Handle, new Vector2(24, 24));
        }
    }
}