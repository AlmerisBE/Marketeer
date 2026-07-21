using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.CompetitionTracking.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.CompetitionTracking.Services;

public class CompetitionStateService : ICompetitionStateService {
    private IReadOnlyList<UndercutItem> undercuts = new List<UndercutItem>();

    public IReadOnlyList<UndercutItem> GetUndercutItems() {
        return this.undercuts;
    }

    public void UpdateUndercuts(IEnumerable<UndercutItem> undercuts) {
        this.undercuts = undercuts.ToList().AsReadOnly();
    }

    public void UpdateItemUndercuts(uint itemId, IEnumerable<UndercutItem> undercutsForItem) {
        var currentList = this.undercuts.Where(u => u.ItemId != itemId).ToList();
        currentList.AddRange(undercutsForItem);
        this.undercuts = currentList.AsReadOnly();
    }
}