using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using RadarProcessing.API.Models;

namespace RadarProcessing.API.Services
{
    public class UdpRadarReceiver : BackgroundService
    {
        private readonly ILogger<UdpRadarReceiver> _logger;
        private readonly IServiceProvider _serviceProvider;
        private UdpClient? _udpClient;
        private readonly int _udpPort = 5201;

        public UdpRadarReceiver(ILogger<UdpRadarReceiver> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🌐 UDP Radar Receiver starting on port {Port}", _udpPort);

            try
            {
                _udpClient = new UdpClient(_udpPort);
                _logger.LogInformation("✅ UDP listener active on port {Port}", _udpPort);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Receive UDP packet
                        var result = await _udpClient.ReceiveAsync();
                        await ProcessReceivedData(result.Buffer, result.RemoteEndPoint);
                    }
                    catch (ObjectDisposedException)
                    {
                        // UDP client was disposed, exit gracefully
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error receiving UDP data");
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to start UDP receiver");
            }
            finally
            {
                _udpClient?.Close();
                _logger.LogInformation("🛑 UDP Radar Receiver stopped");
            }
        }

        private async Task ProcessReceivedData(byte[] data, IPEndPoint remoteEndPoint)
        {
            try
            {
                // Convert bytes to string
                var jsonData = Encoding.UTF8.GetString(data);

                // Deserialize radar data packet
                var radarPacket = JsonSerializer.Deserialize<RadarDataPacket>(jsonData,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                if (radarPacket == null)
                {
                    _logger.LogWarning("⚠️ Received invalid radar packet from {RemoteEndPoint}", remoteEndPoint);
                    return;
                }

                // Convert to internal RadarTarget format
                var radarTarget = new RadarTarget
                {
                    Id = radarPacket.TargetId,
                    X = radarPacket.X,
                    Y = radarPacket.Y,
                    Velocity = radarPacket.Velocity,
                    Heading = radarPacket.Heading,
                    Type = (TargetType)radarPacket.TargetType,
                    Timestamp = radarPacket.Timestamp,
                    SignalStrength = radarPacket.SignalStrength
                };

                // Add to radar processor
                using var scope = _serviceProvider.CreateScope();
                var radarProcessor = scope.ServiceProvider.GetRequiredService<IRadarDataProcessor>();
                radarProcessor.AddRadarTarget(radarTarget);

                _logger.LogDebug("📡 Processed UDP target {Id} from {Station} via {RemoteEndPoint}",
                    radarPacket.TargetId, radarPacket.RadarStationId, remoteEndPoint);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Failed to parse radar data from {RemoteEndPoint}", remoteEndPoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing radar data from {RemoteEndPoint}", remoteEndPoint);
            }
        }

        public override void Dispose()
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            base.Dispose();
        }
    }

    // Data packet structure matching the UDP simulator
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
        public string RadarStationId { get; set; } = "";
    }
}