namespace Marketeer.Features.Retainers.Contracts;

public interface IRetainerService {
    bool IsRetainerAvailable(string retainerName);
    bool SelectRetainer(string retainerName);

    bool IsMenuReadyForRetainer(string retainerName);
    bool IsMenuOptionAvailable(string optionText);
    bool SelectMenuOption(string optionText);

    /// <summary>
    /// Closes the retainer menu safely by sending the universal cancel callback (-1), simulating the ESC key.
    /// </summary>
    bool CloseRetainerMenu();

    bool CloseMarketListings();
}