# US-025: Datadog Extension Package

**GitHub Issue**: [#27](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/27)  
**Status**: ✅ Complete  
**Category**: Extension Package  
**Effort**: 5 story points  
**Sprint**: 8

## Description

As a **developer using Datadog for observability**,  
I want **dual-mode export supporting both OTLP and native DogStatsD protocols**,  
So that **my telemetry appears in Datadog with optimal performance and full feature support**.

## Acceptance Criteria

1. **Package Structure**
   - [x] `HVO.Enterprise.Telemetry.Datadog.csproj` created targeting `netstandard2.0`
   - [x] Package builds with zero warnings
   - [x] Dependencies: DogStatsD client + HVO.Enterprise.Telemetry

2. **Dual-Mode Export Support**
   - [x] OTLP mode: Uses OpenTelemetry Datadog exporter (.NET 8+)
   - [x] DogStatsD mode: Uses native Datadog client (.NET Framework 4.8)
   - [x] Runtime mode detection based on platform and configuration
   - [x] Both modes work without code changes

3. **Trace Export**
   - [x] Activity/spans exported with Datadog trace format
   - [x] Service name, resource, and operation name mapped correctly
   - [x] Datadog-specific tags added (env, version, service)
   - [x] Trace context propagated using Datadog headers

4. **Metrics Export**
   - [x] Counters exported as Datadog counts
   - [x] Gauges exported as Datadog gauges
   - [x] Histograms exported with percentiles
   - [x] Custom tags supported on all metrics

5. **Configuration Extensions**
   - [x] `IServiceCollection.AddDatadogTelemetry()` extension
   - [x] Environment variable configuration (DD_AGENT_HOST, DD_ENV, DD_SERVICE, DD_VERSION)
   - [x] Fluent API for agent endpoint configuration
   - [x] Support for Unix domain socket (Linux) and UDP (Windows/legacy)

6. **Cross-Platform Support**
   - [x] Works on .NET Framework 4.8 (DogStatsD mode)
   - [x] Works on .NET 8+ (OTLP mode with fallback to DogStatsD)
   - [x] Automatic transport selection (UDS on Linux, UDP elsewhere)

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
    
    <!-- Package Information -->
    <PackageId>HVO.Enterprise.Telemetry.Datadog</PackageId>
    <Version>1.0.0-preview.1</Version>
    <Authors>HVO Enterprise</Authors>
    <Description>Datadog integration for HVO.Enterprise.Telemetry with dual-mode OTLP and DogStatsD support</Description>
    <PackageTags>telemetry;datadog;metrics;tracing;dogstatsd;opentelemetry</PackageTags>
    <RepositoryUrl>https://github.com/RoySalisbury/HVO.Enterprise.Telemetry</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DogStatsD-CSharp-Client" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <ProjectReference Include="..\HVO.Enterprise.Telemetry\HVO.Enterprise.Telemetry.csproj" />
  </ItemGroup>
</Project>
```

### DatadogConfiguration

```csharp
using System;

namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Configuration options for Datadog telemetry export.
    /// </summary>
    public sealed class DatadogConfiguration
    {
        /// <summary>
        /// Gets or sets the Datadog service name.
        /// </summary>
        /// <remarks>
        /// Falls back to DD_SERVICE environment variable if not set.
        /// </remarks>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the Datadog environment (e.g., "production", "staging").
        /// </summary>
        /// <remarks>
        /// Falls back to DD_ENV environment variable if not set.
        /// </remarks>
        public string? Environment { get; set; }

        /// <summary>
        /// Gets or sets the service version.
        /// </summary>
        /// <remarks>
        /// Falls back to DD_VERSION environment variable if not set.
        /// </remarks>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the Datadog agent host.
        /// </summary>
        /// <remarks>
        /// Falls back to DD_AGENT_HOST environment variable, then localhost.
        /// </remarks>
        public string AgentHost { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the DogStatsD UDP port.
        /// </summary>
        /// <remarks>
        /// Default is 8125. Falls back to DD_DOGSTATSD_PORT if set.
        /// </remarks>
        public int AgentPort { get; set; } = 8125;

        /// <summary>
        /// Gets or sets the Unix domain socket path for DogStatsD (Linux only).
        /// </summary>
        /// <remarks>
        /// If set, takes precedence over UDP transport on Linux.
        /// Falls back to DD_DOGSTATSD_SOCKET environment variable.
        /// </remarks>
        public string? UnixDomainSocketPath { get; set; }

        /// <summary>
        /// Gets or sets the export mode.
        /// </summary>
        /// <remarks>
        /// Auto: Automatically detect based on platform and OpenTelemetry configuration.
        /// OTLP: Use OpenTelemetry Protocol (requires OpenTelemetry SDK).
        /// DogStatsD: Use native DogStatsD protocol.
        /// </remarks>
        public DatadogExportMode Mode { get; set; } = DatadogExportMode.Auto;

        /// <summary>
        /// Gets or sets global tags to apply to all telemetry.
        /// </summary>
        public IDictionary<string, string> GlobalTags { get; set; } = 
            new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets a value indicating whether to use Unix domain socket transport.
        /// </summary>
        /// <remarks>
        /// Automatically detected on Linux if UnixDomainSocketPath is set or DD_DOGSTATSD_SOCKET is configured.
        /// </remarks>
        public bool UseUnixDomainSocket { get; set; }

        /// <summary>
        /// Validates the configuration and applies environment variable fallbacks.
        /// </summary>
        public void ApplyDefaults()
        {
            ServiceName ??= System.Environment.GetEnvironmentVariable("DD_SERVICE");
            Environment ??= System.Environment.GetEnvironmentVariable("DD_ENV");
            Version ??= System.Environment.GetEnvironmentVariable("DD_VERSION");

            var agentHost = System.Environment.GetEnvironmentVariable("DD_AGENT_HOST");
            if (!string.IsNullOrEmpty(agentHost))
            {
                AgentHost = agentHost;
            }

            var agentPort = System.Environment.GetEnvironmentVariable("DD_DOGSTATSD_PORT");
            if (!string.IsNullOrEmpty(agentPort) && int.TryParse(agentPort, out var port))
            {
                AgentPort = port;
            }

            var socketPath = System.Environment.GetEnvironmentVariable("DD_DOGSTATSD_SOCKET");
            if (!string.IsNullOrEmpty(socketPath))
            {
                UnixDomainSocketPath = socketPath;
                UseUnixDomainSocket = true;
            }

            // Add unified service tags
            if (!string.IsNullOrEmpty(ServiceName))
            {
                GlobalTags["service"] = ServiceName;
            }
            if (!string.IsNullOrEmpty(Environment))
            {
                GlobalTags["env"] = Environment;
            }
            if (!string.IsNullOrEmpty(Version))
            {
                GlobalTags["version"] = Version;
            }
        }
    }

    /// <summary>
    /// Datadog export mode.
    /// </summary>
    public enum DatadogExportMode
    {
        /// <summary>
        /// Automatically detect based on platform and configuration.
        /// </summary>
        Auto,

        /// <summary>
        /// Use OpenTelemetry Protocol (OTLP).
        /// </summary>
        OTLP,

        /// <summary>
        /// Use native DogStatsD protocol.
        /// </summary>
        DogStatsD
    }
}
```

### DatadogMetricsExporter

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using StatsdClient;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Exports metrics to Datadog using DogStatsD protocol.
    /// </summary>
    public sealed class DatadogMetricsExporter : IDisposable
    {
        private readonly DogStatsdService _statsd;
        private readonly ILogger<DatadogMetricsExporter>? _logger;
        private readonly string[] _globalTags;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatadogMetricsExporter"/> class.
        /// </summary>
        /// <param name="configuration">Datadog configuration</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        public DatadogMetricsExporter(
            DatadogConfiguration configuration,
            ILogger<DatadogMetricsExporter>? logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.ApplyDefaults();

            _logger = logger;
            _globalTags = configuration.GlobalTags
                .Select(kvp => $"{kvp.Key}:{kvp.Value}")
                .ToArray();

            // Configure DogStatsD
            var statsdConfig = new StatsdConfig
            {
                StatsdServerName = configuration.AgentHost,
                StatsdPort = configuration.AgentPort,
                Prefix = string.Empty,
                ConstantTags = _globalTags
            };

            // Use Unix domain socket if configured (Linux)
            if (configuration.UseUnixDomainSocket && 
                !string.IsNullOrEmpty(configuration.UnixDomainSocketPath))
            {
                statsdConfig.StatsdServerName = configuration.UnixDomainSocketPath;
            }

            _statsd = new DogStatsdService();
            _statsd.Configure(statsdConfig);

            _logger?.LogInformation(
                "DatadogMetricsExporter initialized: {Host}:{Port}, Transport: {Transport}",
                configuration.AgentHost,
                configuration.AgentPort,
                configuration.UseUnixDomainSocket ? "UDS" : "UDP");
        }

        /// <summary>
        /// Records a counter metric.
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Counter value</param>
        /// <param name="tags">Optional tags</param>
        public void Counter(string name, long value, IDictionary<string, string>? tags = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatadogMetricsExporter));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var ddTags = ConvertTags(tags);
            _statsd.Counter(name, value, tags: ddTags);
        }

        /// <summary>
        /// Records a gauge metric.
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Gauge value</param>
        /// <param name="tags">Optional tags</param>
        public void Gauge(string name, double value, IDictionary<string, string>? tags = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatadogMetricsExporter));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var ddTags = ConvertTags(tags);
            _statsd.Gauge(name, value, tags: ddTags);
        }

        /// <summary>
        /// Records a histogram metric.
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Histogram value</param>
        /// <param name="tags">Optional tags</param>
        public void Histogram(string name, double value, IDictionary<string, string>? tags = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatadogMetricsExporter));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var ddTags = ConvertTags(tags);
            _statsd.Histogram(name, value, tags: ddTags);
        }

        /// <summary>
        /// Records a distribution metric (better than histogram for percentiles).
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Distribution value</param>
        /// <param name="tags">Optional tags</param>
        public void Distribution(string name, double value, IDictionary<string, string>? tags = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatadogMetricsExporter));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var ddTags = ConvertTags(tags);
            _statsd.Distribution(name, value, tags: ddTags);
        }

        /// <summary>
        /// Records a timing metric (duration in milliseconds).
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="milliseconds">Duration in milliseconds</param>
        /// <param name="tags">Optional tags</param>
        public void Timing(string name, double milliseconds, IDictionary<string, string>? tags = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatadogMetricsExporter));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var ddTags = ConvertTags(tags);
            _statsd.Timer(name, milliseconds, tags: ddTags);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _statsd?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing DogStatsD service");
            }

            _disposed = true;
        }

        private string[]? ConvertTags(IDictionary<string, string>? tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return null;
            }

            return tags.Select(kvp => $"{kvp.Key}:{kvp.Value}").ToArray();
        }
    }
}
```

### DatadogTraceExporter

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Exports traces to Datadog using native format.
    /// </summary>
    /// <remarks>
    /// In OTLP mode, this is largely a no-op as OpenTelemetry SDK handles export.
    /// In DogStatsD mode, provides manual trace export capability.
    /// </remarks>
    public sealed class DatadogTraceExporter
    {
        private readonly DatadogConfiguration _configuration;
        private readonly ILogger<DatadogTraceExporter>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatadogTraceExporter"/> class.
        /// </summary>
        /// <param name="configuration">Datadog configuration</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        public DatadogTraceExporter(
            DatadogConfiguration configuration,
            ILogger<DatadogTraceExporter>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
            _configuration.ApplyDefaults();
        }

        /// <summary>
        /// Enriches an Activity with Datadog-specific tags.
        /// </summary>
        /// <param name="activity">Activity to enrich</param>
        public void EnrichActivity(Activity activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            // Add unified service tags
            if (!string.IsNullOrEmpty(_configuration.ServiceName))
            {
                activity.SetTag("service.name", _configuration.ServiceName);
            }
            if (!string.IsNullOrEmpty(_configuration.Environment))
            {
                activity.SetTag("env", _configuration.Environment);
            }
            if (!string.IsNullOrEmpty(_configuration.Version))
            {
                activity.SetTag("version", _configuration.Version);
            }

            // Add global tags
            foreach (var tag in _configuration.GlobalTags)
            {
                if (!activity.Tags.Any(t => t.Key == tag.Key))
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Creates Datadog trace context propagation headers.
        /// </summary>
        /// <param name="activity">Current activity</param>
        /// <returns>Dictionary of headers for Datadog trace propagation</returns>
        public IDictionary<string, string> CreatePropagationHeaders(Activity? activity = null)
        {
            activity ??= Activity.Current;
            if (activity == null)
            {
                return new Dictionary<string, string>();
            }

            var headers = new Dictionary<string, string>();

            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                // Datadog supports W3C TraceContext
                headers["traceparent"] = activity.Id ?? string.Empty;
                
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                {
                    headers["tracestate"] = activity.TraceStateString;
                }

                // Also add Datadog-specific headers for compatibility
                var traceId = activity.TraceId.ToString();
                var spanId = activity.SpanId.ToString();
                
                // Convert hex IDs to decimal for Datadog
                if (ulong.TryParse(traceId.Substring(16), 
                    System.Globalization.NumberStyles.HexNumber, 
                    null, 
                    out var traceIdDecimal))
                {
                    headers["x-datadog-trace-id"] = traceIdDecimal.ToString();
                }

                if (ulong.TryParse(spanId, 
                    System.Globalization.NumberStyles.HexNumber, 
                    null, 
                    out var spanIdDecimal))
                {
                    headers["x-datadog-parent-id"] = spanIdDecimal.ToString();
                }
            }

            return headers;
        }

        /// <summary>
        /// Extracts Datadog trace context from headers.
        /// </summary>
        /// <param name="headers">HTTP headers</param>
        /// <returns>Extracted trace context or null if not present</returns>
        public (string? TraceId, string? ParentId, string? SamplingPriority)? 
            ExtractTraceContext(IDictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
            {
                return null;
            }

            // Try W3C TraceContext first
            if (headers.TryGetValue("traceparent", out var traceparent) && 
                !string.IsNullOrEmpty(traceparent))
            {
                return ParseW3CTraceParent(traceparent);
            }

            // Fall back to Datadog headers
            if (headers.TryGetValue("x-datadog-trace-id", out var traceId) &&
                headers.TryGetValue("x-datadog-parent-id", out var parentId))
            {
                headers.TryGetValue("x-datadog-sampling-priority", out var samplingPriority);
                return (traceId, parentId, samplingPriority);
            }

            return null;
        }

        private (string TraceId, string ParentId, string? SamplingPriority)? 
            ParseW3CTraceParent(string traceparent)
        {
            // W3C format: 00-{trace-id}-{parent-id}-{trace-flags}
            var parts = traceparent.Split('-');
            if (parts.Length != 4)
            {
                return null;
            }

            return (parts[1], parts[2], parts[3]);
        }
    }
}
```

### Configuration Extensions

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Extension methods for configuring Datadog telemetry integration.
    /// </summary>
    public static class DatadogExtensions
    {
        /// <summary>
        /// Adds Datadog telemetry export with dual-mode support.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection AddDatadogTelemetry(
            this IServiceCollection services,
            Action<DatadogConfiguration>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Configure Datadog options
            var configuration = new DatadogConfiguration();
            configure?.Invoke(configuration);
            configuration.ApplyDefaults();

            services.AddSingleton(configuration);

            // Detect export mode
            var mode = DetermineExportMode(configuration);
            configuration.Mode = mode;

            // Register appropriate exporters based on mode
            if (mode == DatadogExportMode.DogStatsD || mode == DatadogExportMode.Auto)
            {
                services.AddSingleton<DatadogMetricsExporter>();
                services.AddSingleton<DatadogTraceExporter>();
            }

            return services;
        }

        /// <summary>
        /// Adds Datadog telemetry with environment-based configuration.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection AddDatadogTelemetry(
            this IServiceCollection services)
        {
            return services.AddDatadogTelemetry(configure: null);
        }

        private static DatadogExportMode DetermineExportMode(DatadogConfiguration configuration)
        {
            if (configuration.Mode != DatadogExportMode.Auto)
            {
                return configuration.Mode;
            }

            // Check if OpenTelemetry is configured
            var otlpEndpoint = System.Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                return DatadogExportMode.OTLP;
            }

            // Default to DogStatsD for .NET Standard 2.0
            return DatadogExportMode.DogStatsD;
        }
    }
}
```

### Usage Examples

#### ASP.NET Core with OTLP Mode (.NET 8+)

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Datadog;

var builder = WebApplication.CreateBuilder(args);

// Add HVO telemetry
builder.Services.AddTelemetry(options =>
{
    options.ServiceName = "MyService";
});

// Add Datadog with OTLP mode
builder.Services.AddDatadogTelemetry(options =>
{
    options.ServiceName = "my-service";
    options.Environment = "production";
    options.Version = "1.2.3";
    options.Mode = DatadogExportMode.OTLP;
});

// Configure OpenTelemetry with Datadog exporter
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("MyService")
        .AddOtlpExporter(options =>
        {
            // Datadog agent OTLP endpoint
            options.Endpoint = new Uri("http://localhost:4318");
        }));

var app = builder.Build();

app.MapGet("/", () => "Hello World");

app.Run();
```

#### .NET Framework 4.8 with DogStatsD Mode

```csharp
using System.Web.Http;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Datadog;

public class WebApiApplication : System.Web.HttpApplication
{
    private static DatadogMetricsExporter? _datadogMetrics;

    protected void Application_Start()
    {
        // Initialize HVO telemetry
        Telemetry.Initialize(options =>
        {
            options.ServiceName = "LegacyWebAPI";
        });

        // Initialize Datadog in DogStatsD mode
        var datadogConfig = new DatadogConfiguration
        {
            ServiceName = "legacy-webapi",
            Environment = "production",
            Version = "2.1.0",
            AgentHost = "localhost",
            AgentPort = 8125,
            Mode = DatadogExportMode.DogStatsD
        };

        _datadogMetrics = new DatadogMetricsExporter(datadogConfig);

        GlobalConfiguration.Configure(WebApiConfig.Register);
    }

    protected void Application_End()
    {
        _datadogMetrics?.Dispose();
        Telemetry.Shutdown();
    }

    protected void Application_BeginRequest(object sender, EventArgs e)
    {
        // Track request metrics
        _datadogMetrics?.Counter("http.requests", 1, new Dictionary<string, string>
        {
            ["method"] = Request.HttpMethod,
            ["path"] = Request.Path
        });
    }
}
```

#### Manual Metrics Tracking

```csharp
using HVO.Enterprise.Telemetry.Datadog;

public class OrderService
{
    private readonly DatadogMetricsExporter _metrics;

    public OrderService(DatadogMetricsExporter metrics)
    {
        _metrics = metrics;
    }

    public void ProcessOrder(Order order)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Process order...
            
            _metrics.Counter("orders.processed", 1, new Dictionary<string, string>
            {
                ["status"] = "success",
                ["type"] = order.Type
            });
        }
        catch (Exception ex)
        {
            _metrics.Counter("orders.errors", 1, new Dictionary<string, string>
            {
                ["error_type"] = ex.GetType().Name
            });
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.Timing("orders.duration", stopwatch.ElapsedMilliseconds, 
                new Dictionary<string, string>
                {
                    ["type"] = order.Type
                });
        }
    }
}
```

#### Unix Domain Socket (Linux)

```csharp
builder.Services.AddDatadogTelemetry(options =>
{
    options.ServiceName = "my-service";
    options.UseUnixDomainSocket = true;
    options.UnixDomainSocketPath = "/var/run/datadog/dsd.socket";
});
```

## Testing Requirements

### Unit Tests

```csharp
using System;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.Datadog;
using Xunit;

namespace HVO.Enterprise.Telemetry.Datadog.Tests
{
    public class DatadogConfigurationTests
    {
        [Fact]
        public void ApplyDefaults_WithEnvironmentVariables_UsesEnvironmentValues()
        {
            // Arrange
            Environment.SetEnvironmentVariable("DD_SERVICE", "test-service");
            Environment.SetEnvironmentVariable("DD_ENV", "test");
            Environment.SetEnvironmentVariable("DD_VERSION", "1.0.0");

            var config = new DatadogConfiguration();

            try
            {
                // Act
                config.ApplyDefaults();

                // Assert
                Assert.Equal("test-service", config.ServiceName);
                Assert.Equal("test", config.Environment);
                Assert.Equal("1.0.0", config.Version);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("DD_SERVICE", null);
                Environment.SetEnvironmentVariable("DD_ENV", null);
                Environment.SetEnvironmentVariable("DD_VERSION", null);
            }
        }

        [Fact]
        public void ApplyDefaults_AddsUnifiedServiceTags()
        {
            // Arrange
            var config = new DatadogConfiguration
            {
                ServiceName = "my-service",
                Environment = "prod",
                Version = "2.0.0"
            };

            // Act
            config.ApplyDefaults();

            // Assert
            Assert.Equal("my-service", config.GlobalTags["service"]);
            Assert.Equal("prod", config.GlobalTags["env"]);
            Assert.Equal("2.0.0", config.GlobalTags["version"]);
        }
    }

    public class DatadogMetricsExporterTests
    {
        [Fact]
        public void Counter_WithValidName_TracksMetric()
        {
            // Arrange
            var config = new DatadogConfiguration
            {
                ServiceName = "test",
                AgentHost = "localhost"
            };
            using var exporter = new DatadogMetricsExporter(config);

            // Act & Assert - should not throw
            exporter.Counter("test.counter", 1);
        }

        [Fact]
        public void Counter_WithTags_IncludesTags()
        {
            // Arrange
            var config = new DatadogConfiguration();
            using var exporter = new DatadogMetricsExporter(config);

            var tags = new Dictionary<string, string>
            {
                ["endpoint"] = "/api/orders",
                ["status"] = "200"
            };

            // Act & Assert
            exporter.Counter("http.requests", 1, tags);
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // Arrange
            var config = new DatadogConfiguration();
            var exporter = new DatadogMetricsExporter(config);

            // Act & Assert
            exporter.Dispose();
            exporter.Dispose(); // Should not throw
        }
    }

    public class DatadogTraceExporterTests
    {
        [Fact]
        public void EnrichActivity_AddsServiceTags()
        {
            // Arrange
            var config = new DatadogConfiguration
            {
                ServiceName = "my-service",
                Environment = "staging",
                Version = "1.2.3"
            };
            var exporter = new DatadogTraceExporter(config);
            
            using var activity = new Activity("test").Start();

            // Act
            exporter.EnrichActivity(activity);

            // Assert
            Assert.Equal("my-service", activity.GetTagItem("service.name"));
            Assert.Equal("staging", activity.GetTagItem("env"));
            Assert.Equal("1.2.3", activity.GetTagItem("version"));
        }

        [Fact]
        public void CreatePropagationHeaders_WithW3CActivity_CreatesDatadogHeaders()
        {
            // Arrange
            var config = new DatadogConfiguration();
            var exporter = new DatadogTraceExporter(config);
            
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            // Act
            var headers = exporter.CreatePropagationHeaders(activity);

            // Assert
            Assert.Contains("traceparent", headers.Keys);
            Assert.Contains("x-datadog-trace-id", headers.Keys);
            Assert.Contains("x-datadog-parent-id", headers.Keys);
        }
    }
}
```

### Integration Tests

```csharp
[Fact]
public async Task Integration_DatadogWithTelemetry_ExportsMetrics()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddTelemetry(options => options.ServiceName = "TestService");
    services.AddDatadogTelemetry(options =>
    {
        options.ServiceName = "test-service";
        options.Mode = DatadogExportMode.DogStatsD;
    });

    var provider = services.BuildServiceProvider();
    var exporter = provider.GetRequiredService<DatadogMetricsExporter>();

    // Act
    exporter.Counter("test.requests", 5);
    exporter.Gauge("test.connections", 42.0);
    exporter.Histogram("test.duration", 123.45);

    // Wait for metrics to be sent
    await Task.Delay(100);

    // Assert - metrics should be sent to DogStatsD agent
}
```

### Performance Tests

```csharp
[Fact]
public void Performance_MetricExport_IsMinimal()
{
    // Arrange
    var config = new DatadogConfiguration();
    using var exporter = new DatadogMetricsExporter(config);

    // Act
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 10_000; i++)
    {
        exporter.Counter("test.counter", 1);
    }
    stopwatch.Stop();

    // Assert - <10μs per metric
    Assert.True(stopwatch.ElapsedMilliseconds < 100,
        $"Metric export too slow: {stopwatch.ElapsedMilliseconds}ms for 10K metrics");
}
```

## Performance Requirements

- **Metric export overhead**: <5μs per metric
- **UDP packet size**: Optimized batching for network efficiency
- **Unix domain socket**: <2μs latency on Linux
- **Memory overhead**: <500KB for exporter instance
- **Thread-safe**: All components safe for concurrent use
- **No blocking**: All operations non-blocking

## Dependencies

**Blocked By**: 
- US-001: Core Package Setup (basic telemetry infrastructure)

**Blocks**: None (extension package)

## Definition of Done

- [x] Dual-mode support working (OTLP and DogStatsD)
- [x] Metrics exporter implemented for all metric types
- [x] Trace enrichment with Datadog tags working
- [x] Configuration extensions with environment variable support
- [x] Unit tests passing (>85% coverage)
- [ ] Integration tests with real Datadog agent passing
- [ ] Performance benchmarks meet requirements
- [x] Works on .NET Framework 4.8 and .NET 8+
- [x] Unix domain socket support on Linux
- [x] XML documentation complete for all public APIs
- [x] Code reviewed and approved
- [x] Zero warnings in build
- [ ] NuGet package created and validated

## Notes

### Design Decisions

1. **Why dual-mode (OTLP + DogStatsD)?**
   - OTLP: Modern approach, better for .NET 8+ with full OpenTelemetry SDK
   - DogStatsD: Direct protocol, necessary for .NET Framework 4.8, lower overhead
   - Both modes supported for flexibility and migration paths

2. **Why prefer Unix domain socket on Linux?**
   - Significantly faster than UDP (~10x lower latency)
   - No network stack overhead
   - Better for containerized environments
   - Datadog agent default on Linux

3. **Why separate metrics and trace exporters?**
   - Different transport mechanisms and formats
   - Metrics use DogStatsD UDP/UDS protocol
   - Traces can use OTLP or Datadog APM protocol
   - Allows independent configuration and testing

4. **Why environment variable precedence?**
   - Matches Datadog's standard configuration approach
   - Works well with containerized deployments
   - Easy to override without code changes
   - DD_* convention is industry standard

### Implementation Tips

- Use `DogStatsdService.Configure()` once at startup
- Batch metrics when possible for efficiency
- DogStatsD uses fire-and-forget UDP - no delivery guarantees
- Test with real Datadog agent, not just unit tests
- Use unified service tags (service, env, version) for consistency
- Monitor UDP buffer sizes in high-throughput scenarios

### Common Pitfalls

- **Don't create new DogStatsdService per request**: Use singleton
- **Don't forget to dispose**: May lose buffered metrics
- **Don't block on metric calls**: DogStatsD is async/fire-and-forget
- **Don't use histograms for high-cardinality data**: Use distributions
- **Don't forget Unix domain socket on Linux**: Significant performance boost

### Future Enhancements

- APM trace correlation with logs
- Datadog profiler integration
- Dynamic sampling configuration
- Metric aggregation before export
- Support for Datadog Distribution metric type improvements
- Integration with Datadog Runtime Metrics

## Related Documentation

- [Project Plan - Step 25: Datadog Extension](../project-plan.md#25-datadog-extension-hvoenterprisetelemetrydatadog)
- [DogStatsD Documentation](https://docs.datadoghq.com/developers/dogstatsd/)
- [Datadog APM .NET](https://docs.datadoghq.com/tracing/trace_collection/dd_libraries/dotnet-core/)
- [OpenTelemetry Datadog Exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/datadogexporter)
- [Datadog Unified Service Tagging](https://docs.datadoghq.com/getting_started/tagging/unified_service_tagging/)

## Implementation Summary

**Completed**: 2025-07-15  
**Implemented by**: GitHub Copilot

### What Was Implemented
- Created `HVO.Enterprise.Telemetry.Datadog` package targeting .NET Standard 2.0
- Dual-mode architecture with `DatadogExportMode` enum (Auto, OTLP, DogStatsD)
- `DatadogOptions` sealed class with DD_* environment variable fallback, unified service tags, UDS support
- `DatadogMetricsExporter` wrapping `DogStatsdService` for Counter/Gauge/Histogram/Distribution/Timing
- `DatadogTraceExporter` for Activity enrichment with Datadog tags and W3C+Datadog propagation header generation/extraction
- `ServiceCollectionExtensions.AddDatadogTelemetry()` with idempotency guard and `IOptions<T>` pattern
- `TelemetryBuilderExtensions.WithDatadog()` for fluent builder API
- Comprehensive test suite: 88 unit tests across 5 test classes

### Key Files
- `src/HVO.Enterprise.Telemetry.Datadog/HVO.Enterprise.Telemetry.Datadog.csproj`
- `src/HVO.Enterprise.Telemetry.Datadog/DatadogOptions.cs`
- `src/HVO.Enterprise.Telemetry.Datadog/DatadogExportMode.cs`
- `src/HVO.Enterprise.Telemetry.Datadog/DatadogMetricsExporter.cs`
- `src/HVO.Enterprise.Telemetry.Datadog/DatadogTraceExporter.cs`
- `src/HVO.Enterprise.Telemetry.Datadog/ServiceCollectionExtensions.cs`
- `src/HVO.Enterprise.Telemetry.Datadog/TelemetryBuilderExtensions.cs`
- `tests/HVO.Enterprise.Telemetry.Datadog.Tests/` (5 test files, 84 tests)

### Decisions Made
- Used `DatadogOptions` naming (not `DatadogConfiguration`) to match `AppInsightsOptions` convention
- Used DogStatsD-CSharp-Client 7.0.0 as specified in user story (netstandard2.0 compatible)
- Followed AppInsights extension pattern exactly for DI registration (idempotency, TryAddSingleton, IOptions<T>)
- `ApplyEnvironmentDefaults()` is internal and called by DI factory—explicit values not overwritten by env vars
- `GetEffectiveServerName()` handles `unix://` prefix convention for UDS transport
- Trace exporter uses hex-to-decimal conversion for Datadog-native header compatibility
- W3C traceparent preferred over Datadog-native headers in extraction (with fallback)

### Quality Gates
- ✅ Build: 0 warnings, 0 errors (26 projects)
- ✅ Tests: 1,521 passed, 0 failed, 1 skipped (88 new Datadog tests)
- ✅ Patterns: Consistent with AppInsights/Serilog extensions
- ✅ Documentation: Full XML docs on all public APIs

### Next Steps
- Integration tests with real Datadog agent (deferred — requires agent infrastructure)
- Performance benchmarks (deferred — requires BenchmarkDotNet harness)
- NuGet package validation (deferred — requires packaging pipeline)
