namespace Marketeer.Features.SalesHistoryUI.Contracts;

public interface ISalesTab {
    string TabName { get; }
    void Draw();
}