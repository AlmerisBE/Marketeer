using System;

namespace Marketeer.UI.Shell.Contracts;

public interface IUniversalisUpdateState {
    bool IsUpdating { get; }
    DateTime? LastUpdateTime { get; }
}