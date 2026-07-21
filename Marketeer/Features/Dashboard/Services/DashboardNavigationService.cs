using Marketeer.Features.Dashboard.Contracts;

namespace Marketeer.Features.Dashboard.Services;

public class DashboardNavigationService : IDashboardNavigationService {
    public INavigationNode? SelectedNode { get; private set; }

    public void NavigateTo(INavigationNode node) {
        this.SelectedNode = node;
    }
}