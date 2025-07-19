namespace RadarProcessing.API.Models
{
    // Core radar target data
    public class RadarTarget
    {
        public int Id { get; set; }
        public double X { get; set; } // Position X coordinate
        public double Y { get; set; } // Position Y coordinate
        public double Velocity { get; set; } // Speed in m/s
        public double Heading { get; set; } // Direction in degrees
        public TargetType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public double SignalStrength { get; set; }

        // Calculated properties
        public double DistanceFromOrigin => Math.Sqrt(X * X + Y * Y);
        public bool IsActive => DateTime.UtcNow.Subtract(Timestamp).TotalSeconds < 300;
    }

    // Target classification enum
    public enum TargetType
    {
        Unknown = 0,
        Aircraft = 1,
        Ship = 2,
        Vehicle = 3,
        Missile = 4
    }

    // Processed radar system data
    public class ProcessedRadarData
    {
        public int ActiveTargets { get; set; }
        public double ProcessingLatencyMs { get; set; }
        public double DataThroughputMbps { get; set; }
        public DateTime LastProcessedTime { get; set; }
        public Dictionary<TargetType, int> TargetsByType { get; set; } = new();
        public double ProcessingTimeMs { get; set; }
    }

    // System health monitoring
    public class SystemHealth
    {
        public bool IsHealthy { get; set; }
        public DateTime Timestamp { get; set; }
        public double CpuUsagePercent { get; set; }
        public double MemoryUsageMb { get; set; }
        public int ProcessingQueueSize { get; set; }
        public long UptimeSeconds { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
    }

    // Real-time radar processing configuration
    public class RadarConfiguration
    {
        public int ProcessingIntervalMs { get; set; } = 50; // 20 Hz processing
        public double MaxTargetRange { get; set; } = 100000.0; // 100km max range
        public int MaxTargetsTracked { get; set; } = 1000;
        public double SignalThreshold { get; set; } = 30.0; // Minimum signal strength
    }

    // Radar sweep data
    public class RadarSweep
    {
        public int SweepId { get; set; }
        public DateTime Timestamp { get; set; }
        public double AzimuthAngle { get; set; }
        public List<RadarTarget> DetectedTargets { get; set; } = new();
        public int TargetCount => DetectedTargets.Count;
    }
}