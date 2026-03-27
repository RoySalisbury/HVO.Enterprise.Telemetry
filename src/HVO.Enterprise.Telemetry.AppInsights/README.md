# HVO.Enterprise.Telemetry.AppInsights

Application Insights integration for HVO.Enterprise.Telemetry.

## Features

- **Dual Mode** — OTLP and Direct SDK support
- **Telemetry Initializers** — Activity tracing and correlation context enrichment
- **Live Metrics** — Real-time telemetry streaming

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.AppInsights
```

## Quick Start

```csharp
services.AddTelemetry(builder =>
    builder.WithAppInsights(options =>
    {
        options.ConnectionString = "InstrumentationKey=...";
    }));
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
