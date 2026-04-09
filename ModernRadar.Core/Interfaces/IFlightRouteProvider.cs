using ModernRadar.Core.Models;

namespace ModernRadar.Core.Interfaces;

public interface IFlightRouteProvider
{
    Task<FlightRouteDto?> GetRouteAsync(string callsign, CancellationToken cancellationToken = default);
}

public record FlightRouteDto(
    string? OriginIata,
    string? OriginName,
    string? DestinationIata,
    string? DestinationName,
    string? AirlineName,
    string? AirlineIata,
    string? AirlineLogo
);
