# US-035: Grafana Stack Extension Package (Loki, Tempo, Mimir)

**GitHub Issue**: [#82](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/82)  
**Status**: ❌ Not Started  
**Category**: Extension Package  
**Effort**: 5 story points  
**Sprint**: 12

## Description

As a **developer using the Grafana observability stack**,  
I want **direct log shipping to Grafana Loki with HVO telemetry context, and OTLP-based trace/metrics routing to Tempo and Mimir**,  
So that **my Grafana dashboards show correlated logs, traces, and metrics from HVO-instrumented services with zero-config correlation**.

## Background

The Grafana observability stack is the fastest-growing open-source alternative to commercial APM
platforms. It consists of three main components:

- **Grafana Loki** — Log aggregation (like Seq or ELK but with label-based indexing)
- **Grafana Tempo** — Distributed tracing backend (accepts OTLP, Jaeger, Zipkin formats)
- **Grafana Mimir** — Metrics backend (accepts Prometheus remote write, OTLP)

**Tempo** and **Mimir** accept OTLP natively, so they are already covered by US-033
(OpenTelemetry/OTLP extension). This story focuses on **Grafana Loki** which has its own push API
(`/loki/api/v1/push`) and a unique label-based data model that requires a dedicated integration.

This extension provides:
1. **Loki Log Pusher** — Ships structured logs to Loki's HTTP push API with HVO correlation labels.
2. **Label Extraction** — Extracts `CorrelationId`, `TraceId`, `service_name`, `level` as Loki labels
   for efficient filtering in Grafana.
3. **OTLP Topology Helper** — Convenience method to configure US-033's OTLP export for the typical
   Grafana Cloud or self-hosted Grafana stack topology (Tempo endpoint, Mimir endpoint, Loki endpoint).

## Acceptance Criteria

1. **Package Structure**
   - [ ] `HVO.Enterprise.Telemetry.Grafana.csproj` created targeting `netstandard2.0`
   - [ ] Package builds with zero warnings
   - [ ] Dependencies: `HVO.Enterprise.Telemetry`, `System.Text.Json` (for JSON serialization)

2. **Loki Log Push Integration**
   - [ ] `LokiLogExporter` pushes structured log events to `/loki/api/v1/push`
   - [ ] Supports Loki push API JSON format (streams with labels + entries)
   - [ ] Batches log entries for efficient HTTP posting
   - [ ] Configurable flush interval, batch size, and retry policy
   - [ ] gzip compression support for reduced bandwidth
   - [ ] Basic auth and bearer token authentication for Grafana Cloud

3. **Loki Label Extraction**
   - [ ] Extracts `level` (log level) as Loki label
   - [ ] Extracts `service_name` from configuration as Loki label
   - [ ] Extracts `CorrelationId` as a structured metadata field (not indexed label)
   - [ ] Extracts `TraceId` as a structured metadata field for Tempo correlation
   - [ ] Configurable additional static labels (e.g., `environment`, `team`)
   - [ ] Respects Loki label cardinality best practices (low-cardinality labels only)

4. **Grafana Stack Topology Helper**
   - [ ] `TelemetryBuilder.WithGrafanaStack()` convenience method
   - [ ] Configures Loki endpoint for logs
   - [ ] Configures OTLP endpoint for traces (Tempo) and metrics (Mimir) via US-033
   - [ ] Support for Grafana Cloud endpoints with instance ID and API key
   - [ ] Support for self-hosted Grafana stack with custom URLs

5. **Configuration Extensions**
   - [ ] `IServiceCollection.AddGrafanaTelemetry()` extension method
   - [ ] `TelemetryBuilder.WithGrafana()` fluent API
   - [ ] `TelemetryBuilder.WithLoki()` fluent API (Loki only, without Tempo/Mimir)
   - [ ] `IOptions<GrafanaOptions>` pattern consistent with other extensions
   - [ ] Environment variable fallback (`GRAFANA_LOKI_ENDPOINT`, `GRAFANA_CLOUD_API_KEY`)
   - [ ] Idempotency guard

6. **Grafana Tempo/Mimir via OTLP**
   - [ ] `.WithGrafanaStack()` registers US-033 OTLP export configured for Tempo endpoint
   - [ ] Passes Grafana Cloud auth headers to OTLP exporter
   - [ ] Falls gracefully to no-op if US-033 is not referenced (log warning)

7. **Cross-Platform Support**
   - [ ] Works on .NET Framework 4.8
   - [ ] Works on .NET 8+
   - [ ] HTTP posting uses `System.Net.Http.HttpClient` (netstandard2.0 compatible)

## Technical Requirements

### Package Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    
    <PackageId>HVO.Enterprise.Telemetry.Grafana</PackageId>
    <Version>1.0.0-preview.1</Version>
    <Authors>HVO Enterprise</Authors>
    <Description>Grafana stack integration for HVO.Enterprise.Telemetry — Loki log shipping, Tempo trace correlation, and Grafana Cloud convenience configuration</Description>
    <PackageTags>telemetry;grafana;loki;tempo;mimir;logging;tracing;metrics;observability</PackageTags>
    <RepositoryUrl>https://github.com/RoySalisbury/HVO.Enterprise.Telemetry</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
    <ProjectReference Include="..\HVO.Enterprise.Telemetry\HVO.Enterprise.Telemetry.csproj" />
  </ItemGroup>
  
  <!-- Optional: reference OTel extension for WithGrafanaStack() topology helper -->
  <!-- <ProjectReference Include="..\HVO.Enterprise.Telemetry.OpenTelemetry\HVO.Enterprise.Telemetry.OpenTelemetry.csproj" /> -->
</Project>
```

### GrafanaOptions

```csharp
using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Grafana
{
    /// <summary>
    /// Configuration options for Grafana stack integration (Loki, Tempo, Mimir).
    /// </summary>
    public sealed class GrafanaOptions
    {
        /// <summary>
        /// Gets or sets the Grafana Loki push endpoint.
        /// Falls back to <c>GRAFANA_LOKI_ENDPOINT</c> environment variable.
        /// Default: <c>"http://localhost:3100"</c>.
        /// </summary>
        public string LokiEndpoint { get; set; } = "http://localhost:3100";

        /// <summary>
        /// Gets or sets the Grafana Tempo OTLP endpoint (for trace export via US-033).
        /// Falls back to <c>GRAFANA_TEMPO_ENDPOINT</c> environment variable.
        /// Default: <c>"http://localhost:4317"</c>.
        /// </summary>
        public string TempoEndpoint { get; set; } = "http://localhost:4317";

        /// <summary>
        /// Gets or sets the service name used as a Loki label.
        /// Falls back to <c>OTEL_SERVICE_NAME</c> environment variable.
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the deployment environment label.
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Loki log push is enabled.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableLoki { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to configure OTLP for Tempo/Mimir
        /// via the OpenTelemetry extension (US-033).
        /// Default: <see langword="false"/> (opt-in — requires <c>HVO.Enterprise.Telemetry.OpenTelemetry</c>).
        /// </summary>
        public bool EnableTempoOtlp { get; set; }

        /// <summary>
        /// Gets or sets the Grafana Cloud instance ID (for cloud authentication).
        /// </summary>
        public string? GrafanaCloudInstanceId { get; set; }

        /// <summary>
        /// Gets or sets the Grafana Cloud API key (for cloud authentication).
        /// Falls back to <c>GRAFANA_CLOUD_API_KEY</c> environment variable.
        /// </summary>
        public string? GrafanaCloudApiKey { get; set; }

        /// <summary>
        /// Gets or sets the Loki push batch size.
        /// Default: 100 entries per batch.
        /// </summary>
        public int LokiBatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the Loki flush interval.
        /// Default: 3 seconds.
        /// </summary>
        public TimeSpan LokiFlushInterval { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Gets or sets a value indicating whether to use gzip compression for Loki push.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool LokiGzipCompression { get; set; } = true;

        /// <summary>
        /// Gets or sets the authentication mode for Loki.
        /// Default: <see cref="LokiAuthMode.None"/>.
        /// </summary>
        public LokiAuthMode AuthMode { get; set; } = LokiAuthMode.None;

        /// <summary>
        /// Gets or sets the basic auth username (for Grafana Cloud: instance ID).
        /// </summary>
        public string? BasicAuthUser { get; set; }

        /// <summary>
        /// Gets or sets the basic auth password (for Grafana Cloud: API key).
        /// </summary>
        public string? BasicAuthPassword { get; set; }

        /// <summary>
        /// Gets or sets the bearer token for token-based auth.
        /// </summary>
        public string? BearerToken { get; set; }

        /// <summary>
        /// Gets or sets static labels added to all Loki streams.
        /// Use low-cardinality values only (service, environment, team).
        /// </summary>
        public IDictionary<string, string> StaticLabels { get; set; } =
            new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the HTTP timeout for Loki push.
        /// Default: 10 seconds.
        /// </summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Applies environment variable defaults. Explicit values take precedence.
        /// </summary>
        internal void ApplyEnvironmentDefaults()
        {
            var lokiEndpoint = System.Environment.GetEnvironmentVariable("GRAFANA_LOKI_ENDPOINT");
            if (!string.IsNullOrEmpty(lokiEndpoint) && LokiEndpoint == "http://localhost:3100")
            {
                LokiEndpoint = lokiEndpoint;
            }

            var tempoEndpoint = System.Environment.GetEnvironmentVariable("GRAFANA_TEMPO_ENDPOINT");
            if (!string.IsNullOrEmpty(tempoEndpoint) && TempoEndpoint == "http://localhost:4317")
            {
                TempoEndpoint = tempoEndpoint;
            }

            ServiceName ??= System.Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");

            GrafanaCloudApiKey ??= System.Environment.GetEnvironmentVariable("GRAFANA_CLOUD_API_KEY");

            // Grafana Cloud auto-configuration
            if (!string.IsNullOrEmpty(GrafanaCloudInstanceId) && !string.IsNullOrEmpty(GrafanaCloudApiKey))
            {
                AuthMode = LokiAuthMode.BasicAuth;
                BasicAuthUser ??= GrafanaCloudInstanceId;
                BasicAuthPassword ??= GrafanaCloudApiKey;
            }
        }
    }

    /// <summary>
    /// Loki authentication mode.
    /// </summary>
    public enum LokiAuthMode
    {
        /// <summary>No authentication.</summary>
        None = 0,

        /// <summary>Basic auth (username + password). Used by Grafana Cloud.</summary>
        BasicAuth = 1,

        /// <summary>Bearer token authentication.</summary>
        BearerToken = 2
    }
}
```

### LokiLogExporter

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Grafana
{
    /// <summary>
    /// Exports structured log events to Grafana Loki via its HTTP push API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Posts log entries to <c>/loki/api/v1/push</c> in Loki's JSON push format.
    /// Events are batched and sent on a configurable interval for efficiency.
    /// </para>
    /// <para>
    /// Labels are extracted following Loki best practices — only low-cardinality
    /// values (level, service_name, environment) are used as labels. High-cardinality
    /// values (CorrelationId, TraceId) are embedded as structured metadata.
    /// </para>
    /// <para>Thread-safe. Register as singleton.</para>
    /// </remarks>
    public sealed class LokiLogExporter : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentQueue<LokiEntry> _buffer;
        private readonly Timer _flushTimer;
        private readonly GrafanaOptions _options;
        private readonly ILogger<LokiLogExporter>? _logger;
        private readonly string _serviceLabel;
        private readonly bool _gzip;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LokiLogExporter"/> class.
        /// </summary>
        public LokiLogExporter(GrafanaOptions options, ILogger<LokiLogExporter>? logger = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _options = options;
            _logger = logger;
            _gzip = options.LokiGzipCompression;
            _serviceLabel = options.ServiceName ?? "unknown";
            _buffer = new ConcurrentQueue<LokiEntry>();

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.LokiEndpoint),
                Timeout = options.HttpTimeout
            };

            // Authentication
            if (options.AuthMode == LokiAuthMode.BasicAuth
                && !string.IsNullOrEmpty(options.BasicAuthUser)
                && !string.IsNullOrEmpty(options.BasicAuthPassword))
            {
                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{options.BasicAuthUser}:{options.BasicAuthPassword}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);
            }
            else if (options.AuthMode == LokiAuthMode.BearerToken
                && !string.IsNullOrEmpty(options.BearerToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.BearerToken);
            }

            _flushTimer = new Timer(
                _ => FlushAsync().ConfigureAwait(false),
                null,
                options.LokiFlushInterval,
                options.LokiFlushInterval);
        }

        /// <summary>
        /// Enqueues a log entry with current telemetry context.
        /// </summary>
        public void Push(string level, string message,
            IDictionary<string, string>? additionalLabels = null)
        {
            if (_disposed) return;

            var entry = new LokiEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = level,
                Message = message,
                CorrelationId = CorrelationContext.Current,
                TraceId = Activity.Current?.TraceId.ToString(),
                SpanId = Activity.Current?.SpanId.ToString(),
                AdditionalLabels = additionalLabels
            };

            _buffer.Enqueue(entry);
        }

        /// <summary>
        /// Flushes all buffered entries to Loki.
        /// </summary>
        public async Task FlushAsync()
        {
            if (_buffer.IsEmpty) return;

            var entries = new List<LokiEntry>();
            while (entries.Count < _options.LokiBatchSize && _buffer.TryDequeue(out var entry))
            {
                entries.Add(entry);
            }

            if (entries.Count == 0) return;

            try
            {
                var payload = BuildLokiPayload(entries);
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);

                HttpContent content;
                if (_gzip)
                {
                    using var memStream = new MemoryStream();
                    using (var gzipStream = new GZipStream(memStream, CompressionMode.Compress, leaveOpen: true))
                    {
                        gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
                    }
                    content = new ByteArrayContent(memStream.ToArray());
                    content.Headers.ContentEncoding.Add("gzip");
                }
                else
                {
                    content = new ByteArrayContent(jsonBytes);
                }

                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await _httpClient.PostAsync("/loki/api/v1/push", content)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to push {Count} entries to Loki", entries.Count);
            }
        }

        private object BuildLokiPayload(List<LokiEntry> entries)
        {
            // Group entries by label set (level + static labels)
            var streams = new Dictionary<string, (Dictionary<string, string> Labels, List<string[]> Values)>();

            foreach (var entry in entries)
            {
                var labels = new Dictionary<string, string>
                {
                    ["service_name"] = _serviceLabel,
                    ["level"] = entry.Level ?? "Information"
                };

                if (!string.IsNullOrEmpty(_options.Environment))
                    labels["environment"] = _options.Environment!;

                foreach (var kv in _options.StaticLabels)
                    labels[kv.Key] = kv.Value;

                if (entry.AdditionalLabels != null)
                {
                    foreach (var kv in entry.AdditionalLabels)
                        labels[kv.Key] = kv.Value;
                }

                var labelKey = string.Join(",", labels.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));

                if (!streams.ContainsKey(labelKey))
                    streams[labelKey] = (labels, new List<string[]>());

                // Build log line with structured metadata embedded
                var logLine = entry.Message ?? "";
                if (!string.IsNullOrEmpty(entry.CorrelationId))
                    logLine += $" CorrelationId={entry.CorrelationId}";
                if (!string.IsNullOrEmpty(entry.TraceId))
                    logLine += $" TraceId={entry.TraceId}";
                if (!string.IsNullOrEmpty(entry.SpanId))
                    logLine += $" SpanId={entry.SpanId}";

                var timestamp = entry.Timestamp.ToUnixTimeNanoseconds().ToString();
                streams[labelKey].Values.Add(new[] { timestamp, logLine });
            }

            return new
            {
                streams = streams.Values.Select(s => new
                {
                    stream = s.Labels,
                    values = s.Values
                }).ToArray()
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _flushTimer.Dispose();
            FlushAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            _httpClient.Dispose();
        }

        private sealed class LokiEntry
        {
            public DateTimeOffset Timestamp { get; set; }
            public string? Level { get; set; }
            public string? Message { get; set; }
            public string? CorrelationId { get; set; }
            public string? TraceId { get; set; }
            public string? SpanId { get; set; }
            public IDictionary<string, string>? AdditionalLabels { get; set; }
        }
    }
}
```

### ServiceCollectionExtensions

```csharp
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Grafana
{
    /// <summary>
    /// Extension methods for registering Grafana stack telemetry integration with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Grafana stack integration (Loki log push, optional Tempo/Mimir via OTLP).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="GrafanaOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddGrafanaTelemetry(
            this IServiceCollection services,
            Action<GrafanaOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (services.Any(s => s.ServiceType == typeof(LokiLogExporter)))
                return services;

            var optionsBuilder = services.AddOptions<GrafanaOptions>();
            if (configure != null)
                optionsBuilder.Configure(configure);

            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<GrafanaOptions>>().Value;
                options.ApplyEnvironmentDefaults();
                var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<LokiLogExporter>();
                return new LokiLogExporter(options, logger);
            });

            return services;
        }
    }
}
```

### TelemetryBuilderExtensions

```csharp
using System;

namespace HVO.Enterprise.Telemetry.Grafana
{
    /// <summary>
    /// Extension methods for integrating Grafana stack with the <see cref="TelemetryBuilder"/> fluent API.
    /// </summary>
    public static class TelemetryBuilderExtensions
    {
        /// <summary>
        /// Adds Grafana Loki log push integration.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="configure">Optional delegate to configure <see cref="GrafanaOptions"/>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddTelemetry(builder =>
        /// {
        ///     builder.WithLoki(options =>
        ///     {
        ///         options.LokiEndpoint = "http://loki:3100";
        ///         options.ServiceName = "my-service";
        ///     });
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithLoki(
            this TelemetryBuilder builder,
            Action<GrafanaOptions>? configure = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddGrafanaTelemetry(configure);
            return builder;
        }

        /// <summary>
        /// Adds full Grafana stack integration: Loki for logs + OTLP for traces (Tempo) and metrics (Mimir).
        /// Requires <c>HVO.Enterprise.Telemetry.OpenTelemetry</c> package for trace/metrics export.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="configure">Optional delegate to configure <see cref="GrafanaOptions"/>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <example>
        /// <code>
        /// // Self-hosted Grafana stack
        /// services.AddTelemetry(builder =>
        /// {
        ///     builder.WithGrafanaStack(options =>
        ///     {
        ///         options.LokiEndpoint = "http://loki:3100";
        ///         options.TempoEndpoint = "http://tempo:4317";
        ///         options.ServiceName = "my-service";
        ///         options.EnableTempoOtlp = true;
        ///     });
        /// });
        ///
        /// // Grafana Cloud
        /// services.AddTelemetry(builder =>
        /// {
        ///     builder.WithGrafanaStack(options =>
        ///     {
        ///         options.GrafanaCloudInstanceId = "123456";
        ///         options.GrafanaCloudApiKey = "glc_...";
        ///         options.LokiEndpoint = "https://logs-prod-us-central1.grafana.net";
        ///         options.TempoEndpoint = "https://tempo-us-central1.grafana.net:443";
        ///         options.ServiceName = "my-service";
        ///         options.EnableTempoOtlp = true;
        ///     });
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithGrafanaStack(
            this TelemetryBuilder builder,
            Action<GrafanaOptions>? configure = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // Register Loki
            builder.Services.AddGrafanaTelemetry(configure);

            // If EnableTempoOtlp is true, configure OTLP export for Tempo/Mimir
            // This requires HVO.Enterprise.Telemetry.OpenTelemetry to be referenced
            // Implementation will use reflection or compile-time reference to avoid hard dependency

            return builder;
        }
    }
}
```

## Sample Application Updates

### ServiceConfiguration.cs

```csharp
// Grafana Loki — log shipping with correlation labels
if (configuration.GetValue<bool>("Extensions:Grafana:Enabled"))
{
    services.AddGrafanaTelemetry(options =>
    {
        options.LokiEndpoint = configuration["Extensions:Grafana:LokiEndpoint"] ?? "http://localhost:3100";
        options.ServiceName = configuration["Extensions:Grafana:ServiceName"] ?? "hvo-sample";
        options.Environment = configuration["Extensions:Grafana:Environment"] ?? "development";
        options.EnableTempoOtlp = configuration.GetValue<bool>("Extensions:Grafana:EnableTempoOtlp");
        options.TempoEndpoint = configuration["Extensions:Grafana:TempoEndpoint"] ?? "http://localhost:4317";

        var cloudId = configuration["Extensions:Grafana:GrafanaCloudInstanceId"];
        if (!string.IsNullOrEmpty(cloudId))
        {
            options.GrafanaCloudInstanceId = cloudId;
            options.GrafanaCloudApiKey = configuration["Extensions:Grafana:GrafanaCloudApiKey"];
        }
    });
}
```

### appsettings.json Section

```jsonc
{
  "Extensions": {
    "Grafana": {
      "Enabled": false,
      "LokiEndpoint": "http://localhost:3100",
      "TempoEndpoint": "http://localhost:4317",
      "ServiceName": "hvo-sample",
      "Environment": "development",
      "EnableTempoOtlp": false,
      "GrafanaCloudInstanceId": "",
      "GrafanaCloudApiKey": "",
      "LokiBatchSize": 100,
      "LokiFlushIntervalSeconds": 3,
      "LokiGzipCompression": true
    }
  }
}
```

## Testing Requirements

### Unit Tests

```csharp
[TestClass]
public class GrafanaOptionsTests
{
    [TestMethod]
    public void Defaults_AreCorrect()
    {
        var options = new GrafanaOptions();

        Assert.AreEqual("http://localhost:3100", options.LokiEndpoint);
        Assert.AreEqual("http://localhost:4317", options.TempoEndpoint);
        Assert.IsTrue(options.EnableLoki);
        Assert.IsFalse(options.EnableTempoOtlp);
        Assert.AreEqual(100, options.LokiBatchSize);
        Assert.AreEqual(TimeSpan.FromSeconds(3), options.LokiFlushInterval);
        Assert.IsTrue(options.LokiGzipCompression);
        Assert.AreEqual(LokiAuthMode.None, options.AuthMode);
    }

    [TestMethod]
    public void ApplyEnvironmentDefaults_SetsLokiEndpointFromEnv()
    {
        var options = new GrafanaOptions();
        System.Environment.SetEnvironmentVariable("GRAFANA_LOKI_ENDPOINT", "http://loki:3100");

        options.ApplyEnvironmentDefaults();

        Assert.AreEqual("http://loki:3100", options.LokiEndpoint);
        System.Environment.SetEnvironmentVariable("GRAFANA_LOKI_ENDPOINT", null);
    }

    [TestMethod]
    public void ApplyEnvironmentDefaults_GrafanaCloud_SetsBasicAuth()
    {
        var options = new GrafanaOptions
        {
            GrafanaCloudInstanceId = "123456",
            GrafanaCloudApiKey = "glc_test_key"
        };

        options.ApplyEnvironmentDefaults();

        Assert.AreEqual(LokiAuthMode.BasicAuth, options.AuthMode);
        Assert.AreEqual("123456", options.BasicAuthUser);
        Assert.AreEqual("glc_test_key", options.BasicAuthPassword);
    }

    [TestMethod]
    public void ApplyEnvironmentDefaults_ExplicitValueTakesPrecedence()
    {
        var options = new GrafanaOptions { LokiEndpoint = "http://custom:3100" };
        System.Environment.SetEnvironmentVariable("GRAFANA_LOKI_ENDPOINT", "http://env:3100");

        options.ApplyEnvironmentDefaults();

        Assert.AreEqual("http://custom:3100", options.LokiEndpoint);
        System.Environment.SetEnvironmentVariable("GRAFANA_LOKI_ENDPOINT", null);
    }
}

[TestClass]
public class LokiLogExporterTests
{
    [TestMethod]
    public void Constructor_NullOptions_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new LokiLogExporter(null!));
    }

    [TestMethod]
    public void Push_EnqueuesEntry()
    {
        var options = new GrafanaOptions { LokiEndpoint = "http://localhost:3100" };
        using var exporter = new LokiLogExporter(options);

        exporter.Push("Information", "Test log message");

        // Entry is buffered — verified indirectly via flush
    }

    [TestMethod]
    public void Push_WithAdditionalLabels_Accepted()
    {
        var options = new GrafanaOptions();
        using var exporter = new LokiLogExporter(options);

        var labels = new Dictionary<string, string> { ["component"] = "api" };
        exporter.Push("Warning", "High latency detected", labels);
    }

    [TestMethod]
    public void Dispose_FlushesBufferedEntries()
    {
        var options = new GrafanaOptions
        {
            LokiFlushInterval = TimeSpan.FromMinutes(10)
        };
        using var exporter = new LokiLogExporter(options);

        exporter.Push("Information", "Test");
        // Dispose flushes — should not throw even if Loki is unavailable
    }

    [TestMethod]
    public void Constructor_WithBasicAuth_SetsAuthHeader()
    {
        var options = new GrafanaOptions
        {
            AuthMode = LokiAuthMode.BasicAuth,
            BasicAuthUser = "user",
            BasicAuthPassword = "pass"
        };
        using var exporter = new LokiLogExporter(options);
        // Verify no exception during construction with auth
    }
}

[TestClass]
public class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddGrafanaTelemetry_NullServices_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddGrafanaTelemetry());
    }

    [TestMethod]
    public void AddGrafanaTelemetry_Idempotent()
    {
        var services = new ServiceCollection();
        services.AddGrafanaTelemetry();
        services.AddGrafanaTelemetry();

        var count = services.Count(s => s.ServiceType == typeof(LokiLogExporter));
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void AddGrafanaTelemetry_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddGrafanaTelemetry(options =>
        {
            options.ServiceName = "test-service";
            options.LokiEndpoint = "http://loki:3100";
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<GrafanaOptions>>().Value;

        Assert.AreEqual("test-service", opts.ServiceName);
        Assert.AreEqual("http://loki:3100", opts.LokiEndpoint);
    }
}

[TestClass]
public class TelemetryBuilderExtensionsTests
{
    [TestMethod]
    public void WithLoki_NullBuilder_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            ((TelemetryBuilder)null!).WithLoki());
    }

    [TestMethod]
    public void WithLoki_RegistersLokiExporter()
    {
        var services = new ServiceCollection();
        services.AddTelemetry(builder => builder.WithLoki());

        Assert.IsTrue(services.Any(s => s.ServiceType == typeof(LokiLogExporter)));
    }

    [TestMethod]
    public void WithGrafanaStack_RegistersLoki()
    {
        var services = new ServiceCollection();
        services.AddTelemetry(builder => builder.WithGrafanaStack(options =>
        {
            options.ServiceName = "test";
        }));

        Assert.IsTrue(services.Any(s => s.ServiceType == typeof(LokiLogExporter)));
    }
}
```

## Performance Requirements

- **Push overhead**: <100ns per log entry (non-blocking buffer append)
- **Loki batch post**: Async, non-blocking, <100ms for 100-entry batch with gzip
- **gzip compression**: 60-80% size reduction typical for JSON log payloads
- **Memory**: <3MB for default buffer settings
- **Label lookup**: O(1) for static label injection

## Dependencies

**Blocked By**:
- US-001: Core Package Setup ✅
- US-002: Auto-Managed Correlation ✅

**Optional Dependency**:
- US-033: OpenTelemetry/OTLP Extension (for `.WithGrafanaStack()` Tempo/Mimir routing)

**Enhances**:
- US-033: OpenTelemetry Extension (provides pre-configured Grafana stack topology)

## Definition of Done

- [ ] `HVO.Enterprise.Telemetry.Grafana.csproj` builds with 0 warnings
- [ ] `GrafanaOptions` with env var fallback and Grafana Cloud auto-config tested
- [ ] `LokiLogExporter` HTTP push working with Loki JSON format
- [ ] gzip compression working
- [ ] Basic auth and bearer token auth working
- [ ] `ServiceCollectionExtensions.AddGrafanaTelemetry()` idempotent and tested
- [ ] `TelemetryBuilder.WithLoki()` fluent API working
- [ ] `TelemetryBuilder.WithGrafanaStack()` topology helper working
- [ ] All unit tests passing (>90% coverage)
- [ ] Sample app updated with Grafana configuration section
- [ ] XML documentation complete on all public APIs
- [ ] Zero warnings in build
- [ ] Code reviewed and approved

## Notes

### Design Decisions

1. **Why a separate Grafana package instead of just using OTel?**
   - Loki has its own push API — OTLP log support in Loki is experimental
   - Label-based data model requires specific label extraction logic
   - Grafana Cloud authentication (basic auth with instance ID) is Grafana-specific
   - Self-hosted Grafana stacks often run Loki without an OTel Collector

2. **Why low-cardinality labels only?**
   - Loki indexes by labels — high-cardinality labels cause performance issues
   - CorrelationId and TraceId go into log line content or structured metadata, not labels
   - `service_name`, `level`, `environment` are ideal Loki labels

3. **Why gzip compression by default?**
   - Log payloads are highly compressible (60-80% reduction)
   - Reduces bandwidth significantly for high-volume services
   - Loki accepts gzip-encoded payloads natively
   - Minor CPU cost is offset by network savings

4. **Why optional Tempo/Mimir via OTLP?**
   - Tempo and Mimir accept OTLP — no custom integration needed
   - The OTel extension (US-033) handles OTLP export generically
   - `WithGrafanaStack()` is a convenience topology helper, not a new protocol implementation
   - Keeps this package focused on Loki (the only Grafana component needing custom integration)

### Docker Compose for Local Grafana Stack

```yaml
services:
  loki:
    image: grafana/loki:latest
    ports:
      - "3100:3100"
    command: -config.file=/etc/loki/local-config.yaml

  tempo:
    image: grafana/tempo:latest
    ports:
      - "3200:3200"   # Tempo API
      - "4317:4317"   # OTLP gRPC
    command: -config.file=/etc/tempo/config.yaml

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_AUTH_ANONYMOUS_ENABLED=true
      - GF_AUTH_ANONYMOUS_ORG_ROLE=Admin
    volumes:
      - ./grafana-datasources.yaml:/etc/grafana/provisioning/datasources/datasources.yaml
```

## Related Documentation

- [Grafana Loki HTTP API](https://grafana.com/docs/loki/latest/reference/api/#push-log-entries-to-loki)
- [Grafana Loki Label Best Practices](https://grafana.com/docs/loki/latest/best-practices/)
- [Grafana Tempo with OTLP](https://grafana.com/docs/tempo/latest/getting-started/)
- [Grafana Cloud Authentication](https://grafana.com/docs/grafana-cloud/account-management/authentication-and-permissions/)
- [US-033: OpenTelemetry/OTLP Extension](./US-033-opentelemetry-otlp-extension.md)
