using Marketeer.API.CharacterManagement.Models;
using System.Collections.Generic;

namespace Marketeer.API.CharacterManagement.Contracts;

public interface IRetainerDataPresenter {
    IReadOnlyList<RetainerDisplayData> GetRetainers(string characterName, uint homeWorldId);
}