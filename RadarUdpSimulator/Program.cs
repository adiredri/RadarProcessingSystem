using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RadarUdpSimulator
{
    public class RadarDataPacket
    {
        public int TargetId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Velocity { get; set; }
        public double Heading { get; set; }
        public int TargetType { get; set; }
        public DateTime Timestamp { get; set; }
        public double SignalStrength { get; set; }
        public string RadarStationId { get; set; } = "RADAR-001";
    }

    class Program
    {
        private static readonly UdpClient udpClient = new UdpClient();
        private static readonly IPEndPoint radarApiEndpoint = new IPEndPoint(IPAddress.Loopback, 5201);
        private static readonly Random random = new Random();
        private static readonly Dictionary<int, RadarDataPacket> simulatedTargets = new Dictionary<int, RadarDataPacket>();

        static async Task Main(string[] args)
        {
            Console.WriteLine("🎯 UDP Radar Data Simulator Starting...");
            Console.WriteLine($"📡 Sending data to: {radarApiEndpoint}");
            Console.WriteLine("Press Ctrl+C to stop\n");

            InitializeTargets();

            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            try
            {
                await SimulateRadarDataAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n🛑 Simulation stopped");
            }
            finally
            {
                udpClient.Close();
            }
        }

        static void InitializeTargets()
        {
            for (int i = 1; i <= 5; i++)
            {
                var target = new RadarDataPacket
                {
                    TargetId = 4000 + i,
                    X = random.NextDouble() * 80000 - 40000,
                    Y = random.NextDouble() * 80000 - 40000,
                    Velocity = random.NextDouble() * 400 + 100,
                    Heading = random.NextDouble() * 360,
                    TargetType = random.Next(1, 5),
                    Timestamp = DateTime.UtcNow,
                    SignalStrength = random.NextDouble() * 40 + 60
                };
                simulatedTargets[target.TargetId] = target;
            }
            Console.WriteLine($"✅ Initialized {simulatedTargets.Count} targets");
        }

        static async Task SimulateRadarDataAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UpdateTargetPositions();

                    foreach (var target in simulatedTargets.Values)
                    {
                        await SendRadarDataPacket(target);
                        await Task.Delay(10, cancellationToken);
                    }

                    Console.WriteLine($"📡 Sent {simulatedTargets.Count} targets | Time: {DateTime.Now:HH:mm:ss}");
                    await Task.Delay(500, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        static async Task SendRadarDataPacket(RadarDataPacket target)
        {
            try
            {
                target.Timestamp = DateTime.UtcNow;
                var jsonData = JsonSerializer.Serialize(target, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var packetData = Encoding.UTF8.GetBytes(jsonData);
                await udpClient.SendAsync(packetData, packetData.Length, radarApiEndpoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send packet for target {target.TargetId}: {ex.Message}");
            }
        }

        static void UpdateTargetPositions()
        {
            foreach (var target in simulatedTargets.Values)
            {
                var deltaTime = 0.5;
                var velocityX = target.Velocity * Math.Cos(target.Heading * Math.PI / 180.0);
                var velocityY = target.Velocity * Math.Sin(target.Heading * Math.PI / 180.0);

                target.X += velocityX * deltaTime;
                target.Y += velocityY * deltaTime;

                target.Heading += (random.NextDouble() - 0.5) * 5;
                target.Velocity += (random.NextDouble() - 0.5) * 10;
                target.Velocity = Math.Max(50, Math.Min(600, target.Velocity));

                if (Math.Abs(target.X) > 100000 || Math.Abs(target.Y) > 100000)
                {
                    target.X = random.NextDouble() * 60000 - 30000;
                    target.Y = random.NextDouble() * 60000 - 30000;
                }
            }
        }
    }
}