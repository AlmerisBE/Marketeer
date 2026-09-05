namespace Marketeer.UI.UiInterop.Contracts;

public interface IWindowTrackerService {
    bool IsTracking { get; }
    void EnableTracking();
    void DisableTracking();
}