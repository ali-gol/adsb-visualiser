using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ModernRadar.Core.Interfaces;
using System.Text.Json;

namespace ModernRadar.Infrastructure.ExternalServices;

public class AdsbLolRouteProvider : IFlightRouteProvider
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdsbLolRouteProvider> _logger;

    public AdsbLolRouteProvider(HttpClient httpClient, IMemoryCache cache, ILogger<AdsbLolRouteProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ModernRadar/1.0 (Integration/Development)");
    }

    public async Task<FlightRouteDto?> GetRouteAsync(string callsign, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callsign)) return null;

        callsign = callsign.Trim().ToUpperInvariant();
        string cacheKey = $"route_{callsign}";

        if (_cache.TryGetValue(cacheKey, out FlightRouteDto? cachedRoute))
        {
            return cachedRoute;
        }

        try
        {
            var response = await _httpClient.GetAsync($"https://api.adsb.lol/v2/route/{callsign}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var json = JsonDocument.Parse(content);
                var root = json.RootElement;

                // Robust extraction - try multiple common paths
                string? originIata = ExtractString(root, "airport", "origin", "iata") 
                                    ?? ExtractString(root, "route", "origin", "iata");
                string? originName = ExtractString(root, "airport", "origin", "name")
                                    ?? ExtractString(root, "route", "origin", "name");
                
                string? destIata = ExtractString(root, "airport", "destination", "iata")
                                  ?? ExtractString(root, "route", "destination", "iata");
                string? destName = ExtractString(root, "airport", "destination", "name")
                                  ?? ExtractString(root, "route", "destination", "name");
                
                string? airlineName = ExtractString(root, "airline", "name")
                                    ?? ExtractString(root, "operator", "name");
                string? airlineIcao = ExtractString(root, "airline", "icao") 
                                     ?? ExtractString(root, "operator", "icao")
                                     ?? (callsign.Length >= 3 ? callsign.Substring(0, 3) : null);
                
                string? airlineLogo = null;
                if (!string.IsNullOrEmpty(airlineIcao) && airlineIcao.Length == 3)
                {
                    airlineLogo = $"https://pics.avs.io/200/200/{airlineIcao}.png";
                }

                var route = new FlightRouteDto(
                    OriginIata: originIata,
                    OriginName: originName,
                    DestinationIata: destIata,
                    DestinationName: destName,
                    AirlineName: airlineName,
                    AirlineIata: null, // ADSB.lol doesn't provide IATA; handled in StandingData provider
                    AirlineLogo: airlineLogo
                );

                // Cache for 12 hours as requested to be polite to community APIs
                _cache.Set(cacheKey, route, TimeSpan.FromHours(12));
                return route;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ADSB route for {Callsign}", callsign);
        }

        // Cache a null default locally for 5 mins to prevent smashing the API for non-existing callsigns
        var emptyRoute = new FlightRouteDto(null, null, null, null, null, null, null);
        _cache.Set(cacheKey, emptyRoute, TimeSpan.FromMinutes(5));
        
        return emptyRoute;
    }

    private string? ExtractString(JsonElement root, params string[] path)
    {
        try
        {
            var current = root;
            foreach (var step in path)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(step, out var next))
                {
                    current = next;
                }
                else
                {
                    return null;
                }
            }
            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
