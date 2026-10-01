using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.UI.Shell.Contracts;
using System;

namespace Marketeer.Core.MarketPricing.Services;

public class UniversalisUpdateStateService : IUniversalisUpdateState, IUniversalisUpdateMutator {
    public bool IsUpdating { get; private set; }
    public DateTime? LastUpdateTime { get; private set; }

    public void SetUpdating(bool isUpdating) {
        this.IsUpdating = isUpdating;
    }

    public void RecordSuccessfulUpdate() {
        this.IsUpdating = false;
        this.LastUpdateTime = DateTime.UtcNow;
    }
}