namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Sampling configuration for a specific Activity source name.
    /// </summary>
    public sealed class SamplingOptions
    {
        /// <summary>
        /// Gets or sets the sampling rate (0.0 to 1.0 inclusive). Default: <c>1.0</c> (always sample).
        /// </summary>
        public double Rate { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets whether Activities containing exceptions are forced to record full data regardless of
        /// <see cref="Rate"/>. Default: <see langword="true"/>.
        /// </summary>
        public bool AlwaysSampleErrors { get; set; } = true;
    }
}
