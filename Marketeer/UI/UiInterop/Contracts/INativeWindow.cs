using System.Collections.Generic;

namespace Marketeer.UI.UiInterop.Contracts;

public interface INativeWindow {
    bool IsVisible { get; }
    string Name { get; }
    WindowType Type { get; }

    IEnumerable<INativeUiElement> GetElements();

    void SendCallback(params object[] arguments);

    void SendCallbackWithUpdateState(bool updateState, params object[] arguments);

    void Close();
}