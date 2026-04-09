using System.Globalization;
using ModernRadar.Core.Interfaces;
using ModernRadar.Core.Models;

namespace ModernRadar.Infrastructure.Parsers;

public class BaseStationParser : IMessageParser
{
    public bool TryParse(string rawData, out BaseStationMessage? message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(rawData)) return false;

        ReadOnlySpan<char> span = rawData.AsSpan();

        try
        {
            // Expected format: MSG,3,111,11111,4CA606,111111,2024/02/09,10:20:30.123,2024/02/09,10:20:30.123,,10000,,,50.1234,-1.2345,,,,,,0
            // We use highly optimized Span reading.
            string messageType = ReadNext(ref span);
            string transmissionType = ReadNext(ref span);
            string sessionId = ReadNext(ref span);
            string aircraftId = ReadNext(ref span);
            string hex = ReadNext(ref span);
            string flightId = ReadNext(ref span);
            string dateGenStr = ReadNext(ref span);
            string timeGenStr = ReadNext(ref span);
            string dateLogStr = ReadNext(ref span);
            string timeLogStr = ReadNext(ref span);
            string callsign = ReadNext(ref span).Trim();
            string altitudeStr = ReadNext(ref span);
            string groundSpeedStr = ReadNext(ref span);
            string trackStr = ReadNext(ref span);
            string latStr = ReadNext(ref span);
            string lonStr = ReadNext(ref span);
            string verticalRateStr = ReadNext(ref span);
            string squawk = ReadNext(ref span);
            string alertStr = ReadNext(ref span);
            string emergencyStr = ReadNext(ref span);
            string spiStr = ReadNext(ref span);
            string isOnGroundStr = ReadNext(ref span);

            if (messageType != "MSG") return false; // Optimization: we mostly care about MSG

            DateTime.TryParse(dateGenStr, CultureInfo.InvariantCulture, out DateTime dateGen);
            TimeSpan.TryParse(timeGenStr, CultureInfo.InvariantCulture, out TimeSpan timeGen);
            DateTime.TryParse(dateLogStr, CultureInfo.InvariantCulture, out DateTime dateLog);
            TimeSpan.TryParse(timeLogStr, CultureInfo.InvariantCulture, out TimeSpan timeLog);

            int? altitude = string.IsNullOrEmpty(altitudeStr) ? null : int.Parse(altitudeStr);
            int? groundSpeed = string.IsNullOrEmpty(groundSpeedStr) ? null : int.Parse(groundSpeedStr);
            int? track = string.IsNullOrEmpty(trackStr) ? null : int.Parse(trackStr);
            double? lat = string.IsNullOrEmpty(latStr) ? null : double.Parse(latStr, CultureInfo.InvariantCulture);
            double? lon = string.IsNullOrEmpty(lonStr) ? null : double.Parse(lonStr, CultureInfo.InvariantCulture);
            int? verticalRate = string.IsNullOrEmpty(verticalRateStr) ? null : int.Parse(verticalRateStr);

            bool? alert = string.IsNullOrEmpty(alertStr) ? null : alertStr == "1" || alertStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            bool? emergency = string.IsNullOrEmpty(emergencyStr) ? null : emergencyStr == "1" || emergencyStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            bool? spi = string.IsNullOrEmpty(spiStr) ? null : spiStr == "1" || spiStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            bool? isOnGround = string.IsNullOrEmpty(isOnGroundStr) ? null : isOnGroundStr == "1" || isOnGroundStr.Equals("true", StringComparison.OrdinalIgnoreCase);

            message = new BaseStationMessage(
                messageType, transmissionType, sessionId, aircraftId, hex, flightId,
                dateGen, timeGen, dateLog, timeLog,
                string.IsNullOrEmpty(callsign) ? null : callsign,
                altitude, groundSpeed, track, lat, lon, verticalRate,
                string.IsNullOrEmpty(squawk) ? null : squawk,
                alert, emergency, spi, isOnGround
            );

            return true;
        }
        catch
        {
            return false;
        }
    }

    private string ReadNext(ref ReadOnlySpan<char> span)
    {
        int commaIndex = span.IndexOf(',');
        if (commaIndex == -1)
        {
            string result = span.ToString();
            span = ReadOnlySpan<char>.Empty;
            return result;
        }
        else
        {
            string result = span.Slice(0, commaIndex).ToString();
            span = span.Slice(commaIndex + 1);
            return result;
        }
    }
}
