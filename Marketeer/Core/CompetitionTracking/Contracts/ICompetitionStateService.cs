using Marketeer.Core.CompetitionTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Core.CompetitionTracking.Contracts;

public interface ICompetitionStateService {
    IReadOnlyList<UndercutItem> GetUndercutItems();
    void UpdateUndercuts(IEnumerable<UndercutItem> undercuts);
    void UpdateItemUndercuts(uint itemId, IEnumerable<UndercutItem> undercutsForItem);
}