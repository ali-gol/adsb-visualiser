using System.Diagnostics.CodeAnalysis;
using ModernRadar.Core.Entities;
using ModernRadar.Core.Models;

namespace ModernRadar.Core.Interfaces;

public interface IAircraftTracker
{
    bool ProcessMessage(BaseStationMessage message, [NotNullWhen(true)] out Aircraft? updatedAircraft, out bool isNew);
    IEnumerable<Aircraft> GetActiveAircraft();
    void CleanupStaleAircraft(TimeSpan timeout);
    int ActiveCount { get; }
}
