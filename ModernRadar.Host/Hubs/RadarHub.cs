using Microsoft.AspNetCore.SignalR;
using ModernRadar.Core.Interfaces;
using ModernRadar.Core.Models;
using ModernRadar.Infrastructure.ExternalServices;

namespace ModernRadar.Host.Hubs;

public class RadarHub : Hub
{
    private readonly IAircraftTracker _tracker;
    private readonly IFlightRouteProvider _routeProvider;
    private readonly PlanespottersImageProvider _imageProvider;
    private readonly IExternalAircraftDataProvider _dataProvider;
    public RadarHub(IAircraftTracker tracker, IFlightRouteProvider routeProvider, PlanespottersImageProvider imageProvider, IExternalAircraftDataProvider dataProvider)
    {
        _tracker = tracker;
        _routeProvider = routeProvider;
        _imageProvider = imageProvider;
        _dataProvider = dataProvider;
    }

    public async Task<AircraftDetailsDto?> GetAircraftDetails(string hex)
    {
        var aircraft = _tracker.GetActiveAircraft().FirstOrDefault(a => a.Hex == hex);
        if (aircraft == null) 
            return null;
        
        await _dataProvider.EnrichAircraftAsync(aircraft, CancellationToken.None);
        FlightRouteDto? route = null;
        if (!string.IsNullOrWhiteSpace(aircraft.Callsign))
        {
            route = await _routeProvider.GetRouteAsync(aircraft.Callsign);
        }

        // Add image enrichment if registration exists
        if (!string.IsNullOrEmpty(aircraft.Registration))
        {
            await _imageProvider.EnrichImagesAsync(aircraft);
        }

        return new AircraftDetailsDto(
            Hex: aircraft.Hex,
            Registration: aircraft.Registration,
            ModelName: aircraft.Model,
            AircraftModelName: aircraft.Model, // Full name was already set in enrichment or fallback
            ManufacturerName: aircraft.ManufacturerName,
            Owner: route?.AirlineName ?? aircraft.Registration?.Split('-')[0], 
            RegistrationCountry: aircraft.RegistrationCountry,
            CountryIsoCode: aircraft.CountryIsoCode,
            ImageUrl: aircraft.ImageUrl,
            ImageUrls: aircraft.ImageUrls,
            Country: aircraft.RegistrationCountry,
            OriginIata: route?.OriginIata,
            DestinationIata: route?.DestinationIata,
            AirlineIata: route?.AirlineIata,
            OriginCity: route?.OriginName,
            DestinationCity: route?.DestinationName,
            AirlineName: route?.AirlineName,
            AirlineLogo: route?.AirlineLogo,
            Track: aircraft.Track,
            VerticalSpeed: aircraft.VerticalSpeed,
            CountryName: aircraft.CountryName,
            CountryCode: aircraft.CountryCode,
            ScheduledDeparture: null, 
            EstimatedDeparture: null
        );
    }
}
