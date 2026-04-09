namespace ModernRadar.Core.Entities;

public class Aircraft
{
    public string Hex { get; set; } = string.Empty;
    public string? Callsign { get; set; }
    public int? Altitude { get; set; }
    public int? Speed { get; set; }
    public int? Track { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? VerticalSpeed { get; set; }
    public DateTime LastSeen { get; set; }

    // Enriched Data Fields
    public string? Registration { get; set; }
    public string? Model { get; set; }
    public string Owner { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
    public string? CountryIsoCode { get; set; }
    public string? ManufacturerName { get; set; }
    public string? RegistrationCountry { get; set; }

    public void UpdateLastSeen()
    {
        LastSeen = DateTime.UtcNow;
    }
}
