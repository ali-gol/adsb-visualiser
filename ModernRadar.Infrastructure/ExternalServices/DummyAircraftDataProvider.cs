using ModernRadar.Core.Entities;
using ModernRadar.Core.Interfaces;

namespace ModernRadar.Infrastructure.ExternalServices;

public class DummyAircraftDataProvider : IExternalAircraftDataProvider
{
    public async Task EnrichAircraftAsync(Aircraft aircraft, CancellationToken cancellationToken = default)
    {
        // Simulate a network delay to an external API (e.g. HexDB, Airframes, etc.)
        await Task.Delay(500, cancellationToken);

        // Dummy logic
        aircraft.Registration = $"N-{aircraft.Hex[..Math.Min(4, aircraft.Hex.Length)].ToUpper()}";
        aircraft.Model = "B738 - Default Mock";
        aircraft.ImageUrl = $"https://example.com/aircraft/{aircraft.Hex}.jpg";
    }
}
