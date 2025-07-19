using RadarProcessing.API.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace RadarProcessing.API.Services
{
    public class RadarDataProcessor : IRadarDataProcessor
    {
        private readonly ILogger<RadarDataProcessor> _logger;
        private readonly ConcurrentQueue<RadarTarget> _incomingData;
        private readonly ConcurrentDictionary<int, RadarTarget> _activeTargets;
        private readonly Timer _processingTimer;
        private readonly object _statsLock = new object();

        // Performance monitoring
        private DateTime _lastProcessingTime;
        private int _processedCount;
        private double _averageProcessingTimeMs;
        private readonly Stopwatch _processingStopwatch;

        // Configuration
        private const int PROCESSING_INTERVAL_MS = 50; // 20 Hz processing rate
        private const int TARGET_TIMEOUT_SECONDS = 30;
        private const double SIGNAL_THRESHOLD = 25.0;

        public RadarDataProcessor(ILogger<RadarDataProcessor> logger)
        {
            _logger = logger;
            _incomingData = new ConcurrentQueue<RadarTarget>();
            _activeTargets = new ConcurrentDictionary<int, RadarTarget>();
            _processingStopwatch = new Stopwatch();
            _lastProcessingTime = DateTime.UtcNow;

            // Start real-time processing timer
            _processingTimer = new Timer(ProcessRadarData, null,
                TimeSpan.FromMilliseconds(PROCESSING_INTERVAL_MS),
                TimeSpan.FromMilliseconds(PROCESSING_INTERVAL_MS));

            _logger.LogInformation("RadarDataProcessor started with {Interval}ms processing interval",
                PROCESSING_INTERVAL_MS);
        }

        public void AddRadarTarget(RadarTarget target)
        {
            if (target == null) return;

            // Validate signal strength
            if (target.SignalStrength < SIGNAL_THRESHOLD)
            {
                _logger.LogDebug("Target {Id} signal strength {Signal} below threshold {Threshold}",
                    target.Id, target.SignalStrength, SIGNAL_THRESHOLD);
                return;
            }

            _incomingData.Enqueue(target);
            _logger.LogDebug("Enqueued radar target {Id} for processing", target.Id);
        }

        private void ProcessRadarData(object? state)
        {
            _processingStopwatch.Restart();

            try
            {
                // Process incoming radar data
                int processedThisCycle = 0;

                while (_incomingData.TryDequeue(out RadarTarget? newTarget) && processedThisCycle < 100)
                {
                    ProcessSingleTarget(newTarget);
                    processedThisCycle++;
                }

                // Remove expired targets
                RemoveExpiredTargets();

                // Update performance statistics
                UpdateProcessingStats(processedThisCycle);

                _logger.LogDebug("Processing cycle completed: {Processed} targets, {Active} active, {QueueSize} queued",
                    processedThisCycle, _activeTargets.Count, _incomingData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during radar data processing cycle");
            }
            finally
            {
                _processingStopwatch.Stop();
            }
        }

        private void ProcessSingleTarget(RadarTarget target)
        {
            // Update timestamp to current processing time
            target.Timestamp = DateTime.UtcNow;

            // Check if target already exists (update) or is new
            if (_activeTargets.ContainsKey(target.Id))
            {
                // Update existing target with new data
                _activeTargets.TryUpdate(target.Id, target, _activeTargets[target.Id]);
                _logger.LogDebug("Updated existing target {Id} at position ({X}, {Y})",
                    target.Id, target.X, target.Y);
            }
            else
            {
                // Add new target
                _activeTargets.TryAdd(target.Id, target);
                _logger.LogInformation("New radar target detected: {Id} - {Type} at ({X}, {Y})",
                    target.Id, target.Type, target.X, target.Y);
            }
        }

        private void RemoveExpiredTargets()
        {
            var expiredTargets = _activeTargets.Values
                .Where(t => DateTime.UtcNow.Subtract(t.Timestamp).TotalSeconds > TARGET_TIMEOUT_SECONDS)
                .ToList();

            foreach (var expiredTarget in expiredTargets)
            {
                if (_activeTargets.TryRemove(expiredTarget.Id, out _))
                {
                    _logger.LogInformation("Removed expired target {Id} - last seen {LastSeen}",
                        expiredTarget.Id, expiredTarget.Timestamp);
                }
            }
        }

        private void UpdateProcessingStats(int processedCount)
        {
            lock (_statsLock)
            {
                _processedCount += processedCount;
                _lastProcessingTime = DateTime.UtcNow;

                // Calculate rolling average processing time
                var currentProcessingTime = _processingStopwatch.Elapsed.TotalMilliseconds;
                _averageProcessingTimeMs = (_averageProcessingTimeMs + currentProcessingTime) / 2.0;
            }
        }

        public IEnumerable<RadarTarget> GetActiveTargets()
        {
            return _activeTargets.Values
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.SignalStrength)
                .ToList();
        }

        public RadarTarget? GetTargetById(int id)
        {
            _activeTargets.TryGetValue(id, out RadarTarget? target);
            return target;
        }

        public IEnumerable<RadarTarget> GetTargetsByType(TargetType targetType)
        {
            return _activeTargets.Values
                .Where(t => t.Type == targetType && t.IsActive)
                .OrderByDescending(t => t.SignalStrength)
                .ToList();
        }

        public ProcessedRadarData GetProcessingStatistics()
        {
            lock (_statsLock)
            {
                var targetsByType = _activeTargets.Values
                    .Where(t => t.IsActive)
                    .GroupBy(t => t.Type)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new ProcessedRadarData
                {
                    ActiveTargets = _activeTargets.Count(kvp => kvp.Value.IsActive),
                    ProcessingLatencyMs = _averageProcessingTimeMs,
                    DataThroughputMbps = CalculateThroughput(),
                    LastProcessedTime = _lastProcessingTime,
                    TargetsByType = targetsByType,
                    ProcessingTimeMs = _processingStopwatch.Elapsed.TotalMilliseconds
                };
            }
        }

        public SystemHealth GetSystemHealth()
        {
            var process = Process.GetCurrentProcess();

            return new SystemHealth
            {
                IsHealthy = _activeTargets.Count < 1000 && _incomingData.Count < 500,
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = GetCpuUsage(),
                MemoryUsageMb = process.WorkingSet64 / (1024.0 * 1024.0),
                ProcessingQueueSize = _incomingData.Count,
                UptimeSeconds = (long)DateTime.UtcNow.Subtract(_lastProcessingTime).TotalSeconds,
                ErrorCount = 0, // Could be tracked separately
                WarningCount = _activeTargets.Count > 500 ? 1 : 0
            };
        }

        private double CalculateThroughput()
        {
            // Estimate data throughput based on targets processed
            const double bytesPerTarget = 100; // Rough estimate
            var targetsPerSecond = _processedCount / Math.Max(1, (DateTime.UtcNow - _lastProcessingTime).TotalSeconds);
            return (targetsPerSecond * bytesPerTarget * 8) / (1024 * 1024); // Mbps
        }

        private double GetCpuUsage()
        {
            // Simplified CPU usage estimation
            // In production, you'd use PerformanceCounter or similar
            return _processingStopwatch.Elapsed.TotalMilliseconds / PROCESSING_INTERVAL_MS * 100;
        }

        public void Dispose()
        {
            _processingTimer?.Dispose();
            _logger.LogInformation("RadarDataProcessor disposed");
        }
    }

    public interface IRadarDataProcessor : IDisposable
    {
        void AddRadarTarget(RadarTarget target);
        IEnumerable<RadarTarget> GetActiveTargets();
        RadarTarget? GetTargetById(int id);
        IEnumerable<RadarTarget> GetTargetsByType(TargetType targetType);
        ProcessedRadarData GetProcessingStatistics();
        SystemHealth GetSystemHealth();
    }
}