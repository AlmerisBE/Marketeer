using Marketeer.UI.Dashboard.Contracts;

namespace Marketeer.Core.Dashboard.Services;

public class DashboardNavigationService : IDashboardNavigationService {
    public INavigationNode? SelectedNode { get; private set; }

    public void NavigateTo(INavigationNode node) {
        this.SelectedNode = node;
    }
}