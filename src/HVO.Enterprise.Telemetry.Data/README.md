# HVO.Enterprise.Telemetry.Data

Shared data instrumentation infrastructure for HVO.Enterprise.Telemetry.

## Features

- **Semantic Conventions** — OpenTelemetry database semantic conventions
- **Parameter Sanitization** — Automatic sensitive data redaction
- **Database Detection** — Auto-detect database system from connection strings
- **Base Package** — Required by EfCore, AdoNet, RabbitMQ, and Redis extensions

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.Data
```

## Quick Start

This is the base package for data instrumentation. Install a provider-specific package for automatic instrumentation:

- `HVO.Enterprise.Telemetry.Data.EfCore` — Entity Framework Core
- `HVO.Enterprise.Telemetry.Data.AdoNet` — ADO.NET / raw DbConnection
- `HVO.Enterprise.Telemetry.Data.Redis` — StackExchange.Redis
- `HVO.Enterprise.Telemetry.Data.RabbitMQ` — RabbitMQ

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
