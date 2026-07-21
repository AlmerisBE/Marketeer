using Dalamud.Bindings.ImGui;
using Marketeer.API.CharacterManagement.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.GameData.Contracts;
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

    public string Name => "Personnages";
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
        IMarketListingTrackerService marketListingTrackerService) {

        this.trackerService = trackerService;
        this.worldDataPresenter = worldDataPresenter;
        this.navigationService = navigationService;
        this.characterSummaryView = characterSummaryView;
        this.retainerDataPresenter = retainerDataPresenter;
        this.retainerDetailsView = retainerDetailsView;
        this.marketListingTrackerService = marketListingTrackerService;
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
            ImGui.TextDisabled("Aucun personnage suivi pour le moment.");
            return;
        }

        ImGui.TextUnformatted("Aperçu de vos personnages");
        ImGui.Separator();
        ImGui.Spacing();

        var nodes = this.GetChildren().ToList();

        foreach (CharacterNode node in nodes) {
            var charData = node.Character;
            var worldName = this.worldDataPresenter.GetWorldName(charData.HomeWorldId);

            if (ImGui.BeginChild($"card_{charData.Name}", new Vector2(0, 110), true)) {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"{charData.Name} ({worldName})");
                ImGui.Separator();

                ImGui.TextUnformatted($"Compagnie Libre : {(string.IsNullOrWhiteSpace(charData.CompanyTag) ? "Aucune" : $"<{charData.CompanyTag}>")}");
                ImGui.TextUnformatted($"Servants actifs : {charData.RetainerCount}");
                ImGui.TextUnformatted($"Dernier scan : {charData.LastScanDate:g}");

                ImGui.SetCursorPos(new Vector2(ImGui.GetWindowWidth() - 100, ImGui.GetWindowHeight() - 35));
                if (ImGui.Button("Ouvrir", new Vector2(90, 24))) {
                    this.navigationService.NavigateTo(node);
                }

                ImGui.EndChild();
            }
        }
    }
}