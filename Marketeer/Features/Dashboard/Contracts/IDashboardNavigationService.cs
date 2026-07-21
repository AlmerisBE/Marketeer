namespace Marketeer.Features.Dashboard.Contracts;

public interface IDashboardNavigationService {
    INavigationNode? SelectedNode { get; }
    void NavigateTo(INavigationNode node);
}