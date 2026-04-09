using ModernRadar.Core.Interfaces;
using ModernRadar.Host.Hubs;
using ModernRadar.Host.Workers;
using ModernRadar.Infrastructure.ExternalServices;
using ModernRadar.Infrastructure.Networking;
using ModernRadar.Infrastructure.Parsers;
using ModernRadar.Infrastructure.Tracking;
using ModernRadar.Host.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
});

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, RadarJsonContext.Default);
    });
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IExternalAircraftDataProvider, BaseStationSqbProvider>();
builder.Services.AddSingleton<IFlightRouteProvider, StandingDataSqbRouteProvider>();
builder.Services.AddHttpClient<PlanespottersImageProvider>();

builder.Services.AddSingleton<IRadarTcpClient, Dump1090TcpClient>();
builder.Services.AddSingleton<IMessageParser, BaseStationParser>();
builder.Services.AddSingleton<IAircraftTracker, InMemoryTracker>();

builder.Services.AddHostedService<RadarWorker>();

var port = builder.Configuration.GetValue<int>("port", 5000);
builder.WebHost.UseUrls($"http://*:{port}");
var app = builder.Build();

app.UseCors("AllowAll");

// Serve Angular statically
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<RadarHub>("/radarhub");

// Fallback for Angular routing
app.MapFallbackToFile("index.html");

app.Run();
