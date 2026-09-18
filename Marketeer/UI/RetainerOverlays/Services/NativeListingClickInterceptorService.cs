using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using System;
using System.Linq;

namespace Marketeer.UI.RetainerOverlays.Services;

public class NativeListingClickInterceptorService : IDisposable {
    private IAddonLifecycle addonLifecycle;
    private IMarketListingProvider listingProvider;
    private IHybridAutomationService hybridAutomation;
    private ILoggerService logger;

    public NativeListingClickInterceptorService(
        IAddonLifecycle addonLifecycle,
        IMarketListingProvider listingProvider,
        IHybridAutomationService hybridAutomation,
        ILoggerService logger) {

        this.addonLifecycle = addonLifecycle;
        this.listingProvider = listingProvider;
        this.hybridAutomation = hybridAutomation;
        this.logger = logger;

        this.addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "RetainerSellList", this.OnReceiveEvent);
    }

    private void OnReceiveEvent(AddonEvent type, AddonArgs args) {
        if (this.hybridAutomation.IsActive) return;

        if (args is AddonReceiveEventArgs receiveArgs) {
            int eventType = (int)receiveArgs.AtkEventType;

            if (eventType == 35) {
                uint uiIndex = (uint)receiveArgs.EventParam;
                this.logger.Debug($"[ClickInterceptor] Native list click detected (Type 35). Target UI Index: {uiIndex}");

                var activeListings = this.listingProvider.GetActiveRetainerListings();
                if (activeListings.Count == 0) return;

                var targetListing = activeListings.FirstOrDefault(l => l.SlotIndex == uiIndex);

                if (targetListing != null) {
                    this.logger.Info($"[ClickInterceptor] Exact match found for '{targetListing.ItemName}' at Slot {uiIndex}. Launching hybrid automation.");
                    this.hybridAutomation.TriggerAdjustment(targetListing);
                }
            }
        }
    }

    public void Dispose() {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "RetainerSellList", this.OnReceiveEvent);
    }
}