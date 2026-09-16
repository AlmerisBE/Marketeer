using Marketeer.Core.CharacterManagement.Contracts;
using Marketeer.Core.CharacterManagement.Models;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.UI.CharacterManagement.UI;

public class RetainerDataPresenter : IRetainerDataPresenter {
    private IRetainerTrackerService retainerTrackerService;

    public RetainerDataPresenter(IRetainerTrackerService retainerTrackerService) {
        this.retainerTrackerService = retainerTrackerService;
    }

    public IReadOnlyList<RetainerDisplayData> GetRetainers(string characterName, uint homeWorldId) {
        return this.retainerTrackerService.GetRetainersForCharacter(characterName, homeWorldId)
            .Select(r => new RetainerDisplayData {
                RetainerId = r.RetainerId,
                Name = r.Name,
                MarketItemCount = r.MarketItemCount,
                Gil = r.Gil
            })
            .ToList();
    }
}