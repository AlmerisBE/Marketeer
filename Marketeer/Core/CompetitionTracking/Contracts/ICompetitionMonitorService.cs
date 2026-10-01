using Marketeer.UI.CompetitionTracking.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marketeer.Core.CompetitionTracking.Contracts;

public interface ICompetitionMonitorService : IDisposable {
    void StartMonitoring();
    void StopMonitoring();
    Task CheckUndercutsAsync();
    Task CheckUndercutForItemAsync(uint itemId);
    IReadOnlyList<UndercutItem> GetInternalUndercutItems();
}