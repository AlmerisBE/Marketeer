using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.Retainers.Contracts;
using Marketeer.Features.WindowAbstraction.Contracts;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Marketeer.Features.Retainers.Services;

public class RetainerService : IRetainerService {
    private INativeWindowService windowService;
    private ILoggerService logger;

    public RetainerService(INativeWindowService windowService, ILoggerService logger) {
        this.windowService = windowService;
        this.logger = logger;
    }

    public bool IsRetainerAvailable(string retainerName) {
        var window = this.windowService.GetWindow("RetainerList");
        if (window == null || !window.IsVisible) {
            return false;
        }

        var pattern = $@"(?:^|\|\s*){Regex.Escape(retainerName)}$";

        return window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Button && e.Text.Contains(" | "))
            .Any(e => Regex.IsMatch(e.Text, pattern, RegexOptions.IgnoreCase));
    }

    public bool SelectRetainer(string retainerName) {
        var window = this.windowService.GetWindow("RetainerList");

        if (window == null || !window.IsVisible) {
            this.logger.Warning("Cannot select retainer: 'RetainerList' window is not visible.");
            return false;
        }

        var elements = window.GetElements().ToList();
        var retainerRows = elements
            .Where(e => e.Type == NativeUiElementType.Button && e.Text.Contains(" | "))
            .ToList();

        var pattern = $@"(?:^|\|\s*){Regex.Escape(retainerName)}$";
        var targetRetainer = retainerRows.FirstOrDefault(e => Regex.IsMatch(e.Text, pattern, RegexOptions.IgnoreCase));

        if (targetRetainer == null) {
            this.logger.Warning($"Retainer '{retainerName}' not found in the active RetainerList.");
            return false;
        }

        var retainerIndex = retainerRows.IndexOf(targetRetainer);
        this.logger.Info($"Selecting retainer '{retainerName}' at logical index {retainerIndex}.");

        window.SendCallbackWithUpdateState(true, 2, retainerIndex);
        return true;
    }

    public bool IsMenuReadyForRetainer(string retainerName) {
        var window = this.windowService.GetWindow("SelectString");

        if (window == null || !window.IsVisible) {
            return false;
        }

        return window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Text)
            .Any(e => e.Text.Contains(retainerName, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsMenuOptionAvailable(string optionText) {
        var window = this.windowService.GetWindow("SelectString");
        if (window == null || !window.IsVisible) {
            return false;
        }

        return window.GetElements()
            .Where(e => e.Type == NativeUiElementType.Button)
            .Any(e => e.Text.StartsWith(optionText, StringComparison.OrdinalIgnoreCase));
    }

    public bool SelectMenuOption(string optionText) {
        var window = this.windowService.GetWindow("SelectString");

        if (window == null || !window.IsVisible) {
            this.logger.Warning("Cannot select menu option: 'SelectString' window is not visible.");
            return false;
        }

        var elements = window.GetElements().ToList();
        var menuRows = elements
            .Where(e => e.Type == NativeUiElementType.Button)
            .ToList();

        var targetOption = menuRows.FirstOrDefault(e => e.Text.StartsWith(optionText, StringComparison.OrdinalIgnoreCase));

        if (targetOption == null) {
            this.logger.Warning($"Menu option starting with '{optionText}' not found.");
            return false;
        }

        var optionIndex = menuRows.IndexOf(targetOption);
        this.logger.Info($"Selecting menu option '{targetOption.Text}' at logical index {optionIndex}.");

        window.SendCallbackWithUpdateState(true, optionIndex);
        return true;
    }

    public bool CloseRetainerMenu() {
        var window = this.windowService.GetWindow("SelectString");

        if (window == null || !window.IsVisible) {
            return false;
        }

        this.logger.Info("Closing retainer menu via universal cancel callback.");
        window.SendCallbackWithUpdateState(true, -1);
        return true;
    }

    public bool CloseMarketListings() {
        var window = this.windowService.GetWindow("RetainerSellList");

        if (window == null || !window.IsVisible) {
            return false;
        }

        this.logger.Info("Closing market listings window via universal cancel callback.");
        window.SendCallbackWithUpdateState(true, -1);
        return true;
    }

    public bool CloseSalesHistory() {
        var window = this.windowService.GetWindow("RetainerHistory");

        if (window == null || !window.IsVisible) {
            return false;
        }

        this.logger.Info("Closing sales history window via universal cancel callback.");
        window.SendCallbackWithUpdateState(true, -1);
        return true;
    }
}