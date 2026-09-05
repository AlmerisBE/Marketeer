using System.Collections.Generic;

namespace Marketeer.UI.UiInterop.Contracts;

public interface INativeWindowService {
    IEnumerable<INativeWindow> GetOpenWindows();
    INativeWindow? GetWindow(string name);
    INativeWindow? GetFocusedWindow();
}