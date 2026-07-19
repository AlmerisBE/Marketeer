namespace Marketeer.Features.Dashboard.Contracts;

public interface IDashboardTab {
    string Name { get; }
    void Draw();
}