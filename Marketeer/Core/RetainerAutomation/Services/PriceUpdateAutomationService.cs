using Dalamud.Plugin.Services;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.CompetitionTracking.Models;
using Marketeer.API.Configuration.Contracts;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.Guidance.Models;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.API.RetainerAutomation.Models;
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
    private IRetainerGuidanceService guidanceService;

    private Dictionary<string, Queue<UndercutItem>> tasksByRetainer;
    private UndercutItem? currentItemTask;

    private DateTime actionAvailableAt;
    private DateTime currentItemStartTime;
    private string currentRetainerName;

    public bool IsUpdating => this.orchestrator.IsActive;

    public PriceUpdateAutomationService(
        IRetainerOrchestratorService orchestrator,
        IRetainerUiInteractionService uiInteraction,
        ICompetitionStateService competitionState,
        IInventoryService inventoryService,
        IObjectTable objectTable,
        ILoggerService logger,
        IConfigurationService configService,
        ILocalizationService localization,
        IRetainerGuidanceService guidanceService) {

        this.orchestrator = orchestrator;
        this.uiInteraction = uiInteraction;
        this.competitionState = competitionState;
        this.inventoryService = inventoryService;
        this.objectTable = objectTable;
        this.logger = logger;
        this.configService = configService;
        this.localization = localization;
        this.guidanceService = guidanceService;

        this.tasksByRetainer = new Dictionary<string, Queue<UndercutItem>>();
        this.currentRetainerName = string.Empty;
    }

    public void TriggerPriceUpdate() {
        if (this.IsUpdating) {
            return;
        }

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

    public void AbortUpdate() {
        this.orchestrator.Abort();
    }

    public void OnMenuOpened(string retainerName) {
        this.currentRetainerName = retainerName;
        this.ProcessNextItem();
    }

    public bool OnTick() {
        if (DateTime.Now < this.actionAvailableAt) {
            return false;
        }

        if (this.currentItemTask == null) {
            return this.ProcessNextItem();
        }

        if (DateTime.Now - this.currentItemStartTime > TimeSpan.FromSeconds(15)) {
            this.logger.Error($"Timeout while updating price for {this.currentItemTask.ItemName}. Attempting recovery.");
            this.uiInteraction.CloseUnexpectedWindows();
            return this.ProcessNextItem();
        }

        uint targetPrice = Math.Max(1u, this.currentItemTask.ServerCheapestPrice - 1);
        uint currentPrice = this.inventoryService.GetRetainerMarketItemPrice(this.currentItemTask.SlotIndex);

        // 1. Validated state: Item has reached the target price
        if (currentPrice == targetPrice) {
            this.logger.Info($"Price for '{this.currentItemTask.ItemName}' successfully updated. Moving to next.");
            this.guidanceService.ClearInstruction();
            return this.ProcessNextItem();
        }

        // 2. Reactive state: Price input window is open
        if (this.uiInteraction.IsAddonReady("RetainerSell")) {
            this.uiInteraction.ConfirmPriceUpdate(targetPrice);
            this.SetDelay(0.5);
            return false;
        }

        // 3. Reactive state: Context menu is open
        if (this.uiInteraction.IsAddonReady("ContextMenu")) {
            var adjustPriceText = this.localization.Translate("RetainerMenu_AdjustPrice");
            var menuIndex = this.uiInteraction.GetContextMenuItemIndex(adjustPriceText);

            if (menuIndex != -1) {
                this.uiInteraction.SelectContextMenuItem(menuIndex);
                this.SetDelay(0.2);
            }
            else {
                this.uiInteraction.CloseUnexpectedWindows();
                this.SetDelay(0.5);
            }
            return false;
        }

        // 4. Default state: Click the item in the list
        var uiIndex = this.inventoryService.GetUiIndexForRetainerMarketItem(this.currentItemTask.SlotIndex);
        if (uiIndex != -1) {
            this.uiInteraction.SelectItemInSellList(uiIndex);
            this.SetDelay(0.5);
        }
        else {
            this.logger.Warning($"Cannot find UI index for {this.currentItemTask.ItemName}. Skipping.");
            return this.ProcessNextItem();
        }

        return false;
    }

    private bool ProcessNextItem() {
        if (!this.tasksByRetainer.TryGetValue(this.currentRetainerName, out var queue) || queue.Count == 0) {
            return true;
        }

        this.currentItemTask = queue.Dequeue();
        this.currentItemStartTime = DateTime.Now;
        this.SetDelay(0.5);

        this.guidanceService.SetInstruction(new GuidanceInstruction {
            ActionType = GuidanceActionType.UpdatePrice,
            ItemName = this.currentItemTask.ItemName,
            TargetPrice = Math.Max(1u, this.currentItemTask.ServerCheapestPrice - 1)
        });

        return false;
    }

    private void SetDelay(double waitSeconds) {
        var config = this.configService.GetConfig();
        double randomDelay = 0;

        if (config.EnableAutomationDelay) {
            var min = config.AutomationDelayMin;
            var max = config.AutomationDelayMax;
            if (min > max) {
                min = max;
            }

            randomDelay = min + (new Random().NextDouble() * (max - min));
        }

        this.actionAvailableAt = DateTime.Now.AddSeconds(waitSeconds + randomDelay);
    }

    public void OnMenuClosed(string retainerName) {
        this.guidanceService.ClearInstruction();
    }

    public void OnAbort() {
        this.tasksByRetainer.Clear();
        this.guidanceService.ClearInstruction();
    }
}