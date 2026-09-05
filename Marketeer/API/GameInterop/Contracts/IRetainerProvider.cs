using Marketeer.Core.CharacterManagement.Models;
using System.Collections.Generic;

namespace Marketeer.API.GameInterop.Contracts;

public interface IRetainerProvider {
    // Reads active retainers from the game's unmanaged memory
    IReadOnlyList<TrackedRetainer> GetActiveRetainers();
}