namespace ModernRadar.Core.Interfaces;

public interface IRadarTcpClient
{
    Task ConnectAndReadAsync(CancellationToken cancellationToken);
    event Action<string>? OnMessageReceived;
}
