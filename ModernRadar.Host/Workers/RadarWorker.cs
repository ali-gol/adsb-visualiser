using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ModernRadar.Core.Interfaces;
using ModernRadar.Core.Models;
using ModernRadar.Host.Hubs;

namespace ModernRadar.Host.Workers;

public class RadarWorker : BackgroundService
{
    private readonly ILogger<RadarWorker> _logger;
    private readonly IRadarTcpClient _tcpClient;
    private readonly IMessageParser _parser;
    private readonly IAircraftTracker _tracker;
    private readonly IExternalAircraftDataProvider _dataProvider;
    private readonly IHubContext<RadarHub> _hubContext;
    private readonly TimeSpan _staleTimeout;

    public RadarWorker(
        ILogger<RadarWorker> logger,
        IRadarTcpClient tcpClient,
        IMessageParser parser,
        IAircraftTracker tracker,
        IExternalAircraftDataProvider dataProvider,
        IConfiguration configuration,
        IHubContext<RadarHub> hubContext)
    {
        _logger = logger;
        _tcpClient = tcpClient;
        _parser = parser;
        _tracker = tracker;
        _dataProvider = dataProvider;
        _hubContext = hubContext;

        int timeoutSeconds = int.TryParse(configuration["Dump1090:StaleTimeoutSeconds"], out var t) ? t : 60;
        _staleTimeout = TimeSpan.FromSeconds(timeoutSeconds);

        _tcpClient.OnMessageReceived += HandleMessage;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RadarWorker started. Connecting to stream...");

        // Start a parallel un-awaited task to clean up old aircraft occasionally.
        _ = CleanupLoopAsync(stoppingToken);

        // This will block and run the pipeline
        await _tcpClient.ConnectAndReadAsync(stoppingToken);
    }

    private void HandleMessage(string rawMessage)
    {
        if (!_parser.TryParse(rawMessage, out BaseStationMessage? message) || message == null)
        {
            return;
        }
        if (!_tracker.ProcessMessage(message, out var updatedAircraft, out bool isNew))
        {
            return;
        }
        if (!isNew)
        {
            return;
        }
        _logger.LogInformation("New Aircraft Spotted: {Hex}", updatedAircraft.Hex);
        // Fire and forget enrichment
        _ = _dataProvider.EnrichAircraftAsync(updatedAircraft).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                _logger.LogError(t.Exception, "Error enriching aircraft {Hex}", updatedAircraft.Hex);
            }
            else
            {
                _logger.LogInformation("Enriched {Hex} with Registration: {Reg}", updatedAircraft.Hex, updatedAircraft.Registration);
            }
        });
    }

    private async Task CleanupLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _tracker.CleanupStaleAircraft(_staleTimeout);
            
            // Print status
            //_logger.LogInformation("Active Aircraft Tracking Count: {Count}", _tracker.ActiveCount);

            // Broadcast to SignalR clients
            var activeAircraft = _tracker.GetActiveAircraft();
            await _hubContext.Clients.All.SendAsync("UpdateAircraftList", activeAircraft, stoppingToken);

            // Broadcast every 1 second
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    public override void Dispose()
    {
        _tcpClient.OnMessageReceived -= HandleMessage;
        base.Dispose();
    }
}
