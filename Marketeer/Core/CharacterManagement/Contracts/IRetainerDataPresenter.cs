using Marketeer.Core.CharacterManagement.Models;
using System.Collections.Generic;

namespace Marketeer.Core.CharacterManagement.Contracts;

public interface IRetainerDataPresenter {
    IReadOnlyList<RetainerDisplayData> GetRetainers(string characterName, uint homeWorldId);
}