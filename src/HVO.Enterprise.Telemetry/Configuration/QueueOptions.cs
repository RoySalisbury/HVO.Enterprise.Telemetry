namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Configures the in-memory background queue that buffers telemetry payloads before export.
    /// </summary>
    public sealed class QueueOptions
    {
        /// <summary>
        /// Gets or sets the queue capacity (number of items). Default: <c>10000</c>. Values below 100 are rejected.
        /// </summary>
        public int Capacity { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the maximum batch size flushed per exporter invocation. Default: <c>100</c>.
        /// Must be greater than zero and less than or equal to <see cref="Capacity"/>.
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }
}
