namespace Marketeer.UI.Shell.Contracts;

public interface IStatusBarProvider {
    int Priority { get; }
    void Draw();
}