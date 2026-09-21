using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Models;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Models;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Models;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.RetainerAutomation.Services;

public class PriceUpdateAutomationService : IPriceUpdateAutomationService, IRetainerTask {
    private IRetainerOrchestratorService orchestrator;
    private IRetainerUiInteractionService uiInteraction;
    private ICompetitionStateService competitionState;
    private IInventoryService inventoryService;
    private IObjectTable objectTable;
    private ILoggerService logger;
    private IConfigurationService configService;
    private ILocalizationService localization;

    private Dictionary<string, Queue<UndercutItem>> tasksByRetainer;
    private UndercutItem? currentItemTask;

    private DateTime actionAvailableAt;
    private DateTime currentItemStartTime;
    private string currentRetainerName;
    private int step;

    public bool IsUpdating => this.orchestrator.IsActive;

    public PriceUpdateAutomationService(
        IRetainerOrchestratorService orchestrator,
        IRetainerUiInteractionService uiInteraction,
        ICompetitionStateService competitionState,
        IInventoryService inventoryService,
        IObjectTable objectTable,
        ILoggerService logger,
        IConfigurationService configService,
        ILocalizationService localization) {

        this.orchestrator = orchestrator;
        this.uiInteraction = uiInteraction;
        this.competitionState = competitionState;
        this.inventoryService = inventoryService;
        this.objectTable = objectTable;
        this.logger = logger;
        this.configService = configService;
        this.localization = localization;

        this.tasksByRetainer = new Dictionary<string, Queue<UndercutItem>>();
        this.currentRetainerName = string.Empty;
    }

    public void TriggerPriceUpdate() {
        if (this.IsUpdating) return;

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) {
            this.logger.Warning("Cannot start price update: No local player found.");
            return;
        }

        var playerName = localPlayer.Name.TextValue;
        var allUndercuts = this.competitionState.GetUndercutItems();
        var myUndercuts = allUndercuts.Where(u => u.CharacterName == playerName).ToList();

        if (!myUndercuts.Any()) {
            this.logger.Info("No undercuts detected for current character. Price update aborted.");
            return;
        }

        this.tasksByRetainer = myUndercuts
            .GroupBy(u => u.RetainerName)
            .ToDictionary(g => g.Key, g => new Queue<UndercutItem>(g));

        var retainers = this.tasksByRetainer.Keys.ToList();
        this.orchestrator.StartOrchestration(retainers, RetainerTargetMenu.MarketListings, this);
    }

    public void TriggerSingleItemUpdate(uint itemId, uint? price = null, uint? quantity = null) {
        if (this.IsUpdating) return;

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.Name == null) {
            this.logger.Warning("Cannot start targeted price update: No local player found.");
            return;
        }

        var normalizedItemId = itemId > 1000000u ? itemId - 1000000u : itemId;
        var playerName = localPlayer.Name.TextValue;
        var allUndercuts = this.competitionState.GetUndercutItems();

        var targetUndercut = allUndercuts.FirstOrDefault(u =>
            u.CharacterName == playerName &&
            u.ItemId == normalizedItemId &&
            (!price.HasValue || u.Price == price.Value) &&
            (!quantity.HasValue || u.Quantity == quantity.Value));

        if (targetUndercut == null) {
            this.logger.Info($"No undercut detected for item {normalizedItemId} on current character. Single update aborted.");
            return;
        }

        this.tasksByRetainer = new Dictionary<string, Queue<UndercutItem>> {
            { targetUndercut.RetainerName, new Queue<UndercutItem>(new[] { targetUndercut }) }
        };

        this.orchestrator.StartOrchestration(new[] { targetUndercut.RetainerName }, RetainerTargetMenu.MarketListings, this);
    }

    public void AbortUpdate() {
        this.orchestrator.Abort();
    }

    public void OnMenuOpened(string retainerName) {
        this.currentRetainerName = retainerName;
        this.ProcessNextItem();
    }

    public bool OnTick() {
        if (DateTime.Now < this.actionAvailableAt) return false;

        if (this.currentItemTask == null) return this.ProcessNextItem();

        if (DateTime.Now - this.currentItemStartTime > TimeSpan.FromSeconds(5)) {
            this.logger.Error($"Timeout while processing {this.currentItemTask.ItemName}. Attempting recovery.");
            this.uiInteraction.CloseUnexpectedWindows();
            return this.ProcessNextItem();
        }

        uint targetPrice = this.currentItemTask.TargetPrice;
        uint currentPrice = this.inventoryService.GetRetainerMarketItemPrice(this.currentItemTask.SlotIndex);

        if (currentPrice == targetPrice && this.currentItemTask.SuggestedAction != PricingAction.CancelListing) {
            this.logger.Info($"Price for '{this.currentItemTask.ItemName}' successfully updated. Moving to next.");
            return this.ProcessNextItem();
        }

        switch (this.step) {
            case 0:
                var uiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem(this.currentItemTask.SlotIndex);
                if (uiIndex != -1) {
                    this.uiInteraction.SelectItemInSellList(uiIndex);
                    this.step = 1;
                    this.SetDelay(0.2);
                }
                else {
                    this.logger.Warning($"Cannot find UI index for {this.currentItemTask.ItemName}. Skipping.");
                    return this.ProcessNextItem();
                }
                break;

            case 1:
                if (this.currentItemTask.SuggestedAction == PricingAction.CancelListing) {
                    if (this.uiInteraction.IsAddonReady("ContextMenu")) {
                        var returnText = this.localization.Translate("RetainerMenu_ReturnToInventory");
                        var menuIndex = this.uiInteraction.GetContextMenuItemIndex(returnText);

                        if (menuIndex == -1) menuIndex = 1;

                        this.logger.Info($"Loss prevention triggered. Cancelling listing for '{this.currentItemTask.ItemName}'.");
                        this.uiInteraction.SelectContextMenuItem(menuIndex);
                        this.step = 2;
                        this.SetDelay(0.5); // Wait for the item to disappear from the native UI
                    }
                }
                else {
                    if (this.uiInteraction.IsAddonReady("RetainerSell")) {
                        this.uiInteraction.CloseItemSearchResult();
                        this.uiInteraction.ConfirmPriceUpdate(targetPrice);
                        this.step = 2;
                        this.SetDelay(0.5);
                    }
                    else if (this.uiInteraction.IsAddonReady("ContextMenu")) {
                        var adjustPriceText = this.localization.Translate("RetainerMenu_AdjustPrice");
                        var menuIndex = this.uiInteraction.GetContextMenuItemIndex(adjustPriceText);

                        if (menuIndex == -1) menuIndex = 0;

                        this.uiInteraction.SelectContextMenuItem(menuIndex);
                        this.SetDelay(0.2);
                    }
                }
                break;

            case 2:
                // Implicit wait for server sync and inventory update
                // If the item was cancelled, the slot checking at the start of OnTick handles progression
                break;
        }

        return false;
    }

    private bool ProcessNextItem() {
        if (!this.tasksByRetainer.TryGetValue(this.currentRetainerName, out var queue) || queue.Count == 0) {
            this.currentItemTask = null;
            return true;
        }

        this.currentItemTask = queue.Dequeue();
        this.currentItemStartTime = DateTime.Now;
        this.step = 0;
        this.SetDelay(0.5);
        return false;
    }

    private void SetDelay(double waitSeconds) {
        var config = this.configService.GetConfig();
        double randomDelay = 0;

        if (config.EnableAutomationDelay) {
            var min = config.AutomationDelayMin;
            var max = config.AutomationDelayMax;
            if (min > max) min = max;

            randomDelay = min + (new Random().NextDouble() * (max - min));
        }

        this.actionAvailableAt = DateTime.Now.AddSeconds(waitSeconds + randomDelay);
    }

    public void OnMenuClosed(string retainerName) { }

    public void OnAbort() {
        this.tasksByRetainer.Clear();
    }
}