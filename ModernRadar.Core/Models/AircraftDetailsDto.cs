namespace ModernRadar.Core.Models;

public record AircraftDetailsDto(
    string Hex,
    string? Registration,
    string? ModelName, // This is the simple ICAOTypeCode or short model
    string? AircraftModelName, // Full model name from StandingData
    string? ManufacturerName,
    string? Owner,
    string? RegistrationCountry,
    string? CountryIsoCode,
    string? ImageUrl, // Primary image
    List<string> ImageUrls, // All images for carousel
    string? Country, // Keep for backward compatibility or UI simplified
    string? OriginIata,
    string? DestinationIata,
    string? AirlineIata,
    string? OriginCity,
    string? DestinationCity,
    string? AirlineName,
    string? AirlineLogo,
    int? Track,
    int? VerticalSpeed,
    string? CountryName,
    string? CountryCode,
    DateTime? ScheduledDeparture,
    DateTime? EstimatedDeparture
);
