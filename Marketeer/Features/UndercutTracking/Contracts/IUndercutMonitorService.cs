using System;
using System.Threading.Tasks;

namespace Marketeer.Features.UndercutTracking.Contracts;

public interface IUndercutMonitorService : IDisposable {
    void StartMonitoring();
    void StopMonitoring();
    Task CheckUndercutsAsync();
}