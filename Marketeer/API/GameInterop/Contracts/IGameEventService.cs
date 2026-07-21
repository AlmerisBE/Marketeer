using System;

namespace Marketeer.API.GameInterop.Contracts;

public interface IGameEventService {
    event Action? RetainerBellOpened;
    event Action? RetainerSellListUpdated;
}