namespace Marketeer.API.UiInterop.Contracts;

public interface IWindowTrackerService {
    bool IsTracking { get; }
    void EnableTracking();
    void DisableTracking();
}