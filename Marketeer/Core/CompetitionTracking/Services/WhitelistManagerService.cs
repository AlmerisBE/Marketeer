using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.Configuration.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marketeer.Core.CompetitionTracking.Services;

public class WhitelistManagerService : IWhitelistManagerService {
    private IConfigurationService configService;
    private IRetainerStateService retainerState;

    public WhitelistManagerService(IConfigurationService configService, IRetainerStateService retainerState) {
        this.configService = configService;
        this.retainerState = retainerState;
    }

    public void AutoAddKnownRetainers() {
        var config = this.configService.GetConfig();
        var allCharacters = this.retainerState.GetAllCharactersListings();
        bool changed = false;

        foreach (var character in allCharacters) {
            foreach (var listing in character.Listings) {
                if (!string.IsNullOrWhiteSpace(listing.RetainerName)) {
                    if (!config.CompetitorWhitelist.Contains(listing.RetainerName, StringComparer.OrdinalIgnoreCase)) {
                        config.CompetitorWhitelist.Add(listing.RetainerName);
                        changed = true;
                    }
                }
            }
        }

        if (changed) this.configService.Save();
    }

    public void AddToWhitelist(string retainerName) {
        if (string.IsNullOrWhiteSpace(retainerName)) return;

        var config = this.configService.GetConfig();
        if (!config.CompetitorWhitelist.Contains(retainerName.Trim(), StringComparer.OrdinalIgnoreCase)) {
            config.CompetitorWhitelist.Add(retainerName.Trim());
            this.configService.Save();
        }
    }

    public void RemoveFromWhitelist(string retainerName) {
        if (string.IsNullOrWhiteSpace(retainerName)) return;

        var config = this.configService.GetConfig();
        var item = config.CompetitorWhitelist.FirstOrDefault(x => x.Equals(retainerName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (item != null) {
            config.CompetitorWhitelist.Remove(item);
            this.configService.Save();
        }
    }

    public bool IsWhitelisted(string retainerName) {
        if (string.IsNullOrWhiteSpace(retainerName)) return false;

        var config = this.configService.GetConfig();

        // Check explicit whitelist
        if (config.CompetitorWhitelist.Contains(retainerName, StringComparer.OrdinalIgnoreCase)) return true;

        // Check auto-whitelist behavior dynamically against known retainers
        if (config.AutoWhitelistOwnRetainers) {
            foreach (var cData in config.FinancialRecords.Values) {
                foreach (var rData in cData.Retainers.Values) {
                    if (rData.Name.Equals(retainerName, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
        }

        return false;
    }

    public IReadOnlyCollection<string> GetWhitelistedRetainers() {
        return this.configService.GetConfig().CompetitorWhitelist.AsReadOnly();
    }
}