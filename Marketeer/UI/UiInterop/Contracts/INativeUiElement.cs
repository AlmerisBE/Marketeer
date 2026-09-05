namespace Marketeer.UI.UiInterop.Contracts;

public interface INativeUiElement {
    string Text { get; }
    NativeUiElementType Type { get; }
    uint NodeId { get; }

    void Click();
}