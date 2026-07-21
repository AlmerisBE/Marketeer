using Dalamud.Plugin.Services;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.SalesHistoryTracking.Contracts;
using Marketeer.Features.UndercutTracking.Contracts;
using System;
using System.Threading.Tasks;

namespace Marketeer.Features.UndercutTracking.Commands;

public class PriceCommand : ICommand {
    private IServerPriceProvider priceProvider;
    private IObjectTable objectTable;
    private IItemResolverService itemResolver;
    private IChatGui chatGui;
    private ILocalizationService localization;
    private ILoggerService logger;

    public string CommandTrigger => "price";
    public string Description => this.localization.Translate("Command_Price_Description");

    public PriceCommand(
        IServerPriceProvider priceProvider,
        IObjectTable objectTable,
        IItemResolverService itemResolver,
        IChatGui chatGui,
        ILocalizationService localization,
        ILoggerService logger) {

        this.priceProvider = priceProvider;
        this.objectTable = objectTable;
        this.itemResolver = itemResolver;
        this.chatGui = chatGui;
        this.localization = localization;
        this.logger = logger;
    }

    public void Execute(string arguments) {
        if (string.IsNullOrWhiteSpace(arguments) || !arguments.StartsWith("lowest ", StringComparison.OrdinalIgnoreCase)) {
            this.chatGui.PrintError(this.localization.Translate("Command_Price_InvalidArgs"));
            return;
        }

        var itemName = arguments.Substring("lowest ".Length).Trim();
        if (string.IsNullOrWhiteSpace(itemName)) {
            this.chatGui.PrintError(this.localization.Translate("Command_Price_InvalidArgs"));
            return;
        }

        var itemId = this.itemResolver.ResolveItemId(itemName);
        if (itemId == 0) {
            this.chatGui.PrintError(this.localization.Translate("Command_Price_ItemNotFound", itemName));
            return;
        }

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null) {
            this.logger.Warning("Local player is not available to determine current world.");
            return;
        }

        // Retrieves the world ID using Lumina's native Excel struct definition
        var worldId = localPlayer.CurrentWorld.RowId;
        var resolvedItemName = this.itemResolver.ResolveItemName(itemId);

        this.chatGui.Print(this.localization.Translate("Command_Price_Fetching", resolvedItemName));

        // Offload the network request to a background thread to prevent blocking the game's main thread
        Task.Run(async () => {
            try {
                var result = await this.priceProvider.GetLowestPriceAsync(itemId, worldId);

                if (result != null) {
                    var formattedPrice = result.Price.ToString("N0");
                    this.chatGui.Print(this.localization.Translate("Command_Price_Result", resolvedItemName, formattedPrice, result.RetainerName));
                }
                else {
                    this.chatGui.PrintError(this.localization.Translate("Command_Price_Error", resolvedItemName));
                }
            }
            catch (Exception ex) {
                this.logger.Error(ex, $"Exception thrown while fetching price for {resolvedItemName} (ID: {itemId})");
                this.chatGui.PrintError(this.localization.Translate("Command_Price_Error", resolvedItemName));
            }
        });
    }
}