using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.InventoryBrowser.Components;
using Marketeer.UI.Localization.Contracts;
using System.Collections.Generic;
using System.Numerics;

namespace Marketeer.UI.InventoryBrowser.UI;

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

        if (this.isRetainerMode) this.DrawRetainer();
        else this.DrawCharacter();
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
        InventoryTablePresenter.DrawTable($"CharacterInvTable_{key}", snapshot.Items, this.localization, this.itemResolver, this.textureProvider);
    }

    private void DrawRetainer() {
        var config = this.configService.GetConfig();

        if (config.RetainerInventorySnapshots == null || !config.RetainerInventorySnapshots.TryGetValue(this.targetRetainerId, out var snapshot)) {
            ImGui.TextDisabled(this.localization.Translate("InventoryTab_NoData"));
            return;
        }

        ImGui.TextDisabled(this.localization.Translate("InventoryTab_LastUpdated", snapshot.Timestamp.ToString("g")));
        ImGui.Spacing();
        InventoryTablePresenter.DrawTable($"RetainerInvTable_{this.targetRetainerId}", snapshot.Items, this.localization, this.itemResolver, this.textureProvider);
    }
}