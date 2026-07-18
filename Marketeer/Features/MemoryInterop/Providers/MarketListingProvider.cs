using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Marketeer.Features.Inventory.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.MarketListingTracking.Contracts;
using Marketeer.Features.MarketListingTracking.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Marketeer.Features.MemoryInterop.Providers;

public unsafe class MarketListingProvider : IMarketListingProvider {
    private ILoggerService logger;
    private IDataManager dataManager;
    private IInventoryService inventoryService;

    public MarketListingProvider(
        ILoggerService logger,
        IDataManager dataManager,
        IInventoryService inventoryService) {

        this.logger = logger;
        this.dataManager = dataManager;
        this.inventoryService = inventoryService;
    }

    public ulong? GetActiveRetainerId() {
        var manager = RetainerManager.Instance();
        if (manager == null) {
            return null;
        }

        var activeRetainer = manager->GetActiveRetainer();
        if (activeRetainer != null && activeRetainer->RetainerId != 0u) {
            return activeRetainer->RetainerId;
        }

        return null;
    }

    public IReadOnlyList<TrackedListing> GetActiveRetainerListings() {
        var listings = new List<TrackedListing>();
        var stopwatch = Stopwatch.StartNew();

        var activeRetainerIdOpt = this.GetActiveRetainerId();
        if (!activeRetainerIdOpt.HasValue) {
            this.logger.Debug("[MarketListingProvider] No active retainer detected. Aborting scan.");
            return listings;
        }
        ulong activeRetainerId = activeRetainerIdOpt.Value;

        // Fetch physical inventory slots natively. 
        // The InventoryService now directly queries InventoryManager for the explicit prices!
        var inventorySlots = this.inventoryService.GetInventorySlots(InventoryType.RetainerMarket);

        foreach (var slot in inventorySlots.Where(s => s.IsOccupied)) {
            var itemName = this.GetItemName(slot.ItemId);
            uint totalPrice = slot.PricePerUnit * slot.Quantity;
            uint tax = (uint)Math.Floor(totalPrice * 0.05);

            listings.Add(new TrackedListing {
                AssociatedRetainerId = activeRetainerId,
                SlotIndex = slot.SlotIndex,
                ItemId = slot.ItemId,
                ItemName = itemName,
                Quantity = slot.Quantity,
                PricePerUnit = slot.PricePerUnit,
                TotalPrice = totalPrice,
                Tax = tax
            });
        }

        stopwatch.Stop();
        this.logger.Info($"[MarketListingProvider] Extracted {listings.Count} listings natively via FFXIVClientStructs in {stopwatch.ElapsedMilliseconds}ms.");
        return listings;
    }

    private string GetItemName(uint itemId) {
        var itemSheet = this.dataManager.GetExcelSheet<Item>();
        uint baseItemId = itemId > 1000000u ? itemId - 1000000u : itemId;

        if (itemSheet != null && itemSheet.HasRow(baseItemId)) {
            return itemSheet.GetRow(baseItemId).Name.ToString();
        }

        return string.Empty;
    }
}