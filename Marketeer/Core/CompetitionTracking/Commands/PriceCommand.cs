using Dalamud.Plugin.Services;
using Marketeer.API.Universalis.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Localization.Contracts;
using System;
using System.Threading.Tasks;

namespace Marketeer.Core.CompetitionTracking.Commands;

public class PriceCommand : ICommand {
    private IServerPriceProvider priceProvider;
    private IObjectTable objectTable;
    private IItemResolverService itemResolver;
    private IChatGui chatGui;
    private ILocalizationService localization;
    private ILoggerService logger;

    public string CommandTrigger => "price";
    public string Description => "Fetches current market price for a given item by name.";

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
        if (string.IsNullOrWhiteSpace(arguments)) {
            return;
        }

        var args = arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var subCommand = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;

        if (subCommand != "lowest" || args.Length < 2) {
            return;
        }

        var rawItemName = args[1].Trim();
        var itemId = this.itemResolver.ResolveItemId(rawItemName);

        if (itemId == 0) {
            this.chatGui.Print(this.localization.Translate("Command_Price_ItemNotFound", rawItemName));
            return;
        }

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer == null || localPlayer.CurrentWorld.RowId == 0) {
            return;
        }

        var worldId = localPlayer.CurrentWorld.RowId;

        Task.Run(async () => {
            try {
                var itemName = this.itemResolver.ResolveItemName(itemId) ?? rawItemName;
                this.chatGui.Print(this.localization.Translate("Command_Price_Fetching", itemName));

                var result = await this.priceProvider.GetLowestPriceAsync(itemId, worldId);
                if (result == null) {
                    this.chatGui.Print(this.localization.Translate("Command_Price_NoData", itemName));
                    return;
                }

                var formattedPrice = result.Price.ToString("N0");
                var message = this.localization.Translate("Command_Price_Result", itemName, formattedPrice, result.RetainerName);
                this.chatGui.Print(message);
            }
            catch (Exception ex) {
                this.logger.Error(ex, $"Failed to fetch price for item ID {itemId}");
            }
        });
    }
}