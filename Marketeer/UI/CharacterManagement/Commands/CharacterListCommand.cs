using Dalamud.Plugin.Services;
using Marketeer.API.CharacterManagement.Contracts;
using Marketeer.API.Command.Contracts;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.Localization.Contracts;

namespace Marketeer.UI.CharacterManagement.Commands;

public class CharacterListCommand : ICommand {
    private ICharacterTrackerService trackerService;
    private IWorldDataPresenter worldDataPresenter;
    private IChatGui chatGui;
    private ILocalizationService localizationService;

    public string CommandTrigger => "characters";
    public string Description => this.localizationService.Translate("Command_Characters_Description");

    public CharacterListCommand(
        ICharacterTrackerService trackerService,
        IWorldDataPresenter worldDataPresenter,
        IChatGui chatGui,
        ILocalizationService localizationService) {

        this.trackerService = trackerService;
        this.worldDataPresenter = worldDataPresenter;
        this.chatGui = chatGui;
        this.localizationService = localizationService;
    }

    public void Execute(string arguments) {
        var characters = this.trackerService.GetKnownCharacters();

        if (characters.Count == 0) {
            this.chatGui.Print(this.localizationService.Translate("Command_Characters_NoCharacters"));
            return;
        }

        this.chatGui.Print(this.localizationService.Translate("Command_Characters_Header"));

        foreach (var character in characters) {
            var worldName = this.worldDataPresenter.GetWorldName(character.HomeWorldId);
            var line = this.localizationService.Translate("Command_Characters_Line", character.Name, worldName);
            this.chatGui.Print(line);
        }
    }
}