namespace Marketeer.API.Dashboard.Contracts;

public interface IDashboardNavigationService {
    INavigationNode? SelectedNode { get; }
    void NavigateTo(INavigationNode node);
}