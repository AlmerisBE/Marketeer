using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Dashboard.Models;
using System.Collections.Generic;

namespace Marketeer.Features.Dashboard.UI;

public class RetainerNode : INavigationNode {
    public RetainerDisplayData Retainer { get; }
    private IRetainerDetailsView detailsView;

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