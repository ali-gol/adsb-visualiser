using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModernRadar.Core.Interfaces;

namespace ModernRadar.Infrastructure.Networking;

public class Dump1090TcpClient : IRadarTcpClient, IDisposable
{
    private readonly ILogger<Dump1090TcpClient> _logger;
    private readonly string _ipAddress;
    private readonly int _port;
    private TcpClient? _tcpClient;

    public event Action<string>? OnMessageReceived;

    public Dump1090TcpClient(IConfiguration configuration, ILogger<Dump1090TcpClient> logger)
    {
        _logger = logger;
        _ipAddress = configuration["Dump1090:IpAddress"] ?? "127.0.0.1";
        _port = int.TryParse(configuration["Dump1090:Port"], out var p) ? p : 30003;
    }

    public async Task ConnectAndReadAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to dump1090 at {IpAddress}:{Port}...", _ipAddress, _port);
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_ipAddress, _port, cancellationToken);
                _logger.LogInformation("Connected successfully.");

                var stream = _tcpClient.GetStream();
                var reader = PipeReader.Create(stream);

                await ProcessStreamAsync(reader, cancellationToken);
            }
            catch (Exception ex) when (ex is SocketException or IOException)
            {
                _logger.LogWarning("Connection Lost - Retrying in 5 seconds. Error: {Message}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Connection cancelled by token.");
                break;
            }
            finally
            {
                _tcpClient?.Dispose();
            }
        }
    }

    private async Task ProcessStreamAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        while (true)
        {
            ReadResult result = await reader.ReadAsync(cancellationToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
            {
                ProcessLine(line);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }
    }

    private bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        SequencePosition? position = buffer.PositionOf((byte)'\n');

        if (position == null)
        {
            line = default;
            return false;
        }

        line = buffer.Slice(0, position.Value);
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
        return true;
    }

    private void ProcessLine(in ReadOnlySequence<byte> buffer)
    {
        if (OnMessageReceived == null) return;
        
        string message = Encoding.UTF8.GetString(buffer.IsSingleSegment ? buffer.First.Span : buffer.ToArray());
        OnMessageReceived.Invoke(message.TrimEnd('\r'));
    }

    public void Dispose()
    {
        _tcpClient?.Dispose();
    }
}
