using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.Dashboard.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.Dashboard.UI;

public class CharacterNode : INavigationNode {
    public TrackedCharacter Character { get; }

    private IWorldDataPresenter worldDataPresenter;
    private IDashboardNavigationService navigationService;
    private ICharacterSummaryView summaryView;
    private IRetainerDataPresenter retainerDataPresenter;
    private IRetainerDetailsView retainerDetailsView;
    private IMarketListingTrackerService marketListingTrackerService;

    public string Name => this.Character.Name;
    public int Priority => 0; // Sorted by engine usually
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public CharacterNode(
        TrackedCharacter character,
        IWorldDataPresenter worldDataPresenter,
        IDashboardNavigationService navigationService,
        ICharacterSummaryView summaryView,
        IRetainerDataPresenter retainerDataPresenter,
        IRetainerDetailsView retainerDetailsView,
        IMarketListingTrackerService marketListingTrackerService) {

        this.Character = character;
        this.worldDataPresenter = worldDataPresenter;
        this.navigationService = navigationService;
        this.summaryView = summaryView;
        this.retainerDataPresenter = retainerDataPresenter;
        this.retainerDetailsView = retainerDetailsView;
        this.marketListingTrackerService = marketListingTrackerService;
    }

    public IEnumerable<INavigationNode> GetChildren() {
        var retainers = this.retainerDataPresenter.GetRetainers(this.Character.Name, this.Character.HomeWorldId);
        return retainers.Select(r => new RetainerNode(r, this.retainerDetailsView));
    }

    public void DrawContent() {
        // We pass the navigation callback so the Summary View can open the retainer directly
        this.summaryView.Draw(this.Character, retainerId => {
            var children = this.GetChildren().Cast<RetainerNode>();
            var targetNode = children.FirstOrDefault(r => r.Retainer.RetainerId == retainerId);
            if (targetNode != null) {
                this.navigationService.NavigateTo(targetNode);
            }
        });
    }
}