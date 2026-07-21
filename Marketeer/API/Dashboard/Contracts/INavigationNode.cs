using System.Collections.Generic;

namespace Marketeer.API.Dashboard.Contracts;

public interface INavigationNode {
    // The display name of the node in the tree menu
    string Name { get; }

    // Sorting priority for root nodes or siblings (lower comes first)
    int Priority { get; }

    // Indicates if clicking this node should render a specific view on the right panel.
    // If false, it acts purely as an organizational folder.
    bool HasContent { get; }

    // Determines if the node should be expanded by default in the UI
    bool DefaultExpanded { get; }

    // Retrieves the dynamic or static sub-nodes. 
    // Returning an empty collection means this is a leaf node.
    IEnumerable<INavigationNode> GetChildren();

    // The drawing logic for the main content area.
    // Invoked only if the node is currently selected and HasContent is true.
    void DrawContent();
}