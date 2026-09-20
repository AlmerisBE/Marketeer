using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.InventoryBrowser.Components;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.InventoryBrowser.UI;

public class InventoryMenu : INavigationNode {
    private IConfigurationService configService;
    private ICharacterTrackerService characterTracker;
    private ILocalizationService localization;
    private IItemResolverService itemResolver;
    private ITextureProvider textureProvider;

    public string GroupName => this.localization.Translate("Group_Inventory");
    public string Name => this.localization.Translate("InventoryTab_Title");
    public int Priority => 15;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public InventoryMenu(
        IConfigurationService configService,
        ICharacterTrackerService characterTracker,
        ILocalizationService localization,
        IItemResolverService itemResolver,
        ITextureProvider textureProvider) {

        this.configService = configService;
        this.characterTracker = characterTracker;
        this.localization = localization;
        this.itemResolver = itemResolver;
        this.textureProvider = textureProvider;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        var config = this.configService.GetConfig();
        var characters = this.characterTracker.GetKnownCharacters();

        if (characters.Count == 0) {
            ImGui.TextDisabled(this.localization.Translate("InventoryTab_NoData"));
            return;
        }

        foreach (var character in characters) {
            var charKey = $"{character.Name}_{character.HomeWorldId}";

            // Render Player Inventory
            if (config.InventorySnapshots.TryGetValue(charKey, out var charSnapshot) && charSnapshot.Items.Count > 0) {
                if (ImGui.CollapsingHeader(this.localization.Translate("InventoryTab_PlayerInventory", character.Name), ImGuiTreeNodeFlags.DefaultOpen)) {
                    ImGui.TextDisabled(this.localization.Translate("InventoryTab_LastUpdated", charSnapshot.Timestamp.ToString("g")));
                    ImGui.Spacing();
                    InventoryTablePresenter.DrawTable($"CharInv_{charKey}", charSnapshot.Items, this.localization, this.itemResolver, this.textureProvider);
                    ImGui.Spacing();
                }
            }

            // Render Retainer Inventories
            if (config.FinancialRecords.TryGetValue(charKey, out var charData)) {
                foreach (var retainer in charData.Retainers.Values) {
                    if (ImGui.CollapsingHeader(this.localization.Translate("InventoryTab_RetainerInventory", retainer.Name))) {

                        if (config.RetainerInventorySnapshots != null &&
                            config.RetainerInventorySnapshots.TryGetValue(retainer.RetainerId, out var retSnapshot) &&
                            retSnapshot.Items.Count > 0) {

                            ImGui.TextDisabled(this.localization.Translate("InventoryTab_LastUpdated", retSnapshot.Timestamp.ToString("g")));
                            ImGui.Spacing();
                            InventoryTablePresenter.DrawTable($"RetInv_{retainer.RetainerId}", retSnapshot.Items, this.localization, this.itemResolver, this.textureProvider);
                            ImGui.Spacing();
                        }
                        else {
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().IndentSpacing);
                            ImGui.TextDisabled(this.localization.Translate("InventoryTab_NoData"));
                            ImGui.Spacing();
                        }
                    }
                }
            }
        }
    }
}