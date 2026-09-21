using System.Collections.Generic;

namespace Marketeer.API.UiInterop.Contracts;

public interface INativeWindowService {
    IEnumerable<INativeWindow> GetOpenWindows();
    INativeWindow? GetWindow(string name);
    INativeWindow? GetFocusedWindow();
}