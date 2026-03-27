# HVO.Enterprise.Telemetry.IIS

IIS extension for HVO.Enterprise.Telemetry.

## Features

- **Lifecycle Management** — Automatic startup and graceful shutdown
- **IRegisteredObject** — Proper IIS application pool recycling support
- **Environment Detection** — IIS hosting environment auto-detection

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.IIS
```

## Quick Start

```csharp
// In your IIS application startup
services.AddTelemetry(builder =>
    builder.WithIisIntegration(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(20);
    }));
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
