using Marketeer.UI.Shell.Contracts;

namespace Marketeer.UI.Shell.Services;

public class NavigationService : INavigationService {
    public INavigationNode? SelectedNode { get; private set; }

    public void NavigateTo(INavigationNode node) {
        this.SelectedNode = node;
    }
}