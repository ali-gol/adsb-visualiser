namespace ModernRadar.Core.Models;

public record BaseStationMessage(
    string MessageType,
    string TransmissionType,
    string SessionId,
    string AircraftId,
    string Hex,
    string FlightId,
    DateTime DateMessageGenerated,
    TimeSpan TimeMessageGenerated,
    DateTime DateMessageLogged,
    TimeSpan TimeMessageLogged,
    string? Callsign,
    int? Altitude,
    int? GroundSpeed,
    int? Track,
    double? Latitude,
    double? Longitude,
    int? VerticalRate,
    string? Squawk,
    bool? Alert,
    bool? Emergency,
    bool? Spi,
    bool? IsOnGround
);
