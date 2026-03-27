# HVO.Enterprise.Telemetry

Core telemetry library providing unified observability (distributed tracing, metrics, structured logging) across all .NET platforms.

## Features

- **Distributed Tracing** — OpenTelemetry-compatible Activity-based tracing
- **Metrics** — Counter, histogram, and gauge instrumentation
- **Structured Logging** — ILogger integration with correlation context
- **Correlation** — Automatic request/operation correlation across service boundaries
- **Health Checks** — Built-in telemetry health monitoring
- **Configuration** — Hierarchical, hot-reloadable telemetry settings
- **Sampling** — Configurable per-operation sampling rates
- **Multi-Platform** — Single binary works on .NET Framework 4.8+ through .NET 10+

## Installation

```
dotnet add package HVO.Enterprise.Telemetry
```

## Quick Start

### Dependency Injection Setup

```csharp
using HVO.Enterprise.Telemetry;

// In Startup.cs or Program.cs
services.AddTelemetry(options =>
{
    options.ServiceName = "MyService";
    options.DefaultSamplingRate = 0.1;
    options.DefaultDetailLevel = DetailLevel.Normal;
});
```

### Tracking Operations

```csharp
// Track an operation with automatic timing and correlation
using var operation = telemetry.TrackOperation("ProcessOrder");
operation.AddProperty("orderId", orderId);
operation.AddProperty("customerId", customerId);

// Nested operations are automatically correlated
using var childOp = telemetry.TrackOperation("ValidatePayment");
childOp.AddProperty("amount", amount);
```

### Error Handling

```csharp
using var operation = telemetry.TrackOperation("ImportData");
try
{
    await ImportDataAsync();
}
catch (Exception ex)
{
    operation.SetException(ex);
    throw;
}
```

### Configuration Hierarchy

Telemetry settings can be configured at multiple levels (global → type → method → call-site), with more specific settings taking precedence:

```csharp
// Global default
services.AddTelemetry(o => o.DefaultSamplingRate = 0.1);

// Per-type override via attribute
[TelemetryOptions(SamplingRate = 1.0)]
public class CriticalService { }

// Per-method override
[TelemetryOptions(DetailLevel = DetailLevel.Detailed, CaptureParameters = CaptureLevel.Values)]
public Task<Order> GetOrderAsync(int id) { }
```

## Extension Packages

| Package | Purpose |
|---------|--------|
| [HVO.Enterprise.Telemetry.OpenTelemetry](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.OpenTelemetry) | OTLP export to Jaeger, Prometheus, Grafana |
| [HVO.Enterprise.Telemetry.Serilog](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Serilog) | Serilog sink integration |
| [HVO.Enterprise.Telemetry.AppInsights](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.AppInsights) | Azure Application Insights bridge |
| [HVO.Enterprise.Telemetry.Datadog](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Datadog) | Datadog APM integration |
| [HVO.Enterprise.Telemetry.IIS](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.IIS) | IIS HTTP module instrumentation |
| [HVO.Enterprise.Telemetry.Wcf](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Wcf) | WCF service/client instrumentation |
| [HVO.Enterprise.Telemetry.Data.EfCore](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.EfCore) | Entity Framework Core interceptor |
| [HVO.Enterprise.Telemetry.Data.AdoNet](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.AdoNet) | ADO.NET command wrapper |

## Documentation

For full documentation, examples, and architecture details, see the [GitHub repository](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry).

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
