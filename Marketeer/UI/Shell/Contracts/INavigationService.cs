namespace Marketeer.UI.Shell.Contracts;

public interface INavigationService {
    INavigationNode? CurrentNode { get; }
    void NavigateTo(INavigationNode node);
    void Draw();
}