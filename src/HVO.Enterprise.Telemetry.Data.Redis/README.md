# HVO.Enterprise.Telemetry.Data.Redis

Redis extension for HVO.Enterprise.Telemetry.

## Features

- **Profiling Integration** — StackExchange.Redis profiler-based tracing
- **Command Tracing** — Automatic spans for Redis operations
- **Semantic Conventions** — OpenTelemetry database span attributes

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.Data.Redis
```

## Quick Start

```csharp
services.AddTelemetry(builder =>
    builder.WithRedisInstrumentation(options =>
    {
        options.FlushInterval = TimeSpan.FromSeconds(5);
    }));
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
