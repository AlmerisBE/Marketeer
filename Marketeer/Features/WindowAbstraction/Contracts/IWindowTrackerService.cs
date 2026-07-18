namespace Marketeer.Features.WindowAbstraction.Contracts;

public interface IWindowTrackerService {
    bool IsTracking { get; }
    void EnableTracking();
    void DisableTracking();
}