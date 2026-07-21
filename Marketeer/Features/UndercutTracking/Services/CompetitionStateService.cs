using Marketeer.Features.UndercutTracking.Contracts;
using Marketeer.Features.UndercutTracking.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.UndercutTracking.Services;

public class CompetitionStateService : ICompetitionStateService {
    private IReadOnlyList<UndercutItem> undercuts = new List<UndercutItem>();

    public IReadOnlyList<UndercutItem> GetUndercutItems() {
        return this.undercuts;
    }

    public void UpdateUndercuts(IEnumerable<UndercutItem> undercuts) {
        // Reassigning the reference ensures atomic updates for the UI thread
        this.undercuts = undercuts.ToList().AsReadOnly();
    }
}