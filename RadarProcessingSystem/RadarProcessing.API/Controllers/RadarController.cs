using Microsoft.AspNetCore.Mvc;
using RadarProcessing.API.Models;
using RadarProcessing.API.Services;

namespace RadarProcessing.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RadarController : ControllerBase
    {
        private readonly ILogger<RadarController> _logger;
        private readonly IRadarDataProcessor _radarProcessor;

        public RadarController(ILogger<RadarController> logger, IRadarDataProcessor radarProcessor)
        {
            _logger = logger;
            _radarProcessor = radarProcessor;
        }

        [HttpGet("targets")]
        public IEnumerable<RadarTarget> GetActiveTargets()
        {
            var targets = _radarProcessor.GetActiveTargets();
            _logger.LogInformation($"Returning {targets.Count()} active radar targets");
            return targets;
        }

        [HttpGet("targets/{id}")]
        public ActionResult<RadarTarget> GetTarget(int id)
        {
            var target = _radarProcessor.GetTargetById(id);
            if (target == null)
            {
                _logger.LogWarning($"Target with ID {id} not found");
                return NotFound($"Target with ID {id} not found");
            }

            _logger.LogInformation($"Returning radar target with ID: {id}");
            return Ok(target);
        }

        [HttpGet("statistics")]
        public ActionResult<ProcessedRadarData> GetStatistics()
        {
            var stats = _radarProcessor.GetProcessingStatistics();
            _logger.LogInformation("Returning radar processing statistics");
            return Ok(stats);
        }

        [HttpGet("health")]
        public ActionResult<SystemHealth> GetSystemHealth()
        {
            var health = _radarProcessor.GetSystemHealth();
            _logger.LogInformation("System health check completed");
            return Ok(health);
        }

        [HttpGet("targets/type/{targetType}")]
        public ActionResult<IEnumerable<RadarTarget>> GetTargetsByType(TargetType targetType)
        {
            var filteredTargets = _radarProcessor.GetTargetsByType(targetType);
            _logger.LogInformation($"Returning {filteredTargets.Count()} targets of type: {targetType}");
            return Ok(filteredTargets);
        }

        [HttpPost("simulate")]
        public ActionResult SimulateRadarTargets()
        {
            // Add some simulated targets for testing
            var random = new Random();

            for (int i = 1; i <= 5; i++)
            {
                var target = new RadarTarget
                {
                    Id = 1000 + i,
                    X = random.NextDouble() * 50000 - 25000, // -25km to +25km
                    Y = random.NextDouble() * 50000 - 25000,
                    Velocity = random.NextDouble() * 300 + 50, // 50-350 m/s
                    Heading = random.NextDouble() * 360,
                    Type = (TargetType)(random.Next(1, 5)),
                    Timestamp = DateTime.UtcNow,
                    SignalStrength = random.NextDouble() * 40 + 60 // 60-100 signal strength
                };

                _radarProcessor.AddRadarTarget(target);
            }

            _logger.LogInformation("Added 5 simulated radar targets");
            return Ok(new { message = "5 simulated targets added successfully" });
        }

        [HttpDelete("targets")]
        public ActionResult ClearAllTargets()
        {
            // This would clear all targets - implement if needed
            _logger.LogInformation("Clear targets endpoint called");
            return Ok(new { message = "Clear targets functionality not implemented yet" });
        }
    }
}