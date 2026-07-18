namespace Marketeer.Features.WindowAbstraction.Contracts;

public interface IWindowHierarchyProvider {
    string? GetParentWindowName(string childWindowName);
}