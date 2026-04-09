using ModernRadar.Core.Models;

namespace ModernRadar.Core.Interfaces;

public interface IMessageParser
{
    bool TryParse(string rawData, out BaseStationMessage? message);
}
