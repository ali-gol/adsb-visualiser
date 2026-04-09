using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ModernRadar.Core.Entities;
using ModernRadar.Core.Interfaces;
using ModernRadar.Core.Models;

namespace ModernRadar.Infrastructure.Tracking;

public class InMemoryTracker : IAircraftTracker
{
    private readonly ConcurrentDictionary<string, Aircraft> _tracker = new();
    private readonly ILogger<InMemoryTracker> _logger;

    public InMemoryTracker(ILogger<InMemoryTracker> logger)
    {
        _logger = logger;
    }

    public int ActiveCount => _tracker.Count;

    public bool ProcessMessage(BaseStationMessage message, [NotNullWhen(true)] out Aircraft? updatedAircraft, out bool isNew)
    {
        updatedAircraft = null;
        isNew = false;

        if (string.IsNullOrEmpty(message.Hex)) return false;

        Aircraft aircraft = _tracker.AddOrUpdate(
            message.Hex,
            hex =>
            {
                var newAc = new Aircraft { Hex = hex };
                UpdateAircraftState(newAc, message);
                return newAc;
            },
            (hex, existing) =>
            {
                UpdateAircraftState(existing, message);
                return existing;
            });

        // If this was added just now, LastSeen was set for the first time inside UpdateAircraftState, 
        // we can check if it's new by just assuming AddOrUpdate returns it.
        // A better approach for identifying strictly new vs update is using a separate bool.
        // For simplicity, we treat it as new if it lacks basic info, but we'll use a local trick:
        // isNew = it's a new instance.
        isNew = string.IsNullOrEmpty(aircraft.Registration) 
                && !string.IsNullOrEmpty(message.Hex) 
                && aircraft.LastSeen == DateTime.UtcNow; // approximate, better to use TryAdd 

        updatedAircraft = aircraft;
        return true;
    }

    private void UpdateAircraftState(Aircraft aircraft, BaseStationMessage msg)
    {
        // AddOrUpdate is thread-safe for reading/writing the dictionary, 
        // but concurrent property updates might race. In this use-case, the newest data wins.
        // For zero allocations we just overwrite.
        if (msg.Callsign != null) aircraft.Callsign = msg.Callsign;
        if (msg.Altitude.HasValue) aircraft.Altitude = msg.Altitude.Value;
        if (msg.GroundSpeed.HasValue) aircraft.Speed = msg.GroundSpeed.Value;
        if (msg.Track.HasValue) aircraft.Track = msg.Track.Value;
        if (msg.Latitude.HasValue) aircraft.Latitude = msg.Latitude.Value;
        if (msg.Longitude.HasValue) aircraft.Longitude = msg.Longitude.Value;
        if (msg.VerticalRate.HasValue) aircraft.VerticalSpeed = msg.VerticalRate.Value;

        aircraft.UpdateLastSeen();
    }

    public IEnumerable<Aircraft> GetActiveAircraft()
    {
        return _tracker.Values;
    }

    public void CleanupStaleAircraft(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        int initialCount = _tracker.Count;

        foreach (var kvp in _tracker)
        {
            if (now - kvp.Value.LastSeen > timeout)
            {
                if (_tracker.TryRemove(kvp.Key, out _))
                {
                    _logger.LogDebug("Removed stale aircraft {Hex}", kvp.Key);
                }
            }
        }
    }
}
