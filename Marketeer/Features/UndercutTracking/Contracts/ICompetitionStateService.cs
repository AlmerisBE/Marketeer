using Marketeer.Features.UndercutTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.UndercutTracking.Contracts;

public interface ICompetitionStateService {
    IReadOnlyList<UndercutItem> GetUndercutItems();
    void UpdateUndercuts(IEnumerable<UndercutItem> undercuts);
}