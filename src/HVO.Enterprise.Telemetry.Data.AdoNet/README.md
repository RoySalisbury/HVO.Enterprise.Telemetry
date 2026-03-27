# HVO.Enterprise.Telemetry.Data.AdoNet

ADO.NET extension for HVO.Enterprise.Telemetry.

## Features

- **Instrumented Wrappers** — Drop-in DbConnection and DbCommand wrappers
- **Automatic Tracing** — Distributed tracing for raw SQL operations
- **Semantic Conventions** — OpenTelemetry database span attributes

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.Data.AdoNet
```

## Quick Start

```csharp
services.AddTelemetry(builder =>
    builder.WithAdoNetInstrumentation(options =>
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
