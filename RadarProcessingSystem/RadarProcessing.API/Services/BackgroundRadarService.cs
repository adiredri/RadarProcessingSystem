using RadarProcessing.API.Models;
using RadarProcessing.API.Services;

namespace RadarProcessing.API.Services
{
    public class BackgroundRadarService : BackgroundService
    {
        private readonly ILogger<BackgroundRadarService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Random _random = new Random();
        private readonly Dictionary<int, RadarTarget> _movingTargets = new Dictionary<int, RadarTarget>();

        public BackgroundRadarService(ILogger<BackgroundRadarService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Radar Service started");

            // Initialize some targets
            InitializeTargets();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var radarProcessor = scope.ServiceProvider.GetRequiredService<IRadarDataProcessor>();

                    // Update existing targets positions
                    UpdateTargetPositions();

                    // Add updated targets to processor
                    foreach (var target in _movingTargets.Values)
                    {
                        radarProcessor.AddRadarTarget(target);
                    }

                    // Occasionally add new targets
                    if (_random.NextDouble() < 0.1) // 10% chance every cycle
                    {
                        AddNewTarget();
                    }

                    _logger.LogDebug($"Updated {_movingTargets.Count} radar targets");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background radar service");
                }

                // Wait 2 seconds before next update
                await Task.Delay(500, stoppingToken);
            }

            _logger.LogInformation("Background Radar Service stopped");
        }

        private void InitializeTargets()
        {
            // Create 8 initial targets
            for (int i = 1; i <= 8; i++)
            {
                var target = new RadarTarget
                {
                    Id = 2000 + i,
                    X = _random.NextDouble() * 60000 - 30000, // -30km to +30km
                    Y = _random.NextDouble() * 60000 - 30000,
                    Velocity = _random.NextDouble() * 200 + 100, // 100-300 m/s
                    Heading = _random.NextDouble() * 360,
                    Type = (TargetType)(_random.Next(1, 5)),
                    Timestamp = DateTime.UtcNow,
                    SignalStrength = _random.NextDouble() * 30 + 70 // 70-100 signal strength
                };

                _movingTargets[target.Id] = target;
            }

            _logger.LogInformation($"Initialized {_movingTargets.Count} radar targets");
        }

        private void UpdateTargetPositions()
        {
            foreach (var target in _movingTargets.Values)
            {
                // Calculate new position based on velocity and heading
                var deltaTime = 2.0; // 2 seconds between updates
                var velocityX = target.Velocity * Math.Cos(target.Heading * Math.PI / 180.0);
                var velocityY = target.Velocity * Math.Sin(target.Heading * Math.PI / 180.0);

                target.X += velocityX * deltaTime;
                target.Y += velocityY * deltaTime;
                target.Timestamp = DateTime.UtcNow;

                // Add some random movement
                target.Heading += (_random.NextDouble() - 0.5) * 10; // Small heading changes
                target.Velocity += (_random.NextDouble() - 0.5) * 5; // Small velocity changes

                // Keep targets in reasonable bounds
                if (Math.Abs(target.X) > 100000 || Math.Abs(target.Y) > 100000)
                {
                    // Reset position if too far out
                    target.X = _random.NextDouble() * 60000 - 30000;
                    target.Y = _random.NextDouble() * 60000 - 30000;
                }

                // Vary signal strength slightly
                target.SignalStrength += (_random.NextDouble() - 0.5) * 5;
                target.SignalStrength = Math.Max(30, Math.Min(100, target.SignalStrength));
            }
        }

        private void AddNewTarget()
        {
            if (_movingTargets.Count >= 15) return; // Don't exceed 15 targets

            var newId = 3000 + _random.Next(1, 1000);
            if (_movingTargets.ContainsKey(newId)) return;

            var newTarget = new RadarTarget
            {
                Id = newId,
                X = _random.NextDouble() * 40000 - 20000,
                Y = _random.NextDouble() * 40000 - 20000,
                Velocity = _random.NextDouble() * 250 + 75,
                Heading = _random.NextDouble() * 360,
                Type = (TargetType)(_random.Next(1, 5)),
                Timestamp = DateTime.UtcNow,
                SignalStrength = _random.NextDouble() * 35 + 65
            };

            _movingTargets[newTarget.Id] = newTarget;
            _logger.LogInformation($"Added new target {newTarget.Id} - {newTarget.Type}");
        }
    }
}