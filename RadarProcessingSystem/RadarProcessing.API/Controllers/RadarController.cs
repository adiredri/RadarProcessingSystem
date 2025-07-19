using Microsoft.AspNetCore.Mvc;
using RadarProcessing.API.Models;

namespace RadarProcessing.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RadarController : ControllerBase
    {
        private readonly ILogger<RadarController> _logger;

        public RadarController(ILogger<RadarController> logger)
        {
            _logger = logger;
        }

        [HttpGet("targets")]
        public IEnumerable<RadarTarget> GetActiveTargets()
        {
            // Sample radar targets for now - we'll replace with real data processor later
            var targets = new List<RadarTarget>
            {
                new RadarTarget
                {
                    Id = 1001,
                    X = 15000.0,
                    Y = 25000.0,
                    Velocity = 250.5,
                    Heading = 45.0,
                    Type = TargetType.Aircraft,
                    Timestamp = DateTime.UtcNow,
                    SignalStrength = 85.2
                },
                new RadarTarget
                {
                    Id = 1002,
                    X = 8500.0,
                    Y = 12000.0,
                    Velocity = 15.8,
                    Heading = 180.0,
                    Type = TargetType.Ship,
                    Timestamp = DateTime.UtcNow.AddSeconds(-2),
                    SignalStrength = 92.1
                },
                new RadarTarget
                {
                    Id = 1003,
                    X = 3200.0,
                    Y = 7800.0,
                    Velocity = 65.0,
                    Heading = 270.0,
                    Type = TargetType.Vehicle,
                    Timestamp = DateTime.UtcNow.AddSeconds(-1),
                    SignalStrength = 78.9
                }
            };

            _logger.LogInformation($"Returning {targets.Count} active radar targets");
            return targets;
        }

        [HttpGet("targets/{id}")]
        public ActionResult<RadarTarget> GetTarget(int id)
        {
            // Sample implementation - find specific target
            var target = new RadarTarget
            {
                Id = id,
                X = 15000.0 + (id * 100),
                Y = 25000.0 + (id * 150),
                Velocity = 200.0 + (id * 5),
                Heading = (id * 15) % 360,
                Type = (TargetType)(id % 5),
                Timestamp = DateTime.UtcNow,
                SignalStrength = 80.0 + (id % 20)
            };

            _logger.LogInformation($"Returning radar target with ID: {id}");
            return Ok(target);
        }

        [HttpGet("statistics")]
        public ActionResult<ProcessedRadarData> GetStatistics()
        {
            var stats = new ProcessedRadarData
            {
                ActiveTargets = 15,
                ProcessingLatencyMs = 12.5,
                DataThroughputMbps = 2.8,
                LastProcessedTime = DateTime.UtcNow,
                TargetsByType = new Dictionary<TargetType, int>
                {
                    { TargetType.Aircraft, 8 },
                    { TargetType.Ship, 4 },
                    { TargetType.Vehicle, 2 },
                    { TargetType.Missile, 1 }
                },
                ProcessingTimeMs = 8.2
            };

            _logger.LogInformation("Returning radar processing statistics");
            return Ok(stats);
        }

        [HttpGet("health")]
        public ActionResult<SystemHealth> GetSystemHealth()
        {
            var health = new SystemHealth
            {
                IsHealthy = true,
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = 15.7,
                MemoryUsageMb = 128.5,
                ProcessingQueueSize = 3,
                UptimeSeconds = 3600 * 4, // 4 hours uptime
                ErrorCount = 0,
                WarningCount = 2
            };

            _logger.LogInformation("System health check completed - All systems operational");
            return Ok(health);
        }

        [HttpGet("targets/active")]
        public ActionResult<IEnumerable<RadarTarget>> GetActiveTargetsOnly()
        {
            var activeTargets = GetActiveTargets()
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.SignalStrength);

            _logger.LogInformation($"Returning {activeTargets.Count()} active radar targets only");
            return Ok(activeTargets);
        }

        [HttpGet("targets/type/{targetType}")]
        public ActionResult<IEnumerable<RadarTarget>> GetTargetsByType(TargetType targetType)
        {
            var filteredTargets = GetActiveTargets()
                .Where(t => t.Type == targetType);

            _logger.LogInformation($"Returning {filteredTargets.Count()} targets of type: {targetType}");
            return Ok(filteredTargets);
        }
    }
}