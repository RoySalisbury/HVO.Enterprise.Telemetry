# HVO.Enterprise.Telemetry.Datadog

Datadog integration for HVO.Enterprise.Telemetry.

## Features

- **Dual Mode** — OTLP and DogStatsD support
- **Metrics Export** — Counter, gauge, histogram, and distribution metrics
- **Trace Enrichment** — Automatic Datadog tag injection
- **Cross-Platform Transport** — UDP and Unix domain socket support

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.Datadog
```

## Quick Start

```csharp
services.AddTelemetry(builder =>
    builder.WithDatadog(options =>
    {
        options.ServiceName = "my-service";
        options.Environment = "production";
        options.EnableTraceExporter = true;
        options.EnableMetricsExporter = true;
    }));
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
