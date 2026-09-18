namespace Marketeer.UI.Shell.Contracts;

public interface INavigationService {
    INavigationNode? SelectedNode { get; }
    void NavigateTo(INavigationNode node);
}