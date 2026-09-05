namespace Marketeer.UI.UiInterop.Contracts;

public interface IWindowHierarchyProvider {
    string? GetParentWindowName(string childWindowName);
}