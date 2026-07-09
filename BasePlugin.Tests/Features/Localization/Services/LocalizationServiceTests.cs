using BasePlugin.Features.Localization.Contracts;
using BasePlugin.Features.Localization.Services;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

namespace BasePlugin.Tests.Features.Localization.Services;

public class LocalizationServiceTests {
    [Fact]
    public void LocalizationService_Translate_ReturnsStringInCurrentLanguage() {
        // Arrange
        var mockClientState = Substitute.For<IClientState>();
        mockClientState.ClientLanguage.Returns(ClientLanguage.French);

        var mockProvider = Substitute.For<ILocalizationProvider>();
        var dummyTranslations = new Dictionary<ClientLanguage, Dictionary<string, string>> {
            { ClientLanguage.French, new Dictionary<string, string> { { "Test_Key", "Bonjour" } } },
            { ClientLanguage.English, new Dictionary<string, string> { { "Test_Key", "Hello" } } }
        };
        mockProvider.GetTranslations().Returns(dummyTranslations);

        var service = new LocalizationService(mockClientState, new[] { mockProvider });

        // Act
        var result = service.Translate("Test_Key");

        // Assert
        Assert.Equal("Bonjour", result);
    }

    [Fact]
    public void LocalizationService_Translate_FallsBackToEnglishWhenCurrentLanguageIsMissingKey() {
        // Arrange
        var mockClientState = Substitute.For<IClientState>();
        // Set game to German, but our provider won't have the German key
        mockClientState.ClientLanguage.Returns(ClientLanguage.German);

        var mockProvider = Substitute.For<ILocalizationProvider>();
        var dummyTranslations = new Dictionary<ClientLanguage, Dictionary<string, string>> {
            { ClientLanguage.German, new Dictionary<string, string>() }, // Empty German dictionary
            { ClientLanguage.English, new Dictionary<string, string> { { "Test_Key", "English Fallback" } } }
        };
        mockProvider.GetTranslations().Returns(dummyTranslations);

        var service = new LocalizationService(mockClientState, new[] { mockProvider });

        // Act
        var result = service.Translate("Test_Key");

        // Assert
        Assert.Equal("English Fallback", result);
    }

    [Fact]
    public void LocalizationService_Translate_ReturnsRawKeyWhenTotallyMissing() {
        // Arrange
        var mockClientState = Substitute.For<IClientState>();
        mockClientState.ClientLanguage.Returns(ClientLanguage.Japanese);

        var mockProvider = Substitute.For<ILocalizationProvider>();
        mockProvider.GetTranslations().Returns(new Dictionary<ClientLanguage, Dictionary<string, string>>());

        var service = new LocalizationService(mockClientState, new[] { mockProvider });

        // Act
        var result = service.Translate("Non_Existent_Key");

        // Assert
        Assert.Equal("Non_Existent_Key", result);
    }

    [Fact]
    public void LocalizationService_Translate_FormatsStringWithArguments() {
        // Arrange
        var mockClientState = Substitute.For<IClientState>();
        mockClientState.ClientLanguage.Returns(ClientLanguage.English);

        var mockProvider = Substitute.For<ILocalizationProvider>();
        var dummyTranslations = new Dictionary<ClientLanguage, Dictionary<string, string>> {
            { ClientLanguage.English, new Dictionary<string, string> { { "Welcome_Message", "Welcome {0} to {1}!" } } }
        };
        mockProvider.GetTranslations().Returns(dummyTranslations);

        var service = new LocalizationService(mockClientState, new[] { mockProvider });

        // Act
        var result = service.Translate("Welcome_Message", "Almeris", "BasePlugin");

        // Assert
        Assert.Equal("Welcome Almeris to BasePlugin!", result);
    }
}