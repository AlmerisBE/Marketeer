using Dalamud.Bindings.ImGui;
using Marketeer.API.CharacterManagement.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketListings.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Marketeer.UI.CharacterManagement.UI;

public class CharacterOverviewMenu : INavigationNode {
    private ICharacterTrackerService trackerService;
    private IWorldDataPresenter worldDataPresenter;
    private IDashboardNavigationService navigationService;
    private ICharacterSummaryView characterSummaryView;
    private IRetainerDataPresenter retainerDataPresenter;
    private IRetainerDetailsView retainerDetailsView;
    private IMarketListingTrackerService marketListingTrackerService;
    private ILocalizationService localizationService;

    public string GroupName => this.localizationService.Translate("Group_Characters");
    public string Name => this.localizationService.Translate("CharacterOverview_TabName");
    public int Priority => 10;
    public bool HasContent => true;
    public bool DefaultExpanded => true;

    public CharacterOverviewMenu(
        ICharacterTrackerService trackerService,
        IWorldDataPresenter worldDataPresenter,
        IDashboardNavigationService navigationService,
        ICharacterSummaryView characterSummaryView,
        IRetainerDataPresenter retainerDataPresenter,
        IRetainerDetailsView retainerDetailsView,
        IMarketListingTrackerService marketListingTrackerService,
        ILocalizationService localizationService) {

        this.trackerService = trackerService;
        this.worldDataPresenter = worldDataPresenter;
        this.navigationService = navigationService;
        this.characterSummaryView = characterSummaryView;
        this.retainerDataPresenter = retainerDataPresenter;
        this.retainerDetailsView = retainerDetailsView;
        this.marketListingTrackerService = marketListingTrackerService;
        this.localizationService = localizationService;
    }

    public IEnumerable<INavigationNode> GetChildren() {
        var characters = this.trackerService.GetKnownCharacters();
        return characters.Select(c => new CharacterNode(
            c,
            this.worldDataPresenter,
            this.navigationService,
            this.characterSummaryView,
            this.retainerDataPresenter,
            this.retainerDetailsView,
            this.marketListingTrackerService));
    }

    public void DrawContent() {
        var characters = this.trackerService.GetKnownCharacters().ToList();

        if (characters.Count == 0) {
            ImGui.TextDisabled(this.localizationService.Translate("CharacterList_NoCharacters"));
            return;
        }

        ImGui.TextUnformatted(this.localizationService.Translate("CharacterOverview_Header"));
        ImGui.Separator();
        ImGui.Spacing();

        var nodes = this.GetChildren().ToList();
        var fcLabel = this.localizationService.Translate("CharacterOverview_FC");
        var noneLabel = this.localizationService.Translate("CharacterOverview_None");
        var activeLabel = this.localizationService.Translate("CharacterOverview_ActiveRetainers");
        var scanLabel = this.localizationService.Translate("CharacterOverview_LastScan");
        var openLabel = this.localizationService.Translate("CharacterOverview_OpenButton");

        foreach (CharacterNode node in nodes) {
            var charData = node.Character;
            var worldName = this.worldDataPresenter.GetWorldName(charData.HomeWorldId);

            if (ImGui.BeginChild($"card_{charData.Name}", new Vector2(0, 110), true)) {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"{charData.Name} ({worldName})");
                ImGui.Separator();

                var fcText = string.IsNullOrWhiteSpace(charData.CompanyTag) ? noneLabel : $"<{charData.CompanyTag}>";
                ImGui.TextUnformatted($"{fcLabel} {fcText}");
                ImGui.TextUnformatted($"{activeLabel} {charData.RetainerCount}");
                ImGui.TextUnformatted($"{scanLabel} {charData.LastScanDate:g}");

                ImGui.SetCursorPos(new Vector2(ImGui.GetWindowWidth() - 100, ImGui.GetWindowHeight() - 35));
                if (ImGui.Button(openLabel, new Vector2(90, 24))) {
                    this.navigationService.NavigateTo(node);
                }

                ImGui.EndChild();
            }
        }
    }
}