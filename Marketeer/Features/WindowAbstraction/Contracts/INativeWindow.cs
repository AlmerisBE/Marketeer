using System.Collections.Generic;

namespace Marketeer.Features.WindowAbstraction.Contracts;

public interface INativeWindow {
    string Name { get; }
    WindowType Type { get; }
    bool IsVisible { get; }
    INativeWindow? Parent { get; }

    void Close(bool closeHierarchy = true);
    IEnumerable<INativeUiElement> GetElements();

    /// <summary>
    /// Sends a raw callback event directly to the FFXIV Addon.
    /// The first argument is typically the Event/Callback ID.
    /// </summary>
    void SendCallback(params object[] args);
}