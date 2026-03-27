# US-034: Seq Structured Log Extension Package

**GitHub Issue**: [#81](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/81)  
**Status**: ❌ Not Started  
**Category**: Extension Package  
**Effort**: 3 story points  
**Sprint**: 12

## Description

As a **developer using Seq for structured log search and analysis**,  
I want **automatic telemetry context enrichment and direct Seq ingestion from HVO telemetry**,  
So that **my structured logs in Seq are correlated with distributed traces and I can search by CorrelationId, TraceId, and custom telemetry properties without manual configuration**.

## Background

[Seq](https://datalust.co/seq) by Datalust is one of the most popular structured log servers in the
.NET ecosystem. It excels at searching, filtering, and dashboarding structured log events. Many
enterprise teams already run Seq, and it is a natural complement to HVO's telemetry — especially for
teams that use Serilog (which HVO already supports via US-023).

This extension provides two integration paths:

1. **Serilog Sink Helper** — A convenience method that configures `Serilog.Sinks.Seq` with HVO
   enrichers pre-wired (CorrelationId, TraceId, SpanId, operation context). This is the recommended
   path for teams already using Serilog.

2. **Direct HTTP Ingestion** — A lightweight `SeqLogExporter` that posts structured log events
   directly to Seq's CLEF (Compact Log Event Format) HTTP ingestion API. This path works without
   Serilog for teams using only `Microsoft.Extensions.Logging.ILogger`.

Both paths ensure that HVO correlation and tracing context flows into Seq automatically.

## Acceptance Criteria

1. **Package Structure**
   - [ ] `HVO.Enterprise.Telemetry.Seq.csproj` created targeting `netstandard2.0`
   - [ ] Package builds with zero warnings
   - [ ] Minimal dependencies: `HVO.Enterprise.Telemetry`, optional `Serilog.Sinks.Seq`

2. **Serilog Sink Helper**
   - [ ] `LoggerConfiguration.WriteTo.SeqWithTelemetry()` extension method
   - [ ] Automatically adds HVO `ActivityEnricher` and `CorrelationEnricher`
   - [ ] Configures Seq sink with server URL and optional API key
   - [ ] Supports compact JSON formatting (CLEF) by default
   - [ ] Configurable minimum log level for Seq output

3. **Direct HTTP Ingestion (ILogger path)**
   - [ ] `SeqLogExporter` posts CLEF events to Seq HTTP ingest API (`/api/events/raw`)
   - [ ] Enriches each event with `CorrelationId`, `TraceId`, `SpanId` from current context
   - [ ] Batches events for efficient HTTP posting
   - [ ] Configurable batch size, flush interval, and retry policy
   - [ ] Implements `IDisposable` for clean shutdown with buffered event flush

4. **Configuration Extensions**
   - [ ] `IServiceCollection.AddSeqTelemetry()` extension method
   - [ ] `TelemetryBuilder.WithSeq()` fluent API
   - [ ] `IOptions<SeqOptions>` pattern consistent with other extensions
   - [ ] Environment variable fallback (`SEQ_SERVER_URL`, `SEQ_API_KEY`)
   - [ ] Idempotency guard

5. **Seq API Key / Authentication**
   - [ ] Support Seq API key for authenticated ingestion
   - [ ] Support Seq API key per-application for multi-tenant Seq servers
   - [ ] API key passed as `X-Seq-ApiKey` header

6. **Cross-Platform Support**
   - [ ] Works on .NET Framework 4.8
   - [ ] Works on .NET 8+
   - [ ] HTTP ingestion uses `System.Net.Http.HttpClient` (netstandard2.0 compatible)

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
    
    <PackageId>HVO.Enterprise.Telemetry.Seq</PackageId>
    <Version>1.0.0-preview.1</Version>
    <Authors>HVO Enterprise</Authors>
    <Description>Seq structured log integration for HVO.Enterprise.Telemetry — enriched log shipping via Serilog sink or direct HTTP ingestion</Description>
    <PackageTags>telemetry;seq;logging;structured-logging;correlation;tracing;datalust</PackageTags>
    <RepositoryUrl>https://github.com/RoySalisbury/HVO.Enterprise.Telemetry</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Serilog.Sinks.Seq" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
    <ProjectReference Include="..\HVO.Enterprise.Telemetry\HVO.Enterprise.Telemetry.csproj" />
    <ProjectReference Include="..\HVO.Enterprise.Telemetry.Serilog\HVO.Enterprise.Telemetry.Serilog.csproj" />
  </ItemGroup>
</Project>
```

### SeqOptions

```csharp
using System;

namespace HVO.Enterprise.Telemetry.Seq
{
    /// <summary>
    /// Configuration options for Seq structured log integration.
    /// </summary>
    /// <remarks>
    /// Supports environment-variable fallbacks: <c>SEQ_SERVER_URL</c>, <c>SEQ_API_KEY</c>.
    /// </remarks>
    public sealed class SeqOptions
    {
        /// <summary>
        /// Gets or sets the Seq server URL.
        /// Falls back to <c>SEQ_SERVER_URL</c> environment variable.
        /// Default: <c>"http://localhost:5341"</c>.
        /// </summary>
        public string ServerUrl { get; set; } = "http://localhost:5341";

        /// <summary>
        /// Gets or sets the Seq API key for authenticated ingestion.
        /// Falls back to <c>SEQ_API_KEY</c> environment variable.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Serilog sink integration is enabled.
        /// When <see langword="true"/>, configures <c>Serilog.Sinks.Seq</c> with HVO enrichers.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableSerilogSink { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether direct HTTP ingestion is enabled.
        /// When <see langword="true"/>, registers <see cref="SeqLogExporter"/> for CLEF HTTP posting.
        /// Default: <see langword="false"/> (opt-in — use Serilog sink by default).
        /// </summary>
        public bool EnableDirectIngestion { get; set; }

        /// <summary>
        /// Gets or sets the batch size for direct HTTP ingestion.
        /// Default: 100 events per batch.
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the flush interval for batched events.
        /// Default: 2 seconds.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets or sets the minimum log level for Seq output.
        /// Default: <c>"Information"</c>.
        /// </summary>
        public string MinimumLevel { get; set; } = "Information";

        /// <summary>
        /// Gets or sets the HTTP timeout for Seq API calls.
        /// Default: 10 seconds.
        /// </summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Applies environment variable defaults. Explicit values take precedence.
        /// </summary>
        internal void ApplyEnvironmentDefaults()
        {
            var serverUrl = System.Environment.GetEnvironmentVariable("SEQ_SERVER_URL");
            if (!string.IsNullOrEmpty(serverUrl) && ServerUrl == "http://localhost:5341")
            {
                ServerUrl = serverUrl;
            }

            ApiKey ??= System.Environment.GetEnvironmentVariable("SEQ_API_KEY");
        }
    }
}
```

### SeqLogExporter (Direct HTTP Ingestion)

```csharp
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Seq
{
    /// <summary>
    /// Exports structured log events to Seq via its CLEF HTTP ingestion API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Posts events to <c>/api/events/raw</c> in Compact Log Event Format (CLEF).
    /// Events are batched and sent on a configurable interval for efficiency.
    /// </para>
    /// <para>
    /// Automatically enriches each event with <c>CorrelationId</c>, <c>TraceId</c>,
    /// and <c>SpanId</c> from the current telemetry context.
    /// </para>
    /// <para>Thread-safe. Register as singleton.</para>
    /// </remarks>
    public sealed class SeqLogExporter : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentQueue<string> _buffer;
        private readonly Timer _flushTimer;
        private readonly int _batchSize;
        private readonly ILogger<SeqLogExporter>? _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeqLogExporter"/> class.
        /// </summary>
        /// <param name="options">Seq configuration options.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public SeqLogExporter(SeqOptions options, ILogger<SeqLogExporter>? logger = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _logger = logger;
            _batchSize = options.BatchSize;
            _buffer = new ConcurrentQueue<string>();

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.ServerUrl),
                Timeout = options.HttpTimeout
            };

            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Seq-ApiKey", options.ApiKey);
            }

            _flushTimer = new Timer(
                _ => FlushAsync().ConfigureAwait(false),
                null,
                options.FlushInterval,
                options.FlushInterval);
        }

        /// <summary>
        /// Enqueues a structured log event with current telemetry context.
        /// </summary>
        /// <param name="level">Log level.</param>
        /// <param name="messageTemplate">Serilog-style message template.</param>
        /// <param name="properties">Additional structured properties.</param>
        public void Enqueue(string level, string messageTemplate,
            params (string Key, object? Value)[] properties)
        {
            if (_disposed) return;

            var sb = new StringBuilder();
            sb.Append("{\"@t\":\"");
            sb.Append(DateTimeOffset.UtcNow.ToString("O"));
            sb.Append("\",\"@l\":\"");
            sb.Append(level);
            sb.Append("\",\"@mt\":\"");
            sb.Append(EscapeJson(messageTemplate));
            sb.Append("\"");

            // Enrich with telemetry context
            var correlationId = CorrelationContext.Current;
            if (!string.IsNullOrEmpty(correlationId))
            {
                sb.Append(",\"CorrelationId\":\"");
                sb.Append(EscapeJson(correlationId));
                sb.Append("\"");
            }

            var activity = Activity.Current;
            if (activity != null)
            {
                sb.Append(",\"TraceId\":\"");
                sb.Append(activity.TraceId.ToString());
                sb.Append("\",\"SpanId\":\"");
                sb.Append(activity.SpanId.ToString());
                sb.Append("\"");
            }

            foreach (var (key, value) in properties)
            {
                sb.Append(",\"");
                sb.Append(EscapeJson(key));
                sb.Append("\":");
                if (value is string s)
                {
                    sb.Append("\"");
                    sb.Append(EscapeJson(s));
                    sb.Append("\"");
                }
                else
                {
                    sb.Append(value?.ToString() ?? "null");
                }
            }

            sb.Append("}");
            _buffer.Enqueue(sb.ToString());
        }

        /// <summary>
        /// Flushes all buffered events to Seq.
        /// </summary>
        public async Task FlushAsync()
        {
            if (_buffer.IsEmpty) return;

            var batch = new StringBuilder();
            var count = 0;

            while (count < _batchSize && _buffer.TryDequeue(out var evt))
            {
                batch.AppendLine(evt);
                count++;
            }

            if (count == 0) return;

            try
            {
                var content = new StringContent(
                    batch.ToString(), Encoding.UTF8, "application/vnd.serilog.clef");
                var response = await _httpClient.PostAsync("/api/events/raw", content)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to flush {Count} events to Seq", count);
            }
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

        private static string EscapeJson(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
```

### Serilog Sink Helper Extension

```csharp
using System;
using Serilog;
using Serilog.Configuration;
using HVO.Enterprise.Telemetry.Serilog;

namespace HVO.Enterprise.Telemetry.Seq
{
    /// <summary>
    /// Extension methods for configuring Seq with HVO telemetry enrichment via Serilog.
    /// </summary>
    public static class SeqSerilogExtensions
    {
        /// <summary>
        /// Adds a Seq sink with HVO telemetry enrichers pre-wired.
        /// Equivalent to <c>.WriteTo.Seq(...).Enrich.WithTelemetry()</c>.
        /// </summary>
        /// <param name="writeTo">The Serilog WriteTo configuration.</param>
        /// <param name="serverUrl">The Seq server URL.</param>
        /// <param name="apiKey">Optional Seq API key.</param>
        /// <returns>The logger configuration for chaining.</returns>
        public static LoggerConfiguration SeqWithTelemetry(
            this LoggerSinkConfiguration writeTo,
            string serverUrl,
            string? apiKey = null)
        {
            if (writeTo == null)
                throw new ArgumentNullException(nameof(writeTo));
            if (string.IsNullOrEmpty(serverUrl))
                throw new ArgumentException("Server URL is required.", nameof(serverUrl));

            // This returns the root LoggerConfiguration
            // The caller chains: Log.Logger = new LoggerConfiguration()
            //     .Enrich.WithTelemetry()
            //     .WriteTo.SeqWithTelemetry("http://localhost:5341")
            //     .CreateLogger();

            return writeTo.Seq(serverUrl, apiKey: apiKey);
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

namespace HVO.Enterprise.Telemetry.Seq
{
    /// <summary>
    /// Extension methods for registering Seq telemetry integration with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Seq structured log integration with HVO telemetry enrichment.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="SeqOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// This method is idempotent — calling it multiple times will not add duplicate registrations.
        /// </remarks>
        public static IServiceCollection AddSeqTelemetry(
            this IServiceCollection services,
            Action<SeqOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (services.Any(s => s.ServiceType == typeof(SeqLogExporter)))
                return services;

            var optionsBuilder = services.AddOptions<SeqOptions>();
            if (configure != null)
                optionsBuilder.Configure(configure);

            services.TryAddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<SeqOptions>>().Value;
                options.ApplyEnvironmentDefaults();
                var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<SeqLogExporter>();
                return new SeqLogExporter(options, logger);
            });

            return services;
        }
    }
}
```

### TelemetryBuilderExtensions

```csharp
using System;

namespace HVO.Enterprise.Telemetry.Seq
{
    /// <summary>
    /// Extension methods for integrating Seq with the <see cref="TelemetryBuilder"/> fluent API.
    /// </summary>
    public static class TelemetryBuilderExtensions
    {
        /// <summary>
        /// Adds Seq structured log integration to the telemetry builder.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="configure">Optional delegate to configure <see cref="SeqOptions"/>.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddTelemetry(builder =>
        /// {
        ///     builder.WithSeq(options =>
        ///     {
        ///         options.ServerUrl = "http://seq-server:5341";
        ///         options.ApiKey = "your-api-key";
        ///     });
        /// });
        /// </code>
        /// </example>
        public static TelemetryBuilder WithSeq(
            this TelemetryBuilder builder,
            Action<SeqOptions>? configure = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddSeqTelemetry(configure);
            return builder;
        }

        /// <summary>
        /// Adds Seq integration with a server URL.
        /// </summary>
        /// <param name="builder">The telemetry builder.</param>
        /// <param name="serverUrl">The Seq server URL.</param>
        /// <param name="apiKey">Optional Seq API key.</param>
        /// <returns>The telemetry builder for chaining.</returns>
        public static TelemetryBuilder WithSeq(
            this TelemetryBuilder builder,
            string serverUrl,
            string? apiKey = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrEmpty(serverUrl))
                throw new ArgumentException("Server URL is required.", nameof(serverUrl));

            return builder.WithSeq(options =>
            {
                options.ServerUrl = serverUrl;
                options.ApiKey = apiKey;
            });
        }
    }
}
```

## Sample Application Updates

### ServiceConfiguration.cs

```csharp
// Seq structured log server — enriched log shipping
if (configuration.GetValue<bool>("Extensions:Seq:Enabled"))
{
    services.AddSeqTelemetry(options =>
    {
        options.ServerUrl = configuration["Extensions:Seq:ServerUrl"] ?? "http://localhost:5341";
        options.ApiKey = configuration["Extensions:Seq:ApiKey"];
        options.EnableDirectIngestion = configuration.GetValue<bool>("Extensions:Seq:EnableDirectIngestion");
    });
}
```

### appsettings.json Section

```jsonc
{
  "Extensions": {
    "Seq": {
      "Enabled": false,
      "ServerUrl": "http://localhost:5341",
      "ApiKey": "",
      "MinimumLevel": "Information",
      "EnableDirectIngestion": false,
      "BatchSize": 100,
      "FlushIntervalSeconds": 2
    }
  }
}
```

## Testing Requirements

### Unit Tests

```csharp
[TestClass]
public class SeqOptionsTests
{
    [TestMethod]
    public void Defaults_AreCorrect()
    {
        var options = new SeqOptions();

        Assert.AreEqual("http://localhost:5341", options.ServerUrl);
        Assert.IsNull(options.ApiKey);
        Assert.IsTrue(options.EnableSerilogSink);
        Assert.IsFalse(options.EnableDirectIngestion);
        Assert.AreEqual(100, options.BatchSize);
        Assert.AreEqual(TimeSpan.FromSeconds(2), options.FlushInterval);
    }

    [TestMethod]
    public void ApplyEnvironmentDefaults_SetsServerUrlFromEnv()
    {
        var options = new SeqOptions();
        System.Environment.SetEnvironmentVariable("SEQ_SERVER_URL", "http://seq:5341");

        options.ApplyEnvironmentDefaults();

        Assert.AreEqual("http://seq:5341", options.ServerUrl);
        System.Environment.SetEnvironmentVariable("SEQ_SERVER_URL", null);
    }

    [TestMethod]
    public void ApplyEnvironmentDefaults_SetsApiKeyFromEnv()
    {
        var options = new SeqOptions();
        System.Environment.SetEnvironmentVariable("SEQ_API_KEY", "test-key-123");

        options.ApplyEnvironmentDefaults();

        Assert.AreEqual("test-key-123", options.ApiKey);
        System.Environment.SetEnvironmentVariable("SEQ_API_KEY", null);
    }

    [TestMethod]
    public void ApplyEnvironmentDefaults_ExplicitValueTakesPrecedence()
    {
        var options = new SeqOptions { ServerUrl = "http://custom:5341" };
        System.Environment.SetEnvironmentVariable("SEQ_SERVER_URL", "http://env:5341");

        options.ApplyEnvironmentDefaults();

        Assert.AreEqual("http://custom:5341", options.ServerUrl);
        System.Environment.SetEnvironmentVariable("SEQ_SERVER_URL", null);
    }
}

[TestClass]
public class SeqLogExporterTests
{
    [TestMethod]
    public void Constructor_NullOptions_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new SeqLogExporter(null!));
    }

    [TestMethod]
    public void Enqueue_AddsEventToBuffer()
    {
        var options = new SeqOptions { ServerUrl = "http://localhost:5341" };
        using var exporter = new SeqLogExporter(options);

        exporter.Enqueue("Information", "Test event {Name}", ("Name", "World"));

        // Buffer should have one event (internal state — verify indirectly via flush)
    }

    [TestMethod]
    public void Dispose_FlushesRemainingEvents()
    {
        var options = new SeqOptions
        {
            ServerUrl = "http://localhost:5341",
            FlushInterval = TimeSpan.FromMinutes(10) // long interval so manual flush on dispose
        };
        using var exporter = new SeqLogExporter(options);

        exporter.Enqueue("Information", "Test event");
        // Dispose will attempt to flush — should not throw even if Seq is unavailable
    }
}

[TestClass]
public class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddSeqTelemetry_NullServices_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddSeqTelemetry());
    }

    [TestMethod]
    public void AddSeqTelemetry_Idempotent()
    {
        var services = new ServiceCollection();
        services.AddSeqTelemetry();
        services.AddSeqTelemetry();

        var exporterCount = services.Count(s => s.ServiceType == typeof(SeqLogExporter));
        Assert.AreEqual(1, exporterCount);
    }

    [TestMethod]
    public void AddSeqTelemetry_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddSeqTelemetry(options =>
        {
            options.ServerUrl = "http://seq:5341";
            options.ApiKey = "test-key";
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<SeqOptions>>().Value;

        Assert.AreEqual("http://seq:5341", opts.ServerUrl);
        Assert.AreEqual("test-key", opts.ApiKey);
    }
}

[TestClass]
public class TelemetryBuilderExtensionsTests
{
    [TestMethod]
    public void WithSeq_NullBuilder_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            ((TelemetryBuilder)null!).WithSeq());
    }

    [TestMethod]
    public void WithSeq_RegistersExporter()
    {
        var services = new ServiceCollection();
        services.AddTelemetry(builder => builder.WithSeq());

        Assert.IsTrue(services.Any(s => s.ServiceType == typeof(SeqLogExporter)));
    }

    [TestMethod]
    public void WithSeq_UrlOverload_SetsServerUrl()
    {
        var services = new ServiceCollection();
        services.AddTelemetry(builder =>
            builder.WithSeq("http://seq:5341", "api-key-123"));

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<SeqOptions>>().Value;

        Assert.AreEqual("http://seq:5341", opts.ServerUrl);
        Assert.AreEqual("api-key-123", opts.ApiKey);
    }
}
```

## Performance Requirements

- **Enqueue overhead**: <100ns per event (non-blocking buffer append)
- **HTTP batch post**: Async, non-blocking, <50ms for 100-event batch
- **Memory**: <2MB for default buffer settings
- **Serilog sink path**: Zero additional overhead beyond Serilog's own Seq sink

## Dependencies

**Blocked By**:
- US-001: Core Package Setup ✅
- US-002: Auto-Managed Correlation ✅
- US-023: Serilog Extension ✅ (for Serilog sink helper)

**Enhances**:
- US-023: Serilog Extension (adds Seq sink convenience method)
- US-013: ILogger Enrichment (direct ingestion enriches with HVO context)

**Optional Enhancement From**:
- US-033: OpenTelemetry/OTLP (Seq supports OTLP ingest — logs can also flow via OTLP)

## Definition of Done

- [ ] `HVO.Enterprise.Telemetry.Seq.csproj` builds with 0 warnings
- [ ] `SeqOptions` with env var fallback tested
- [ ] `SeqLogExporter` direct HTTP ingestion working and tested
- [ ] `SeqSerilogExtensions.SeqWithTelemetry()` convenience method tested
- [ ] `ServiceCollectionExtensions.AddSeqTelemetry()` idempotent and tested
- [ ] `TelemetryBuilder.WithSeq()` fluent API working
- [ ] All unit tests passing (>90% coverage)
- [ ] Sample app updated with Seq configuration section
- [ ] XML documentation complete on all public APIs
- [ ] Zero warnings in build
- [ ] Code reviewed and approved

## Notes

### Design Decisions

1. **Why two integration paths (Serilog sink + direct)?**
   - Most Seq users already use Serilog — the sink helper is a natural fit
   - Some teams use only `ILogger` without Serilog — direct ingestion covers them
   - Direct ingestion also works as a fallback when Serilog is not available

2. **Why depend on HVO.Enterprise.Telemetry.Serilog?**
   - Reuses existing `ActivityEnricher` and `CorrelationEnricher`
   - Avoids duplicating enricher code
   - Teams already using US-023 get seamless Seq integration

3. **Why CLEF format for direct ingestion?**
   - CLEF is Seq's native compact format — most efficient
   - Well-documented and stable API
   - Supports structured properties natively

4. **Why batched posting?**
   - Reduces HTTP connection overhead
   - Matches Serilog.Sinks.Seq behavior
   - Configurable for different throughput needs

### Seq + OpenTelemetry

Seq (v2024.1+) supports OTLP log ingestion. If teams also install US-033 (OpenTelemetry), they can
route logs to Seq via the OTel Collector's OTLP exporter instead of using this extension directly.
This extension remains valuable for:
- Teams not running an OTel Collector
- Simpler configuration (direct Seq URL vs. collector routing)
- Serilog-native enrichment path
- .NET Framework 4.8 environments where OTel SDK support is limited

## Related Documentation

- [Seq Documentation](https://docs.datalust.co/docs)
- [Seq CLEF Format](https://docs.datalust.co/docs/posting-raw-events)
- [Serilog.Sinks.Seq](https://github.com/datalust/serilog-sinks-seq)
- [US-023: Serilog Extension](./US-023-serilog-extension.md)
- [US-033: OpenTelemetry/OTLP Extension](./US-033-opentelemetry-otlp-extension.md)
