using System;

namespace Marketeer.Features.GameEvents.Contracts;

public interface IGameEventService {
    event Action? RetainerBellOpened;
}