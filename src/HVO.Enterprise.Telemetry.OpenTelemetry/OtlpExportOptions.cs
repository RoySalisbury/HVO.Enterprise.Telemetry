using System;
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace HVO.Enterprise.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Configuration options for OpenTelemetry OTLP export integration.
    /// </summary>
    /// <remarks>
    /// All properties support environment-variable fallbacks following OpenTelemetry conventions
    /// (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, <c>OTEL_SERVICE_NAME</c>, etc.).
    /// When this options type is registered via the OpenTelemetry integration (for example, through
    /// dependency injection extension methods), environment variable values are merged into the
    /// instance automatically and no additional method calls are required by consumers.
    /// </remarks>
    public sealed class OtlpExportOptions
    {
        /// <summary>
        /// Gets or sets the OTLP collector endpoint.
        /// Falls back to <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable.
        /// Default: <c>"http://localhost:4317"</c> (gRPC).
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:4317";

        /// <summary>
        /// Gets or sets the OTLP transport protocol.
        /// Default: <see cref="OtlpTransport.Grpc"/>.
        /// </summary>
        public OtlpTransport Transport { get; set; } = OtlpTransport.Grpc;

        /// <summary>
        /// Gets or sets the service name for resource attributes.
        /// Falls back to <c>OTEL_SERVICE_NAME</c> environment variable.
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the service version for resource attributes.
        /// </summary>
        public string? ServiceVersion { get; set; }

        /// <summary>
        /// Gets or sets the deployment environment for resource attributes.
        /// Falls back to <c>OTEL_RESOURCE_ATTRIBUTES</c> (<c>deployment.environment</c> key).
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether trace export is enabled.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableTraceExport { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether metrics export is enabled.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableMetricsExport { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether log export via OTLP is enabled.
        /// Default: <see langword="false"/> (opt-in).
        /// </summary>
        public bool EnableLogExport { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Prometheus scrape endpoint is enabled.
        /// Default: <see langword="false"/> (opt-in). Requires ASP.NET Core (.NET 6+).
        /// </summary>
        public bool EnablePrometheusEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the Prometheus scrape endpoint path.
        /// Default: <c>"/metrics"</c>.
        /// </summary>
        public string PrometheusEndpointPath { get; set; } = "/metrics";

        /// <summary>
        /// Gets or sets the metrics export interval.
        /// Default: 60 seconds.
        /// </summary>
        public TimeSpan MetricsExportInterval { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Gets or sets the trace batch export scheduled delay.
        /// Default: 5 seconds.
        /// </summary>
        public TimeSpan TraceBatchExportDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the maximum batch size for trace export.
        /// Default: 512.
        /// </summary>
        public int TraceBatchMaxSize { get; set; } = 512;

        /// <summary>
        /// Gets or sets the maximum queue size for trace export.
        /// Default: 2048.
        /// </summary>
        public int TraceBatchMaxQueueSize { get; set; } = 2048;

        /// <summary>
        /// Gets or sets additional resource attributes.
        /// </summary>
        public IDictionary<string, string> ResourceAttributes { get; set; } =
            new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the metrics temporality preference.
        /// Default: <see cref="MetricsTemporality.Cumulative"/>.
        /// </summary>
        public MetricsTemporality TemporalityPreference { get; set; } = MetricsTemporality.Cumulative;

        /// <summary>
        /// Gets or sets additional OTLP headers (e.g., API keys for hosted backends).
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } =
            new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets additional ActivitySource names to register in the TracerProvider.
        /// These are registered alongside the built-in HVO activity sources.
        /// </summary>
        /// <example>
        /// <code>
        /// options.AdditionalActivitySources.Add("MyApp");
        /// options.AdditionalActivitySources.Add("MyApp.HttpClient");
        /// </code>
        /// </example>
        public IList<string> AdditionalActivitySources { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets additional Meter names to register in the MeterProvider.
        /// These are registered alongside the built-in HVO meter.
        /// </summary>
        /// <example>
        /// <code>
        /// options.AdditionalMeterNames.Add("MyApp.Metrics");
        /// </code>
        /// </example>
        public IList<string> AdditionalMeterNames { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether standard .NET runtime meters are registered.
        /// When enabled, registers well-known meters such as <c>Microsoft.AspNetCore.Hosting</c>,
        /// <c>System.Net.Http</c>, and others.
        /// Default: <see langword="false"/> (opt-in).
        /// </summary>
        public bool EnableStandardMeters { get; set; }

        /// <summary>
        /// Gets or sets an optional callback to further configure the <see cref="TracerProviderBuilder"/>.
        /// Use this for advanced scenarios such as adding ASP.NET Core or HttpClient instrumentation.
        /// </summary>
        /// <example>
        /// <code>
        /// options.ConfigureTracerProvider = builder =>
        /// {
        ///     builder.AddAspNetCoreInstrumentation();
        ///     builder.AddHttpClientInstrumentation();
        /// };
        /// </code>
        /// </example>
        public Action<TracerProviderBuilder>? ConfigureTracerProvider { get; set; }

        /// <summary>
        /// Gets or sets an optional callback to further configure the <see cref="MeterProviderBuilder"/>.
        /// Use this for advanced scenarios such as adding custom instrumentation.
        /// </summary>
        public Action<MeterProviderBuilder>? ConfigureMeterProvider { get; set; }

        /// <summary>
        /// Applies environment variable defaults following OTel conventions.
        /// Explicit property values take precedence over environment variables.
        /// </summary>
        internal void ApplyEnvironmentDefaults()
        {
            var endpoint = System.Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            if (!string.IsNullOrEmpty(endpoint) && Endpoint == "http://localhost:4317")
            {
                Endpoint = endpoint;
            }

            // Auto-detect transport from well-known ports (only if transport was not explicitly configured)
            if (Transport == OtlpTransport.Grpc
                && Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri)
                && uri.Port == 4318)
            {
                Transport = OtlpTransport.HttpProtobuf;
            }

            ServiceName ??= System.Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");

            var resourceAttrs = System.Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");
            if (!string.IsNullOrEmpty(resourceAttrs))
            {
                var parsedPairs = resourceAttrs.Split(',')
                    .Select(pair => pair.Split('='))
                    .Where(parts => parts.Length == 2)
                    .Select(parts => (key: parts[0].Trim(), value: parts[1].Trim()));

                foreach (var (key, value) in parsedPairs)
                {
                    if (key == "deployment.environment" && Environment == null)
                    {
                        Environment = value;
                    }
                    if (!ResourceAttributes.ContainsKey(key))
                    {
                        ResourceAttributes[key] = value;
                    }
                }
            }

            var headers = System.Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
            if (!string.IsNullOrEmpty(headers))
            {
                var parsedHeaders = headers.Split(',')
                    .Select(pair => pair.Split('='))
                    .Where(parts => parts.Length == 2)
                    .Select(parts => (key: parts[0].Trim(), value: parts[1].Trim()))
                    .Where(h => !Headers.ContainsKey(h.key));

                foreach (var (key, value) in parsedHeaders)
                {
                    Headers[key] = value;
                }
            }
        }
    }
}
