# HVO.Enterprise.Telemetry.Serilog

Serilog enrichers for HVO.Enterprise.Telemetry.

## Features

- **Activity Enricher** — Adds TraceId, SpanId, and ParentId to log events
- **Correlation Enricher** — Adds CorrelationId and custom context properties
- **Automatic Integration** — Works with any Serilog sink

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.Serilog
```

## Quick Start

```csharp
using Serilog;
using HVO.Enterprise.Telemetry.Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.WithTelemetry()  // Adds TraceId, SpanId, CorrelationId
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
        "{Properties:j}{NewLine}{Exception}")
    .CreateLogger();
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
