using Marketeer.Core.CharacterManagement.Models;
using Marketeer.Core.MarketListings.Contracts;
using Marketeer.UI.Shell.Contracts;
using System.Collections.Generic;

namespace Marketeer.UI.CharacterManagement.UI;

public class RetainerNode : INavigationNode {
    public RetainerDisplayData Retainer { get; }
    private IRetainerDetailsView detailsView;

    public string GroupName => string.Empty;
    public string Name => this.Retainer.Name;
    public int Priority => 0;
    public bool HasContent => true;
    public bool DefaultExpanded => false;

    public RetainerNode(RetainerDisplayData retainer, IRetainerDetailsView detailsView) {
        this.Retainer = retainer;
        this.detailsView = detailsView;
    }

    public IEnumerable<INavigationNode> GetChildren() => [];

    public void DrawContent() {
        this.detailsView.Draw(this.Retainer);
    }
}