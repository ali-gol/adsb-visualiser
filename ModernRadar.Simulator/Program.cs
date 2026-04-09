using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ModernRadar.Simulator;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Dump1090 Mock Simulator on 127.0.0.1:30003...");

        var aircraft = new List<FakeAircraft>
        {
            // 1. İzmir (ADB) kalkışlı, İstanbul (ISL) varışlı Pegasus (Tescil: TC-AAI, Model: B738)
            new("4B8429", "PGT2816", 38.5100, 27.1500, 15000, 320), 
            
            // 2. İstanbul (SAW) - Marsilya (MRS) rotasında, Ege açıklarında seyreden Pegasus (Tescil: TC-AAJ)
            new("4B842A", "PGT1125", 38.3500, 26.1200, 34000, 460), 
            
            // 3. İstanbul (SAW) - Prag (PRG) rotasında tırmanışta olan Pegasus (Tescil: TC-AAL)
            new("4B842C", "PGT303", 40.1500, 28.5000, 24000, 380),
            
            // 4. İstanbul (SAW) - Tiran (TIA) rotasında, Çanakkale civarında seyreden Pegasus (Tescil: TC-AAN)
            new("4B842E", "PGT283", 39.8000, 26.3000, 36000, 440)
        };

        var listener = new TcpListener(IPAddress.Loopback, 30003);
        listener.Start();

        Console.WriteLine("Listening for incoming connections...");

        while (true)
        {
            TcpClient? client = null;
            try
            {
                client = await listener.AcceptTcpClientAsync();
                Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
                await HandleClientAsync(client, aircraft);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client disconnected or error: {ex.Message}");
            }
            finally
            {
                client?.Dispose();
            }
            // Loop back and wait for another single connection
        }
    }

    static async Task HandleClientAsync(TcpClient client, List<FakeAircraft> aircraftList)
    {
        using var stream = client.GetStream();
        // StreamWriter'a NewLine karakterini açıkça \r\n olarak set ediyoruz
        using var writer = new StreamWriter(stream, new UTF8Encoding(false))
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };

        while (client.Connected)
        {
            var now = DateTime.UtcNow;
            string dateStr = now.ToString("yyyy/MM/dd");
            string timeStr = now.ToString("HH:mm:ss.fff");

            foreach (var ac in aircraftList)
            {
                ac.Latitude += 0.0005; // Biraz daha yavaş hareket etsinler
                ac.Longitude += 0.0005;

                // CultureInfo.InvariantCulture kullanarak ondalık ayracının her zaman nokta olmasını sağlıyoruz
                string lat = ac.Latitude.ToString("F4", CultureInfo.InvariantCulture);
                string lon = ac.Longitude.ToString("F4", CultureInfo.InvariantCulture);

                // Msg Type 1: Identification and Category (Callsign)
                // SBS-1: MSG,1,sess,acId,HEX,flt,dategen,timegen,datelog,timelog,callsign,[empty x9]
                await writer.WriteLineAsync($"MSG,1,1,1,{ac.Hex},1,{dateStr},{timeStr},{dateStr},{timeStr},{ac.Callsign},,,,,,,,,,");

                // Msg Type 3: Airborne Position
                // SBS-1: MSG,3,sess,acId,HEX,flt,dategen,timegen,datelog,timelog,[callsign],altitude,[sqwk],[alert],[emrg],[lat],[lon],[vs],[spi],[gnd]
                await writer.WriteLineAsync($"MSG,3,1,1,{ac.Hex},1,{dateStr},{timeStr},{dateStr},{timeStr},,{ac.Altitude},,,{lat},{lon},,0,0");

                // Msg Type 4: Airborne Velocity
                // SBS-1: MSG,4,...,[callsign],[alt],[speed],[track],,,,[vs],,,,
                await writer.WriteLineAsync($"MSG,4,1,1,{ac.Hex},1,{dateStr},{timeStr},{dateStr},{timeStr},,,{ac.Speed},{ac.Track},,,,,,,");
            }

            await Task.Delay(1000);
        }
    }
}

class FakeAircraft
{
    public string Hex { get; }
    public string Callsign { get; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Altitude { get; set; }
    public int Speed { get; set; }
    public int Track { get; set; }

    public FakeAircraft(string hex, string callsign, double lat, double lon, int alt, int speed, int track = 45)
    {
        Hex = hex;
        Callsign = callsign;
        Latitude = lat;
        Longitude = lon;
        Altitude = alt;
        Speed = speed;
        Track = track;
    }
}
