# HVO.Enterprise.Telemetry.Data.RabbitMQ

RabbitMQ extension for HVO.Enterprise.Telemetry.

## Features

- **Publish Tracing** — Automatic spans for message publishing
- **Consume Tracing** — Context propagation on message consumption
- **W3C TraceContext** — Distributed tracing via message headers

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.Data.RabbitMQ
```

## Quick Start

```csharp
services.AddTelemetry(builder =>
    builder.WithRabbitMqInstrumentation(options =>
    {
        options.CaptureMessageHeaders = true;
    }));
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
