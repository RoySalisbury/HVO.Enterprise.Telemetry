# HVO.Enterprise.Telemetry.OpenTelemetry

OpenTelemetry OTLP exporter integration for HVO.Enterprise.Telemetry.

## Features

- **OTLP Export** — Send traces, metrics, and logs to any OTLP-compatible backend
- **Auto-Configuration** — Environment variable-based endpoint configuration (`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME`, etc.)
- **Transport Auto-Detection** — Automatically selects HTTP/protobuf when endpoint uses port 4318
- **Provider Wiring** — Automatic TracerProvider, MeterProvider, and LoggerProvider setup
- **Custom Sources & Meters** — Register application-specific ActivitySources and Meters alongside HVO defaults
- **Standard .NET Meters** — Opt-in registration of well-known ASP.NET Core and runtime meters
- **Provider Callbacks** — Advanced `ConfigureTracerProvider` / `ConfigureMeterProvider` callbacks for custom instrumentation
- **Backend Support** — Jaeger, Zipkin, Grafana Tempo, Honeycomb, Dynatrace, New Relic, Splunk, Elastic, Prometheus

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.OpenTelemetry
```

## Quick Start

```csharp
using HVO.Enterprise.Telemetry.OpenTelemetry;

services.AddTelemetry(options => { options.ServiceName = "MyService"; });

services.AddOpenTelemetryExport(options =>
{
    options.ServiceName = "MyWebApi";
    options.ServiceVersion = "1.0.0";
    options.Endpoint = "http://otel-collector:4318"; // auto-detects HTTP/protobuf

    // Export traces, metrics, and logs
    options.EnableTraceExport = true;
    options.EnableMetricsExport = true;
    options.EnableLogExport = true;

    // Register standard .NET meters (ASP.NET Core, Kestrel, System.Net.Http, etc.)
    options.EnableStandardMeters = true;

    // Register custom application sources and meters
    options.AdditionalActivitySources.Add("MyApp");
    options.AdditionalMeterNames.Add("MyApp.Metrics");

    // Advanced: add ASP.NET Core and HttpClient instrumentation via callbacks
    options.ConfigureTracerProvider = builder =>
    {
        builder.AddAspNetCoreInstrumentation();
        builder.AddHttpClientInstrumentation();
    };
});
```

Or configure entirely from environment variables:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
export OTEL_SERVICE_NAME=MyService
export OTEL_RESOURCE_ATTRIBUTES=deployment.environment=production,team=platform
```

```csharp
services.AddOpenTelemetryExportFromEnvironment();
```

## Transport Auto-Detection

When the configured endpoint uses a well-known port, the transport protocol is inferred automatically:

| Port | Protocol | Notes |
|------|----------|-------|
| 4317 | gRPC (default) | No change needed |
| 4318 | HTTP/protobuf | Auto-detected |
| Other | gRPC (default) | Set `Transport` explicitly if needed |

Auto-detection only applies when `Transport` has not been explicitly set.

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
