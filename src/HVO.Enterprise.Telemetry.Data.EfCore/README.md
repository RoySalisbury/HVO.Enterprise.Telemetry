# HVO.Enterprise.Telemetry.Data.EfCore

Entity Framework Core extension for HVO.Enterprise.Telemetry.

## Features

- **DbCommandInterceptor** — Automatic query tracing via EF Core interceptors
- **Semantic Conventions** — OpenTelemetry database span attributes
- **Performance Tracking** — Query duration and row count metrics

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.Data.EfCore
```

## Quick Start

```csharp
services.AddTelemetry(builder =>
    builder.WithEfCoreInstrumentation(options =>
    {
        options.CaptureCommandText = true;
    }));
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
