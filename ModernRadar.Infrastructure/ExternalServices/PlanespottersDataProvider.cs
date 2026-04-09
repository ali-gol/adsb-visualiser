using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ModernRadar.Core.Entities;
using ModernRadar.Core.Interfaces;
using System.Text.Json;

namespace ModernRadar.Infrastructure.ExternalServices;

/// <summary>
/// Enriches aircraft metadata using:
///  - HexDB.io  → Registration, ICAO Type (via hex lookup)
///  - Planespotters.net → High-quality aircraft photos (via registration)
/// All results are cached in IMemoryCache to protect community APIs.
/// </summary>
public class PlanespottersDataProvider : IExternalAircraftDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlanespottersDataProvider> _logger;

    public PlanespottersDataProvider(HttpClient httpClient, IMemoryCache cache, ILogger<PlanespottersDataProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ModernRadar/1.0 (contact: admin@modernradar.dev)");
    }

    public async Task EnrichAircraftAsync(Aircraft aircraft, CancellationToken cancellationToken = default)
    {
        // 1. Resolve Country from ICAO hex prefix (local, instant, no API call)
        var (countryName, countryCode) = IcaoRangeService.GetCountry(aircraft.Hex);
        aircraft.CountryName = countryName;
        aircraft.CountryCode = countryCode;

        // 2. Lookup Registration + Model from HexDB.io
        await EnrichFromHexDbAsync(aircraft, cancellationToken);

        // 3. Fetch aircraft photo from Planespotters.net using registration
        if (!string.IsNullOrWhiteSpace(aircraft.Registration))
        {
            await EnrichPhotoFromPlanespottersAsync(aircraft, cancellationToken);
        }
    }

    private async Task EnrichFromHexDbAsync(Aircraft aircraft, CancellationToken cancellationToken)
    {
        string hex = aircraft.Hex.ToUpperInvariant();
        string cacheKey = $"hexdb_{hex}";

        if (_cache.TryGetValue(cacheKey, out (string? reg, string? model) cached))
        {
            aircraft.Registration = cached.reg;
            aircraft.Model = cached.model;
            return;
        }

        try
        {
            // HexDB.io free API — no key required
            var response = await _httpClient.GetAsync(
                $"https://hexdb.io/api/v1/aircraft/{hex}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var json = JsonDocument.Parse(content);
                var root = json.RootElement;

                string? reg = root.TryGetProperty("Registration", out var regProp)
                    ? regProp.GetString()
                    : null;

                // HexDB returns ICAOTypeCode; we map that to a readable model name
                string? icaoType = root.TryGetProperty("ICAOTypeCode", out var typeProp)
                    ? typeProp.GetString()
                    : null;
                string? manufacturer = root.TryGetProperty("Manufacturer", out var mfgProp)
                    ? mfgProp.GetString()
                    : null;
                string? type = root.TryGetProperty("Type", out var fullTypeProp)
                    ? fullTypeProp.GetString()
                    : null;

                // Prefer full Type string, fall back to manufacturer + ICAO type
                string? model = !string.IsNullOrWhiteSpace(type)
                    ? type
                    : (!string.IsNullOrWhiteSpace(manufacturer) && !string.IsNullOrWhiteSpace(icaoType))
                        ? $"{manufacturer} {icaoType}"
                        : icaoType;

                aircraft.Registration = reg;
                aircraft.Model = model;

                // Cache for 24 hours (registration never changes for a given hex)
                _cache.Set(cacheKey, (reg, model), TimeSpan.FromHours(24));

                _logger.LogDebug("HexDB enriched {Hex} → Reg: {Reg}, Model: {Model}", hex, reg, model);
            }
            else
            {
                _logger.LogDebug("HexDB returned {Status} for {Hex}", response.StatusCode, hex);
                // Cache negative result for 1 hour to avoid hammering the API
                _cache.Set(cacheKey, ((string?)null, (string?)null), TimeSpan.FromHours(1));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HexDB lookup failed for {Hex}", hex);
        }
    }

    private async Task EnrichPhotoFromPlanespottersAsync(Aircraft aircraft, CancellationToken cancellationToken)
    {
        string cacheKey = $"ps_photo_{aircraft.Registration}";

        if (_cache.TryGetValue(cacheKey, out string? cachedUrl))
        {
            aircraft.ImageUrl = cachedUrl;
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"https://api.planespotters.net/pub/photos/reg/{aircraft.Registration}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var json = JsonDocument.Parse(content);
                var root = json.RootElement;

                if (root.TryGetProperty("photos", out var photos) && photos.GetArrayLength() > 0)
                {
                    var first = photos[0];
                    string? url = null;

                    // Try thumbnail_large first, fall back to thumbnail
                    if (first.TryGetProperty("thumbnail_large", out var large) && large.TryGetProperty("src", out var largeSrc))
                        url = largeSrc.GetString();
                    else if (first.TryGetProperty("thumbnail", out var thumb) && thumb.TryGetProperty("src", out var thumbSrc))
                        url = thumbSrc.GetString();

                    if (!string.IsNullOrEmpty(url))
                    {
                        aircraft.ImageUrl = url;
                        // Cache photo URLs for 6 hours
                        _cache.Set(cacheKey, url, TimeSpan.FromHours(6));
                        _logger.LogDebug("Planespotters photo found for {Reg}", aircraft.Registration);
                    }
                    else
                    {
                        _cache.Set(cacheKey, (string?)null, TimeSpan.FromHours(1));
                    }
                }
                else
                {
                    _cache.Set(cacheKey, (string?)null, TimeSpan.FromHours(1));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Planespotters photo lookup failed for {Reg}", aircraft.Registration);
        }
    }
}
