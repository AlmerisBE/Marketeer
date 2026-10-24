using Dalamud.Interface;
using System.Collections.Generic;

namespace Marketeer.UI.Shell.Contracts;

public interface INavigationNode {
    string GroupName { get; }
    FontAwesomeIcon? GroupIcon { get; }

    string Name { get; }
    FontAwesomeIcon? Icon { get; }

    int Priority { get; }
    bool HasContent { get; }
    bool DefaultExpanded { get; }

    IEnumerable<INavigationNode> GetChildren();
    void DrawContent();
}