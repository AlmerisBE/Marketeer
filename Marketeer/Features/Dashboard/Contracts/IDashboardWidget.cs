namespace Marketeer.Features.Dashboard.Contracts;

public interface IDashboardWidget {
    string Name { get; }
    void Draw();
}