using ModernRadar.Core.Entities;

namespace ModernRadar.Core.Interfaces;

public interface IExternalAircraftDataProvider
{
    Task EnrichAircraftAsync(Aircraft aircraft, CancellationToken cancellationToken = default);
}
