using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ModernRadar.Core.Entities;
using ModernRadar.Core.Interfaces;
using System.Text.Json;

namespace ModernRadar.Infrastructure.ExternalServices;

/// <summary>
/// Fetches aircraft photos from Planespotters.net and returns multiple URLs for carousel.
/// </summary>
public class PlanespottersImageProvider
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlanespottersImageProvider> _logger;

    public PlanespottersImageProvider(HttpClient httpClient, IMemoryCache cache, ILogger<PlanespottersImageProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ModernRadar/1.0 (Integration/Development)");
    }

    public async Task EnrichImagesAsync(Aircraft aircraft, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(aircraft.Registration)) return;

        string cacheKey = $"ps_images_{aircraft.Registration}";
        
        if (_cache.TryGetValue(cacheKey, out List<string>? cachedUrls))
        {
            if (cachedUrls != null && cachedUrls.Count > 0)
            {
                aircraft.ImageUrls = cachedUrls;
                aircraft.ImageUrl = cachedUrls[0];
            }
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync($"https://api.planespotters.net/pub/photos/reg/{aircraft.Registration}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var json = JsonDocument.Parse(content);
                var root = json.RootElement;

                if (root.TryGetProperty("photos", out var photos) && photos.GetArrayLength() > 0)
                {
                    var urls = new List<string>();
                    for (int i = 0; i < photos.GetArrayLength(); i++)
                    {
                        var photo = photos[i];
                        if (photo.TryGetProperty("thumbnail_large", out var thumb) && thumb.TryGetProperty("src", out var src))
                        {
                            urls.Add(src.GetString() ?? string.Empty);
                        }
                    }

                    if (urls.Count > 0)
                    {
                        aircraft.ImageUrls = urls;
                        aircraft.ImageUrl = urls[0];
                        _cache.Set(cacheKey, urls, TimeSpan.FromHours(6));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Planespotters image fetch failed for {Reg}", aircraft.Registration);
        }
    }
}
