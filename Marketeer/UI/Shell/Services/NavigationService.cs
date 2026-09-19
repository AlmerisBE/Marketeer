using Marketeer.UI.Shell.Contracts;

namespace Marketeer.UI.Shell.Services;

public class NavigationService : INavigationService {
    public INavigationNode? CurrentNode { get; private set; }

    public void NavigateTo(INavigationNode node) {
        if (node != null) this.CurrentNode = node;
    }

    public void Draw() {
        if (this.CurrentNode != null) this.CurrentNode.DrawContent();
    }
}