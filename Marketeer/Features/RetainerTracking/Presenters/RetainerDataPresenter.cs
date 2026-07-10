using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Models;
using Marketeer.Features.RetainerTracking.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Features.RetainerTracking.Presenters;

public class RetainerDataPresenter : IRetainerDataPresenter {
    private IRetainerTrackerService retainerTrackerService;

    public RetainerDataPresenter(IRetainerTrackerService retainerTrackerService) {
        this.retainerTrackerService = retainerTrackerService;
    }

    public IReadOnlyList<RetainerDisplayData> GetRetainers(string characterName, uint homeWorldId) {
        return this.retainerTrackerService.GetRetainersForCharacter(characterName, homeWorldId)
            .Select(r => new RetainerDisplayData {
                RetainerId = r.RetainerId,
                Name = r.Name
            })
            .ToList();
    }
}