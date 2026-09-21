namespace Marketeer.API.UiInterop.Contracts;

public interface IWindowHierarchyProvider {
    string? GetParentWindowName(string childWindowName);
}