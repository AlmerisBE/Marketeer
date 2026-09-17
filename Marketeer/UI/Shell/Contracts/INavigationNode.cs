using System.Collections.Generic;

namespace Marketeer.UI.Shell.Contracts;

public interface INavigationNode {
    string GroupName { get; }
    string Name { get; }
    int Priority { get; }
    bool HasContent { get; }
    bool DefaultExpanded { get; }

    IEnumerable<INavigationNode> GetChildren();
    void DrawContent();
}