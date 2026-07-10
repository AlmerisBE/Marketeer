using Marketeer.Features.Dashboard.Models;
using System.Collections.Generic;

namespace Marketeer.Features.Dashboard.Contracts;

public interface IRetainerDataPresenter {
    IReadOnlyList<RetainerDisplayData> GetRetainers(string characterName, uint homeWorldId);
}