using Dalamud.Bindings.ImGui;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorService : IDisposable {
    private IDalamudPluginInterface pluginInterface;
    private IGameGui gameGui;
    private IMarketListingProvider listingProvider;
    private IHybridAutomationService hybridAutomation;
    private ILoggerService logger;

    public NativeListingClickInterceptorService(
        IDalamudPluginInterface pluginInterface,
        IGameGui gameGui,
        IMarketListingProvider listingProvider,
        IHybridAutomationService hybridAutomation,
        ILoggerService logger) {

        this.pluginInterface = pluginInterface;
        this.gameGui = gameGui;
        this.listingProvider = listingProvider;
        this.hybridAutomation = hybridAutomation;
        this.logger = logger;

        this.pluginInterface.UiBuilder.Draw += this.OnDraw;
    }

    private unsafe void OnDraw() {
        if (this.hybridAutomation.IsActive) return;
        if (!ImGui.IsMouseClicked(ImGuiMouseButton.Left)) return;

        this.CheckForClick();
    }

    private unsafe void CheckForClick() {
        var addonPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.Address == IntPtr.Zero) return;

        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return;

        var activeListings = this.listingProvider.GetActiveRetainerListings();
        if (activeListings.Count == 0) return;

        var mousePos = ImGui.GetMousePos();

        float rootX = addon->X;
        float rootY = addon->Y;
        float scale = addon->Scale;

        this.TraverseAndHitTest(&addon->UldManager, addon, mousePos, activeListings, rootX, rootY, scale);
    }

    private unsafe bool TraverseAndHitTest(AtkUldManager* uldManager, AtkUnitBase* addon, System.Numerics.Vector2 mousePos, IReadOnlyList<Core.MarketListings.Models.TrackedListing> listings, float parentX = 0, float parentY = 0, float scale = 1f) {
        if (uldManager == null) return false;

        for (int i = uldManager->NodeListCount - 1; i >= 0; i--) {
            var node = uldManager->NodeList[i];
            if (node == null || !node->IsVisible()) continue;

            var (nodeX, nodeY) = this.GetNodeAbsolutePosition(node, parentX, parentY, scale);
            float nodeW = node->Width * scale * node->ScaleX;
            float nodeH = node->Height * scale * node->ScaleY;

            if ((ushort)node->Type >= 1000) {
                var compNode = (AtkComponentNode*)node;
                var comp = compNode->Component;

                if (comp != null) {
                    if (mousePos.X >= nodeX && mousePos.X <= nodeX + nodeW &&
                        mousePos.Y >= nodeY && mousePos.Y <= nodeY + nodeH) {

                        var textNodes = new List<nint>();
                        this.CollectVisibleTextNodes(&comp->UldManager, textNodes);

                        string? matchedItemName = null;
                        var rowNumbers = new List<uint>();

                        foreach (var textNodePtr in textNodes) {
                            var textNode = (AtkTextNode*)textNodePtr;
                            var text = this.ExtractString(textNode);

                            if (!string.IsNullOrWhiteSpace(text)) {
                                if (matchedItemName == null) matchedItemName = this.GetMatchingItemName(text, listings.Select(l => l.ItemName));

                                var digits = new string(text.Where(char.IsDigit).ToArray());
                                if (!string.IsNullOrEmpty(digits) && uint.TryParse(digits, out var parsedNum)) {
                                    rowNumbers.Add(parsedNum);
                                }
                            }
                        }

                        if (matchedItemName != null) {
                            foreach (var listing in listings) {
                                if (listing.ItemName == matchedItemName && rowNumbers.Contains(listing.PricePerUnit) && rowNumbers.Contains(listing.Quantity)) {
                                    this.logger.Info($"[ClickInterceptor] Clicked on listing: {listing.ItemName}. Launching hybrid automation.");
                                    this.hybridAutomation.TriggerAdjustment(listing);
                                    return true;
                                }
                            }
                        }

                        if (this.TraverseAndHitTest(&comp->UldManager, addon, mousePos, listings, nodeX, nodeY, scale * node->ScaleX)) return true;
                    }
                }
            }
        }
        return false;
    }

    private unsafe void CollectVisibleTextNodes(AtkUldManager* uldManager, List<nint> list) {
        if (uldManager == null) return;

        for (int i = 0; i < uldManager->NodeListCount; i++) {
            var node = uldManager->NodeList[i];
            if (node == null || !node->IsVisible()) continue;

            if (node->Type == NodeType.Text) list.Add((nint)node);
            else if ((ushort)node->Type >= 1000) {
                var compNode = (AtkComponentNode*)node;
                if (compNode->Component != null) this.CollectVisibleTextNodes(&compNode->Component->UldManager, list);
            }
        }
    }

    private unsafe (float X, float Y) GetNodeAbsolutePosition(AtkResNode* node, float parentX, float parentY, float scale) {
        float x = node->X * scale;
        float y = node->Y * scale;
        var parent = node->ParentNode;

        while (parent != null) {
            x += parent->X * scale;
            y += parent->Y * scale;
            parent = parent->ParentNode;
        }

        return (parentX + x, parentY + y);
    }

    protected virtual string? GetMatchingItemName(string uiText, IEnumerable<string> allItemNames) {
        var cleanText = uiText.Replace("\uE03C", "").Trim();
        if (string.IsNullOrEmpty(cleanText)) return null;

        bool isTruncated = cleanText.EndsWith("...") || cleanText.EndsWith("…");
        string searchName = isTruncated ? cleanText.TrimEnd('.', '…').Trim() : cleanText;

        foreach (var item in allItemNames) {
            if (isTruncated) {
                if (item.StartsWith(searchName, StringComparison.InvariantCultureIgnoreCase)) return item;
            }
            else if (item.Equals(searchName, StringComparison.InvariantCultureIgnoreCase)) return item;
        }

        return null;
    }

    private unsafe string ExtractString(AtkTextNode* textNode) {
        if (textNode == null) return string.Empty;

        byte* ptr = textNode->NodeText.StringPtr;
        if (ptr == null) return string.Empty;

        return MemoryHelper.ReadSeStringNullTerminated((nint)ptr).TextValue ?? string.Empty;
    }

    public void Dispose() {
        this.pluginInterface.UiBuilder.Draw -= this.OnDraw;
    }
}