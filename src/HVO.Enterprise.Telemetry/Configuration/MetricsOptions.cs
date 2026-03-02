namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Configures built-in metrics collection (EventCounters on .NET Framework, <see cref="System.Diagnostics.Metrics"/> elsewhere).
    /// </summary>
    public sealed class MetricsOptions
    {
        /// <summary>
        /// Gets or sets whether metrics collection is enabled. Default: <see langword="true"/>.
        /// Disable to remove the background collector entirely.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the metrics collection interval in seconds. Default: <c>10</c> seconds.
        /// Applies to polling-based exporters and EventCounter flush cadence.
        /// </summary>
        public int CollectionIntervalSeconds { get; set; } = 10;
    }
}
