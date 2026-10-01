using Marketeer.UI.CompetitionTracking.Models;
using System.Collections.Generic;

namespace Marketeer.UI.CompetitionTracking.Contracts;

public interface ICompetitionStateService {
    IReadOnlyList<UndercutItem> GetUndercutItems();
}