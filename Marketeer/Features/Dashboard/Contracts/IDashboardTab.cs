namespace Marketeer.Features.Dashboard.Contracts;

public interface IDashboardTab {
    string Name { get; }
    int Priority { get; }
    void Draw();
}