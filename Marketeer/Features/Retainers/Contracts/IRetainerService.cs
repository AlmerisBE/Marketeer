namespace Marketeer.Features.Retainers.Contracts;

public interface IRetainerService {
    /// <summary>
    /// Attempts to select a retainer from the open RetainerList window by their name.
    /// </summary>
    /// <param name="retainerName">The exact name of the retainer to select.</param>
    /// <returns>True if the retainer was found and clicked, false otherwise.</returns>
    bool SelectRetainer(string retainerName);
}