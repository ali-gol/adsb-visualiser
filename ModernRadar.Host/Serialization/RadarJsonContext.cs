using System.Text.Json.Serialization;
using ModernRadar.Core.Entities; // Kendi namespace'lerine göre düzenle
using ModernRadar.Core.Models;   // DTO'larının bulunduğu namespace

namespace ModernRadar.Host.Serialization;

// KURAL: Sınıf mutlaka 'partial' olmalı. Geri kalan kodu .NET derlerken kendisi yazacak.
[JsonSerializable(typeof(Aircraft))]
[JsonSerializable(typeof(IEnumerable<Aircraft>))]
[JsonSerializable(typeof(List<Aircraft>))]
[JsonSerializable(typeof(AircraftDetailsDto))] // SignalR üzerinden gidip gelen tüm tipleri buraya ekle
public partial class RadarJsonContext : JsonSerializerContext
{
}