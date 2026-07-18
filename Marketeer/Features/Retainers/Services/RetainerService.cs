using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.Retainers.Contracts;
using Marketeer.Features.WindowAbstraction.Contracts;
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

        // The pattern ensures the name is at the end of the string ($), 
        // preceded by either the start of the line (^) or the aggregation separator (|\s*).
        // Regex.Escape ensures any special characters in the user's input are safely neutralized.
        var pattern = $@"(?:^|\|\s*){Regex.Escape(retainerName)}$";

        var targetRetainer = retainerRows.FirstOrDefault(e => Regex.IsMatch(e.Text, pattern, RegexOptions.IgnoreCase));

        if (targetRetainer == null) {
            this.logger.Warning($"Retainer '{retainerName}' not found in the active RetainerList. Ensure exact name match.");
            return false;
        }

        var retainerIndex = retainerRows.IndexOf(targetRetainer);

        this.logger.Info($"Selecting retainer '{retainerName}' at logical index {retainerIndex}.");

        window.SendCallback(2, retainerIndex);

        return true;
    }
}