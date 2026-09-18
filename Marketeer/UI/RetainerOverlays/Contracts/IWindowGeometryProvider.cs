namespace Marketeer.UI.RetainerOverlays.Contracts;

public interface IWindowGeometryProvider {
    bool GetWindowGeometry(string windowName, out float x, out float y, out float width, out float height, out float scale);
}