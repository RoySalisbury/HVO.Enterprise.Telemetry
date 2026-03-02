using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Root configuration for HVO.Enterprise.Telemetry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options are bound from configuration providers (appsettings.json, environment variables, etc.) or
    /// configured programmatically via <c>services.AddTelemetry(...)</c>. Each property documents its default value
    /// and constraints so that generated XML documentation and IntelliSense accurately reflect the runtime behavior.
    /// </para>
    /// </remarks>
    public sealed class TelemetryOptions
    {
        /// <summary>
        /// Gets or sets the logical service name used for metric dimensions, traces, and exporter-specific tags.
        /// Default: <c>"Unknown"</c>.
        /// </summary>
        public string ServiceName { get; set; } = "Unknown";

        /// <summary>
        /// Gets or sets the semantic version of the service. Optional but strongly recommended for exporters such as Datadog or OTLP.
        /// </summary>
        public string? ServiceVersion { get; set; }

        /// <summary>
        /// Gets or sets the deployment environment (for example, <c>Production</c>, <c>Staging</c>, or <c>Development</c>).
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Gets or sets whether telemetry is enabled globally. Set to <see langword="false"/> to short-circuit all instrumentation.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default sampling rate (0.0 to 1.0 inclusive) applied to every Activity source
        /// unless overridden in <see cref="Sampling"/>. Default: <c>1.0</c> (always sample).
        /// </summary>
        public double DefaultSamplingRate { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets per-source sampling configuration keyed by Activity source name.
        /// When empty, the <see cref="DefaultSamplingRate"/> applies to every source.
        /// </summary>
        public Dictionary<string, SamplingOptions> Sampling { get; set; } =
            new Dictionary<string, SamplingOptions>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets logging configuration for correlation enrichment and minimum log levels.
        /// </summary>
        public LoggingOptions Logging { get; set; } = new LoggingOptions();

        /// <summary>
        /// Gets or sets metrics configuration (enablement and collection interval).
        /// </summary>
        public MetricsOptions Metrics { get; set; } = new MetricsOptions();

        /// <summary>
        /// Gets or sets background queue configuration for batching exporters. Controls capacity and batch sizes.
        /// </summary>
        public QueueOptions Queue { get; set; } = new QueueOptions();

        /// <summary>
        /// Gets or sets feature flags that toggle auto-instrumentation components.
        /// </summary>
        public FeatureFlags Features { get; set; } = new FeatureFlags();

        /// <summary>
        /// Gets or sets Activity source names to enable for automatic tracing. Default contains <c>"HVO.Enterprise.Telemetry"</c>.
        /// </summary>
        public List<string> ActivitySources { get; set; } = new List<string>
        {
            "HVO.Enterprise.Telemetry"
        };

        /// <summary>
        /// Gets or sets resource attributes (key-value pairs) that are attached to spans, metrics, and logs.
        /// Values should be serializable via <see cref="string"/>.
        /// </summary>
        public Dictionary<string, object> ResourceAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when configuration values are outside accepted ranges.
        /// </exception>
        public void Validate()
        {
            EnsureDefaults();

            if (string.IsNullOrWhiteSpace(ServiceName))
                throw new InvalidOperationException("ServiceName is required.");

            if (DefaultSamplingRate < 0.0 || DefaultSamplingRate > 1.0)
                throw new InvalidOperationException("DefaultSamplingRate must be between 0.0 and 1.0");

            if (Queue.Capacity < 100)
                throw new InvalidOperationException("Queue capacity must be at least 100");

            if (Queue.BatchSize <= 0 || Queue.BatchSize > Queue.Capacity)
                throw new InvalidOperationException("Queue batch size must be between 1 and capacity");

            if (Metrics.CollectionIntervalSeconds <= 0)
                throw new InvalidOperationException("Metrics collection interval must be greater than zero");

            foreach (var kvp in Sampling)
            {
                if (kvp.Value == null)
                    throw new InvalidOperationException("Sampling options must not be null");

                if (kvp.Value.Rate < 0.0 || kvp.Value.Rate > 1.0)
                    throw new InvalidOperationException("Sampling rate for '" + kvp.Key + "' must be between 0.0 and 1.0");
            }
        }

        private void EnsureDefaults()
        {
            Sampling ??= new Dictionary<string, SamplingOptions>(StringComparer.OrdinalIgnoreCase);
            Logging ??= new LoggingOptions();
            Metrics ??= new MetricsOptions();
            Queue ??= new QueueOptions();
            Features ??= new FeatureFlags();
            ActivitySources ??= new List<string> { "HVO.Enterprise.Telemetry" };
            ResourceAttributes ??= new Dictionary<string, object>();

            Logging.MinimumLevel ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
