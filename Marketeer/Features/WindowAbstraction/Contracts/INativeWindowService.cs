using System.Collections.Generic;

namespace Marketeer.Features.WindowAbstraction.Contracts;

public interface INativeWindowService {
    IEnumerable<INativeWindow> GetOpenWindows();
    INativeWindow? GetWindow(string name);
    INativeWindow? GetFocusedWindow();
}