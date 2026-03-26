# Telemetry Configuration Schema

`AddTelemetry(IConfiguration)` binds directly to `TelemetryOptions` (see [TelemetryOptions.cs](../src/HVO.Enterprise.Telemetry/Configuration/TelemetryOptions.cs)). This document explains every supported property so you can confidently author `appsettings.json`, environment variables, or any other configuration source.

## Minimal configuration

```json
{
  "Telemetry": {
    "ServiceName": "MyService",
    "ServiceVersion": "1.0.0",
    "Environment": "Production",
    "Enabled": true,
    "DefaultSamplingRate": 1.0
  }
}
```

> All properties are optional. `ServiceName` defaults to `"Unknown"` but should always be set for meaningful telemetry. Defaults are shown in the tables below.

## Complete JSON shape

```json
{
  "Telemetry": {
    "ServiceName": "MyService",
    "ServiceVersion": "1.0.0",
    "Environment": "Production",
    "Enabled": true,
    "DefaultSamplingRate": 1.0,
    "Sampling": {
      "Contoso.Checkout": {
        "Rate": 0.25,
        "AlwaysSampleErrors": true
      }
    },
    "Logging": {
      "EnableCorrelationEnrichment": true,
      "MinimumLevel": {
        "Default": "Information",
        "Microsoft": "Warning"
      }
    },
    "Metrics": {
      "Enabled": true,
      "CollectionIntervalSeconds": 10
    },
    "Queue": {
      "Capacity": 10000,
      "BatchSize": 100
    },
    "Features": {
      "EnableHttpInstrumentation": true,
      "EnableProxyInstrumentation": true,
      "EnableExceptionTracking": true,
      "EnableParameterCapture": false
    },
    "ActivitySources": [ "MyService", "Contoso.Shared" ],
    "ResourceAttributes": {
      "deployment.environment": "production",
      "service.instance.id": "node-01"
    }
  }
}
```

## Top-level options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServiceName` | string | `"Unknown"` | Logical service/app name used for traces, metrics, and exporters. **Strongly recommended** — defaults to `"Unknown"` if omitted. |
| `ServiceVersion` | string | `null` | Semantic version (e.g., `"1.2.3"`). Forwarded to exporters that support version tags. |
| `Environment` | string | `null` | Deployment environment (`Production`, `Staging`, etc.). |
| `Enabled` | bool | `true` | Global kill switch. When `false`, instrumentation short-circuits. |
| `DefaultSamplingRate` | double (0-1) | `1.0` | Baseline sampling probability for all Activity sources. |
| `Sampling` | object map | `{}` | Per-Activity-source sampling overrides. Keys are source names, values map to [SamplingOptions](#sampling-options). |
| `Logging` | object | see below | Controls log enrichment and minimum levels (see [LoggingOptions](#logging-options)). |
| `Metrics` | object | see below | Enables metrics and sets the polling interval (see [MetricsOptions](#metrics-options)). |
| `Queue` | object | see below | Bounded queue settings for exporter batching (see [QueueOptions](#queue-options)). |
| `Features` | object | see below | Feature toggles for auto-instrumentation (see [FeatureFlags](#feature-flags)). |
| `ActivitySources` | array of string | `["HVO.Enterprise.Telemetry"]` | Activity sources automatically registered with the telemetry service. Include your custom `ActivitySource` names here. |
| `ResourceAttributes` | string/object map | `{}` | Arbitrary tags applied to every span/metric/log (e.g., cloud provider metadata). |

### Sampling options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Rate` | double (0-1) | `1.0` | Sampling probability for the matching Activity source. |
| `AlwaysSampleErrors` | bool | `true` | Forces error Activities to record even when the rate would drop them. |

### Logging options

Defined in [LoggingOptions.cs](../src/HVO.Enterprise.Telemetry/Configuration/LoggingOptions.cs).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableCorrelationEnrichment` | bool | `true` | Injects `TraceId`, `SpanId`, `CorrelationId`, etc. into `ILogger` scopes via `AddTelemetryLoggingEnrichment()`. |
| `MinimumLevel` | map<string,string> | `{}` | Category-specific minimum log levels (e.g., `{ "Microsoft": "Warning" }`). Strings map to `LogLevel` names. |

### Metrics options

Defined in [MetricsOptions.cs](../src/HVO.Enterprise.Telemetry/Configuration/MetricsOptions.cs).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | bool | `true` | Turns built-in metrics collection on/off. |
| `CollectionIntervalSeconds` | int | `10` | Polling interval for EventCounters (.NET Framework) or flush cadence for exporters. Must be positive. |

### Queue options

Defined in [QueueOptions.cs](../src/HVO.Enterprise.Telemetry/Configuration/QueueOptions.cs).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Capacity` | int | `10000` | Maximum queued telemetry payloads before backpressure kicks in. Minimum accepted value is 100. |
| `BatchSize` | int | `100` | Number of items flushed per exporter invocation. Must be between 1 and `Capacity`. |

### Feature flags

Defined in [FeatureFlags.cs](../src/HVO.Enterprise.Telemetry/Configuration/FeatureFlags.cs).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableHttpInstrumentation` | bool | `true` | Enables distributed tracing for `HttpClient`/ASP.NET handlers. |
| `EnableProxyInstrumentation` | bool | `true` | Turns on DispatchProxy-based automatic instrumentation. |
| `EnableExceptionTracking` | bool | `true` | Captures exception fingerprints and aggregates error metrics. |
| `EnableParameterCapture` | bool | `false` | Records method parameter names/values for decorated operations (potentially sensitive). |

## Binding tips

- **Configuration providers**: Any `IConfiguration` source works (JSON, environment variables, Azure App Configuration, etc.).
- **Environment variables** follow the `Telemetry__Queue__Capacity` naming convention (double underscores).
- **Validation**: `TelemetryOptionsValidator` enforces ranges and will throw during startup if values are invalid.
- **Hierarchical configuration**: Use attributes (see `TelemetryConfigurationAttribute`) or the [operation configuration API](../src/HVO.Enterprise.Telemetry/Configuration/OperationConfiguration.cs) for per-type or per-method overrides.

For a step-by-step walkthrough, see [quickstart.md](quickstart.md).

---

## OpenTelemetry Export Configuration (`OtlpExportOptions`)

The `HVO.Enterprise.Telemetry.OpenTelemetry` package is configured separately via `AddOpenTelemetryExport()`. It supports both programmatic configuration and standard OpenTelemetry environment variables.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Endpoint` | string | `"http://localhost:4317"` | OTLP collector endpoint. Env: `OTEL_EXPORTER_OTLP_ENDPOINT`. |
| `Transport` | `OtlpTransport` | `Grpc` | Transport protocol. Auto-detected from port: 4318 → `HttpProtobuf`. |
| `ServiceName` | string? | `null` | Service name for resource attributes. Env: `OTEL_SERVICE_NAME`. |
| `ServiceVersion` | string? | `null` | Service version for resource attributes. |
| `Environment` | string? | `null` | Deployment environment. Env: `OTEL_RESOURCE_ATTRIBUTES` (`deployment.environment`). |
| `EnableTraceExport` | bool | `true` | Enable OTLP trace export. |
| `EnableMetricsExport` | bool | `true` | Enable OTLP metrics export. |
| `EnableLogExport` | bool | `false` | Enable OTLP log export (opt-in). |
| `EnableStandardMeters` | bool | `false` | Register well-known .NET meters (`Microsoft.AspNetCore.Hosting`, `System.Net.Http`, etc.). |
| `EnablePrometheusEndpoint` | bool | `false` | Enable Prometheus scrape endpoint (requires ASP.NET Core). |
| `PrometheusEndpointPath` | string | `"/metrics"` | Prometheus endpoint path. |
| `MetricsExportInterval` | TimeSpan | 60s | Periodic metrics export interval. |
| `TraceBatchExportDelay` | TimeSpan | 5s | Trace batch export scheduled delay. |
| `TraceBatchMaxSize` | int | 512 | Maximum traces per export batch. |
| `TraceBatchMaxQueueSize` | int | 2048 | Maximum queued traces before dropping. |
| `TemporalityPreference` | `MetricsTemporality` | `Cumulative` | Metrics aggregation temporality. |
| `ResourceAttributes` | `IDictionary<string, string>` | empty | Additional OTEL resource attributes. |
| `Headers` | `IDictionary<string, string>` | empty | OTLP headers (e.g., API keys). Env: `OTEL_EXPORTER_OTLP_HEADERS`. |
| `AdditionalActivitySources` | `IList<string>` | empty | Custom ActivitySource names to register in TracerProvider. |
| `AdditionalMeterNames` | `IList<string>` | empty | Custom Meter names to register in MeterProvider. |
| `ConfigureTracerProvider` | `Action<TracerProviderBuilder>?` | `null` | Callback for advanced tracer configuration. |
| `ConfigureMeterProvider` | `Action<MeterProviderBuilder>?` | `null` | Callback for advanced meter configuration. |

### Environment Variables

| Variable | Maps To |
|----------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `Endpoint` (only when at default value) |
| `OTEL_SERVICE_NAME` | `ServiceName` (only when null) |
| `OTEL_RESOURCE_ATTRIBUTES` | `ResourceAttributes` + `Environment` (`deployment.environment` key) |
| `OTEL_EXPORTER_OTLP_HEADERS` | `Headers` (only for keys not already set) |

### Transport Auto-Detection

When `Transport` is at its default (`Grpc`), the port from the configured `Endpoint` is checked:

- Port **4317** → `OtlpTransport.Grpc` (no change)
- Port **4318** → `OtlpTransport.HttpProtobuf` (auto-detected)
- Other ports → no change (remains `Grpc`)

Explicitly setting `Transport` prevents auto-detection.
