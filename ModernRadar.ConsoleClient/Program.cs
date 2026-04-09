using Microsoft.AspNetCore.SignalR.Client;
using ModernRadar.Core.Entities;
using Spectre.Console;

namespace ModernRadar.ConsoleClient;

class Program
{
    private static IEnumerable<Aircraft> _aircraftState = Array.Empty<Aircraft>();
    private static readonly object _lock = new();

    static async Task Main(string[] args)
    {
        Console.Title = "ModernRadar Terminal Dashboard";

        AnsiConsole.MarkupLine("[bold cyan] ModernRadar Live Dashboard Initiating...[/]");

        // SignalR Connection Setup
        var hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/radarhub") // Ensure this matches Host url. We'll assume default 5000.
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<IEnumerable<Aircraft>>("UpdateAircraftList", (aircrafts) =>
        {
            lock (_lock)
            {
                _aircraftState = aircrafts;
            }
        });

        hubConnection.Reconnecting += error =>
        {
            lock (_lock)
            {
                _aircraftState = Array.Empty<Aircraft>(); // Clear out old state on reconnect to avoid ghosts
            }
            return Task.CompletedTask;
        };

        try
        {
            await hubConnection.StartAsync();
            AnsiConsole.MarkupLine("[bold green] Connected to RadarHub successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red] Error connecting to Hub: {ex.Message}[/]");
            return;
        }

        // Run UI Loop
        await AnsiConsole.Live(new Table())
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Cropping(VerticalOverflowCropping.Bottom)
            .StartAsync(async ctx =>
            {
                while (true)
                {
                    IEnumerable<Aircraft> snapshot;
                    lock (_lock)
                    {
                        snapshot = _aircraftState.ToList();
                    }

                    var table = new Table().Border(TableBorder.Rounded);

                    table.AddColumn("[bold yellow]Hex[/]");
                    table.AddColumn("[bold yellow]CallSign[/]");
                    table.AddColumn("[bold yellow]Altitude (ft)[/]");
                    table.AddColumn("[bold yellow]Speed (kts)[/]");
                    table.AddColumn("[bold yellow]Lat/Lon[/]");
                    table.AddColumn("[bold yellow]Registration / Model[/]");

                    foreach (var ac in snapshot.OrderByDescending(a => a.LastSeen))
                    {
                        var latLon = ac.Latitude.HasValue && ac.Longitude.HasValue
                            ? $"{ac.Latitude:F4}, {ac.Longitude:F4}"
                            : "N/A";

                        var enriched = !string.IsNullOrEmpty(ac.Registration)
                            ? $"[green]{ac.Registration}[/] / {ac.Model}"
                            : "[grey]Querying...[/]";

                        table.AddRow(
                            $"[cyan]{ac.Hex}[/]",
                            ac.Callsign ?? "-",
                            ac.Altitude?.ToString() ?? "-",
                            ac.Speed?.ToString() ?? "-",
                            latLon,
                            enriched
                        );
                    }

                    if (!snapshot.Any())
                    {
                        table.AddRow("[grey]Waiting for aircraft...[/]", "", "", "", "", "");
                    }

                    ctx.UpdateTarget(table);
                    await Task.Delay(200); // 5 FPS UI repainting
                }
            });
    }
}
