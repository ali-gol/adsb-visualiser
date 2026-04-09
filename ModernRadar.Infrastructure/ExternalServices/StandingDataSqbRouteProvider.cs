using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModernRadar.Core.Interfaces;

namespace ModernRadar.Infrastructure.ExternalServices;

/// <summary>
/// Resolves flight route information (origin, destination, airline) from the local
/// VirtualRadar StandingData.sqb SQLite database using the flight callsign.
/// Results are cached for 12 hours.
/// </summary>
public class StandingDataSqbRouteProvider : IFlightRouteProvider
{
    private readonly string _connectionString;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StandingDataSqbRouteProvider> _logger;

    public StandingDataSqbRouteProvider(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<StandingDataSqbRouteProvider> logger)
    {
        _connectionString = configuration.GetConnectionString("StandingDataDb")
            ?? throw new InvalidOperationException(
                "Missing 'ConnectionStrings:StandingDataDb' in configuration.");
        _cache = cache;
        _logger = logger;
    }

    public async Task<FlightRouteDto?> GetRouteAsync(string callsign, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callsign)) return null;

        callsign = callsign.Trim().ToUpperInvariant();
        string cacheKey = $"sdsqb_{callsign}";

        if (_cache.TryGetValue(cacheKey, out FlightRouteDto? cached))
            return cached;

        FlightRouteDto? result = null;
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            // StandingData.sqb schema (VirtualRadar standard):
            //   Route           (RouteId, RouteCode, FromAirportId, ToAirportId, OperatorIcao)
            //   Airport         (AirportId, Iata, Name, City)
            //   Operator/Airline(OperatorId, Icao, Name)
            // RouteCode is stored as uppercase callsign prefix + flight number, e.g. "THY2FW"
            // We try an exact match first, then strip the numeric suffix for airline-level match.

            var row = await conn.QueryFirstOrDefaultAsync<RouteRow>(
                """
                SELECT
                    origin.Iata AS OriginIata, origin.Name AS OriginName,
                    dest.Iata AS DestinationIata, dest.Name AS DestinationName,
                    op.Name AS AirlineName, op.Icao AS AirlineIcao, op.Iata AS AirlineIata
                FROM Route r
                INNER JOIN Airport origin ON origin.AirportId = r.FromAirportId
                INNER JOIN Airport dest ON dest.AirportId = r.ToAirportId
                LEFT JOIN Operator op ON op.OperatorId = r.OperatorId
                WHERE r.Callsign = @Callsign
                LIMIT 1
                """,
                new { Callsign = callsign });

            if (row != null)
            {
                string? logo = BuildLogoUrl(row.AirlineIata, row.AirlineIcao, callsign);
                result = new FlightRouteDto(
                    OriginIata: row.OriginIata,
                    OriginName: row.OriginName,
                    DestinationIata: row.DestinationIata,
                    DestinationName: row.DestinationName,
                    AirlineName: row.AirlineName,
                    AirlineIata: row.AirlineIata,
                    AirlineLogo: logo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StandingData.sqb query failed for callsign {Callsign}", callsign);
        }

        // Cache result (or null) for 12 hours
        _cache.Set(cacheKey, result, TimeSpan.FromHours(12));
        return result;
    }

    private static string? BuildLogoUrl(string? iata, string? icao, string callsign)
    {
        // pics.avs.io uses the 2-letter IATA code — prefer it, fall back to ICAO (3-letter)
        if (!string.IsNullOrWhiteSpace(iata) && iata.Length == 2)
            return $"https://pics.avs.io/200/200/{iata}.png";

        string? code = !string.IsNullOrWhiteSpace(icao) ? icao
                     : callsign.Length >= 3 ? callsign[..3]
                     : null;

        return code is { Length: 3 }
            ? $"https://pics.avs.io/200/200/{code}.png"
            : null;
    }

    private sealed class RouteRow
    {
        public string? OriginIata { get; init; }
        public string? OriginName { get; init; }
        public string? DestinationIata { get; init; }
        public string? DestinationName { get; init; }
        public string? AirlineName { get; init; }
        public string? AirlineIcao { get; init; }
        public string? AirlineIata { get; init; }
    }
}
