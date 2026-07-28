using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.InventoryTracking.Models;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.SalesHistory.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.InventoryTracking.UI;

public class InventoryView : INavigationNode {
    private IConfigurationService configService;
    private ILocalizationService localization;
    private IItemResolverService itemResolver;
    private ITextureProvider textureProvider;

    private string targetCharacterName = string.Empty;
    private uint targetHomeWorldId;
    private ulong targetRetainerId;
    private string targetRetainerName = string.Empty;
    private bool isRetainerMode;

    public string GroupName => string.Empty;
    public string Name => this.isRetainerMode
        ? this.localization.Translate("InventoryTab_RetainerInventory", this.targetRetainerName)
        : this.localization.Translate("InventoryTab_PlayerInventory", this.targetCharacterName);

    public int Priority => 0;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public IEnumerable<INavigationNode> GetChildren() => [];

    public InventoryView(
        IConfigurationService configService,
        ILocalizationService localization,
        IItemResolverService itemResolver,
        ITextureProvider textureProvider) {

        this.configService = configService;
        this.localization = localization;
        this.itemResolver = itemResolver;
        this.textureProvider = textureProvider;
    }

    public void OpenForCharacter(string characterName, uint homeWorldId) {
        this.targetCharacterName = characterName;
        this.targetHomeWorldId = homeWorldId;
        this.isRetainerMode = false;
    }

    public void OpenForRetainer(ulong retainerId, string retainerName) {
        this.targetRetainerId = retainerId;
        this.targetRetainerName = retainerName;
        this.isRetainerMode = true;
    }

    public void DrawContent() {
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), this.Name);
        ImGui.Separator();
        ImGui.Spacing();

        if (this.isRetainerMode) {
            this.DrawRetainer();
        }
        else {
            this.DrawCharacter();
        }
    }

    private void DrawCharacter() {
        var config = this.configService.GetConfig();
        var key = $"{this.targetCharacterName}_{this.targetHomeWorldId}";

        if (!config.InventorySnapshots.TryGetValue(key, out var snapshot)) {
            ImGui.TextDisabled(this.localization.Translate("InventoryTab_NoData"));
            return;
        }

        ImGui.TextDisabled(this.localization.Translate("InventoryTab_LastUpdated", snapshot.Timestamp.ToString("g")));
        ImGui.Spacing();
        this.DrawTable(snapshot.Items);
    }

    private void DrawRetainer() {
        var config = this.configService.GetConfig();

        if (config.RetainerInventorySnapshots == null || !config.RetainerInventorySnapshots.TryGetValue(this.targetRetainerId, out var snapshot)) {
            ImGui.TextDisabled(this.localization.Translate("InventoryTab_NoData"));
            return;
        }

        ImGui.TextDisabled(this.localization.Translate("InventoryTab_LastUpdated", snapshot.Timestamp.ToString("g")));
        ImGui.Spacing();
        this.DrawTable(snapshot.Items);
    }

    private void DrawTable(IEnumerable<TrackedItem> items) {
        if (ImGui.BeginTable("InventoryTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, -1))) {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 24f);
            ImGui.TableSetupColumn(this.localization.Translate("InventoryTab_ColName"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.localization.Translate("InventoryTab_ColQuantity"), ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableHeadersRow();

            foreach (var item in items.OrderBy(i => i.ContainerId).ThenBy(i => i.SlotIndex)) {
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