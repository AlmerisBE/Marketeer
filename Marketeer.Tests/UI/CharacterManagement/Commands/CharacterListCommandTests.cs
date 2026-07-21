using Dalamud.Plugin.Services;
using Marketeer.API.CharacterManagement.Contracts;
using Marketeer.API.CharacterManagement.Models;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.UI.CharacterManagement.Commands;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.UI.CharacterManagement.Commands;

public class CharacterListCommandTests {
    [Fact]
    public void Execute_WhenNoCharacters_PrintsEmptyMessage() {
        // Arrange
        var mockTrackerService = Substitute.For<ICharacterTrackerService>();
        var mockWorldDataPresenter = Substitute.For<IWorldDataPresenter>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalizationService = Substitute.For<ILocalizationService>();

        mockTrackerService.GetKnownCharacters().Returns(new List<TrackedCharacter>());
        mockLocalizationService.Translate("Command_Characters_NoCharacters").Returns("Empty");

        var command = new CharacterListCommand(mockTrackerService, mockWorldDataPresenter, mockChatGui, mockLocalizationService);

        // Act
        command.Execute(string.Empty);

        // Assert
        mockChatGui.Received(1).Print("Empty");
        mockLocalizationService.DidNotReceive().Translate("Command_Characters_Header");
    }

    [Fact]
    public void Execute_WhenCharactersExist_PrintsHeaderAndCharacterLines() {
        // Arrange
        var mockTrackerService = Substitute.For<ICharacterTrackerService>();
        var mockWorldDataPresenter = Substitute.For<IWorldDataPresenter>();
        var mockChatGui = Substitute.For<IChatGui>();
        var mockLocalizationService = Substitute.For<ILocalizationService>();

        var characters = new List<TrackedCharacter> {
            new TrackedCharacter { Name = "Test One", HomeWorldId = 33 }
        };
        mockTrackerService.GetKnownCharacters().Returns(characters);

        mockWorldDataPresenter.GetWorldName(33).Returns("Twintania");

        mockLocalizationService.Translate("Command_Characters_Header").Returns("Header:");
        mockLocalizationService.Translate("Command_Characters_Line", "Test One", "Twintania").Returns("- Test One (Twintania)");

        var command = new CharacterListCommand(mockTrackerService, mockWorldDataPresenter, mockChatGui, mockLocalizationService);

        // Act
        command.Execute(string.Empty);

        // Assert
        mockChatGui.Received(1).Print("Header:");
        mockChatGui.Received(1).Print("- Test One (Twintania)");
    }
}