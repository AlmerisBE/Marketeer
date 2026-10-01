using Dalamud.Plugin.Services;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.Configuration.Contracts;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.Core.SalesHistory.Contracts;
using Marketeer.UI.Localization.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.RetainerAutomation.Services;

public class ListingCancellationServiceTests {
    [Fact]
    public void TriggerCancellation_WithValidDependencies_ShouldExecuteWithoutThrowing() {
        var uiInteraction = Substitute.For<IRetainerUiInteractionService>();
        var inventoryService = Substitute.For<IInventoryService>();
        var configService = Substitute.For<IConfigurationService>();
        var itemResolver = Substitute.For<IItemResolverService>();
        var localization = Substitute.For<ILocalizationService>();
        var framework = Substitute.For<IFramework>();
        var logger = Substitute.For<ILoggerService>();
        var notificationService = Substitute.For<INotificationService>();
        var delayProvider = Substitute.For<IAutomationDelayProvider>();

        using var service = new ListingCancellationService(
            uiInteraction, inventoryService, configService, itemResolver,
            localization, framework, logger, notificationService, delayProvider);

        // We assert that the new method signature and constructor are fully compatible
        var exception = Record.Exception(() => service.TriggerCancellation(100u));

        Assert.Null(exception);
    }
}