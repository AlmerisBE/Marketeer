using Marketeer.Features.CharacterTracking.Models;
using System.Collections.Generic;

namespace Marketeer.Features.CharacterTracking.Contracts;

public interface IRetainerDataPresenter {
    IReadOnlyList<RetainerDisplayData> GetRetainers(string characterName, uint homeWorldId);
}