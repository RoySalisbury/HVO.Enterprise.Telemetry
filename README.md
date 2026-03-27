# HVO.Enterprise

[![Build Status](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/workflows/CI/badge.svg)](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/actions)
[![Tests](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/RoySalisbury/97b18534aba3f1fac0072b22227a9209/raw/tests.json)](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/actions/workflows/ci.yml)
[![Line Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/RoySalisbury/97b18534aba3f1fac0072b22227a9209/raw/coverage-line.json)](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/actions/workflows/ci.yml)
[![Branch Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/RoySalisbury/97b18534aba3f1fac0072b22227a9209/raw/coverage-branch.json)](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/actions/workflows/ci.yml)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

> Unified telemetry and observability for all .NET platforms — single binary from .NET Framework 4.8 to .NET 10+

## Features

- **Automatic Correlation** — AsyncLocal-based correlation across async boundaries
- **Adaptive Metrics** — Meter API (.NET 6+) with EventCounters fallback (.NET Framework)
- **Distributed Tracing** — W3C TraceContext with OpenTelemetry integration
- **High Performance** — <100ns overhead, lock-free queues, zero-allocation hot paths
- **Extensible** — Platform-specific extensions (IIS, WCF, Serilog, App Insights, Datadog, Database, gRPC)
- **Single Binary** — .NET Standard 2.0 for universal deployment
- **Functional Patterns** — `Result<T>`, `Option<T>`, discriminated unions for robust error handling
- **Production-Ready** — Health checks, exception aggregation, configuration hot reload, lifecycle management

## Project Structure

```
HVO.Enterprise/
├── src/
│   ├── HVO.Enterprise.Telemetry/              # Core telemetry library
│   ├── HVO.Enterprise.Telemetry.IIS/          # IIS hosting integration
│   ├── HVO.Enterprise.Telemetry.Wcf/          # WCF instrumentation
│   ├── HVO.Enterprise.Telemetry.Serilog/      # Serilog enrichers
│   ├── HVO.Enterprise.Telemetry.AppInsights/  # Application Insights bridge
│   ├── HVO.Enterprise.Telemetry.Datadog/      # Datadog integration
│   ├── HVO.Enterprise.Telemetry.Data/         # Database instrumentation (shared)
│   ├── HVO.Enterprise.Telemetry.Data.EfCore/  # Entity Framework Core
│   ├── HVO.Enterprise.Telemetry.Data.AdoNet/  # Raw ADO.NET
│   ├── HVO.Enterprise.Telemetry.Data.Redis/   # StackExchange.Redis
│   ├── HVO.Enterprise.Telemetry.Data.RabbitMQ/# RabbitMQ messaging
│   ├── HVO.Enterprise.Telemetry.OpenTelemetry/# OTLP export (traces, metrics, logs)
│   └── HVO.Enterprise.Telemetry.Grpc/         # gRPC interceptor instrumentation
├── tests/                                     # Unit and integration tests
├── samples/
│   └── HVO.Enterprise.Samples.Net8/           # Weather monitoring API sample
├── benchmarks/                                # Performance benchmarks
└── docs/                                      # Documentation
```

> Shared functional primitives (Result, Option, OneOf, guards, extensions) now come from the external [HVO.Core](https://www.nuget.org/packages/HVO.Core) NuGet package that lives in the [HVO.SDK](https://github.com/RoySalisbury/HVO.SDK) repository.

## Packages

### NuGet Availability

| Package | Version | Downloads |
|---------|---------|-----------|
| `HVO.Enterprise.Telemetry` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry) |
| `HVO.Enterprise.Telemetry.OpenTelemetry` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.OpenTelemetry.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.OpenTelemetry) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.OpenTelemetry.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.OpenTelemetry) |
| `HVO.Enterprise.Telemetry.IIS` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.IIS.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.IIS) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.IIS.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.IIS) |
| `HVO.Enterprise.Telemetry.Wcf` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.Wcf.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Wcf) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.Wcf.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Wcf) |
| `HVO.Enterprise.Telemetry.Serilog` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.Serilog.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Serilog) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.Serilog.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Serilog) |
| `HVO.Enterprise.Telemetry.AppInsights` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.AppInsights.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.AppInsights) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.AppInsights.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.AppInsights) |
| `HVO.Enterprise.Telemetry.Datadog` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.Datadog.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Datadog) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.Datadog.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Datadog) |
| `HVO.Enterprise.Telemetry.Data` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.Data.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.Data.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data) |
| `HVO.Enterprise.Telemetry.Data.EfCore` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.Data.EfCore.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.EfCore) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.Data.EfCore.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.EfCore) |
| `HVO.Enterprise.Telemetry.Data.AdoNet` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.Data.AdoNet.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.AdoNet) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.Data.AdoNet.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.AdoNet) |
| `HVO.Enterprise.Telemetry.Data.RabbitMQ` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.Data.RabbitMQ.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.RabbitMQ) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.Data.RabbitMQ.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.RabbitMQ) |
| `HVO.Enterprise.Telemetry.Data.Redis` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.Data.Redis.svg?logo=nuget&label=nuget)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.Redis) | [![Downloads](https://img.shields.io/nuget/dt/HVO.Enterprise.Telemetry.Data.Redis.svg?label=downloads)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Data.Redis) |
| `HVO.Enterprise.Telemetry.Grpc` | Pending release (tracking `docs/user-stories/US-036-grpc-interceptor-extension.md`) | N/A |

### HVO.Core (NuGet dependency)

Shared utilities and functional programming patterns used across all HVO projects, published from [HVO.SDK](https://github.com/RoySalisbury/HVO.SDK):

- **Result&lt;T&gt;** / **Result&lt;T, TEnum&gt;** — Functional error handling without exceptions
- **Option&lt;T&gt;** — Type-safe optional values
- **OneOf&lt;T1, T2, ...&gt;** — Discriminated unions for type-safe variants
- **Extensions** — String, collection, and enum utilities
- **Guard / Ensure** — Input validation and runtime assertions

**Target**: .NET Standard 2.0 (distributed as `HVO.Core` + `HVO.Core.SourceGenerators`)

### HVO.Enterprise.Telemetry

Core telemetry library providing:

- ActivitySource-based distributed tracing
- Runtime-adaptive metrics (Meter API on .NET 6+, EventCounters on .NET Framework)
- Automatic correlation ID management with AsyncLocal
- Background job correlation utilities
- Automatic ILogger enrichment
- User and request context capture
- DispatchProxy-based automatic instrumentation
- Exception tracking and aggregation
- Configuration hot reload
- Health checks and telemetry statistics

**Target**: .NET Standard 2.0

### Extension Packages

| Package | Description |
|---------|-------------|
| `Telemetry.Serilog` | Serilog enrichers (CorrelationId, TraceId, SpanId) |
| `Telemetry.AppInsights` | Azure Application Insights bridge (OTLP / Direct) |
| `Telemetry.Datadog` | Datadog trace and metrics export (OTLP / DogStatsD) |
| `Telemetry.IIS` | IIS hosting lifecycle management |
| `Telemetry.Wcf` | WCF instrumentation with W3C TraceContext |
| `Telemetry.Data` | Shared database instrumentation base |
| `Telemetry.Data.EfCore` | Entity Framework Core interceptor |
| `Telemetry.Data.AdoNet` | Raw ADO.NET wrapper instrumentation |
| `Telemetry.Data.Redis` | StackExchange.Redis profiling |
| `Telemetry.Data.RabbitMQ` | RabbitMQ message instrumentation |
| `Telemetry.OpenTelemetry` | Universal OTLP export for traces, metrics, and logs |
| `Telemetry.Grpc` | gRPC server/client interceptors with `rpc.*` semantic conventions |

## Quick Start

> Prefer a copy-paste friendly walkthrough? See [docs/quickstart.md](docs/quickstart.md).

### Installation

```bash
# Core library
dotnet add package HVO.Core
dotnet add package HVO.Enterprise.Telemetry

# Platform-specific extensions (as needed)
dotnet add package HVO.Enterprise.Telemetry.Serilog
dotnet add package HVO.Enterprise.Telemetry.AppInsights
dotnet add package HVO.Enterprise.Telemetry.Datadog
```

### .NET 8+ Example

```csharp
// Program.cs
using HVO.Enterprise.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Add telemetry with DI
builder.Services.AddTelemetry(options =>
{
    options.DefaultSamplingRate = 0.1;
    options.DefaultDetailLevel = DetailLevel.Normal;
})
.WithActivitySources("MyApp.*")
.WithLoggingEnrichment()
.WithDatadogExporter();

builder.Services.AddHealthChecks()
    .AddCheck<TelemetryHealthCheck>("telemetry");

var app = builder.Build();
app.MapHealthChecks("/health");
app.Run();
```

### .NET Framework 4.8 Example

```csharp
// Global.asax.cs
using HVO.Enterprise.Telemetry;

public class Global : HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        Telemetry.Initialize(config => config
            .WithActivitySources("MyApp.*")
            .WithSampling(samplingRate: 0.1)
            .WithLoggingEnrichment()
            .WithDatadogExporter()
            .ForIIS());
    }

    protected void Application_End(object sender, EventArgs e)
    {
        Telemetry.Shutdown(timeout: TimeSpan.FromSeconds(10));
    }
}
```

### Using Result&lt;T&gt; for Error Handling

```csharp
using HVO.Core.Results;

public Result<Customer> GetCustomer(int id)
{
    try
    {
        var customer = _repository.Find(id);
        if (customer == null)
            return Result<Customer>.Failure(
                new NotFoundException($"Customer {id} not found"));

        return Result<Customer>.Success(customer);
    }
    catch (Exception ex)
    {
        return ex; // Implicit conversion
    }
}

// Usage
var result = GetCustomer(customerId);
if (result.IsSuccessful)
{
    var customer = result.Value;
}
else
{
    _logger.LogError(result.Error, "Failed to get customer");
}
```

### Manual Operation Tracking

```csharp
using HVO.Enterprise.Telemetry;

public async Task<Result<Order>> CreateOrderAsync(OrderRequest request)
{
    using (var operation = _telemetry.TrackOperation(
        "OrderService.CreateOrder",
        detailLevel: DetailLevel.Detailed))
    {
        operation.AddProperty("customerId", request.CustomerId);
        operation.AddProperty("itemCount", request.Items.Count);

        try
        {
            var order = await ProcessOrderAsync(request);
            operation.AddProperty("orderId", order.Id);
            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            operation.SetException(ex);
            return ex;
        }
    }
}
```

## Framework Compatibility

| Framework | Support Level | Notes |
|-----------|--------------|-------|
| .NET 10 | Full | All modern features available |
| .NET 8 | Full | All modern features available |
| .NET 6+ | Full | Meter API for metrics |
| .NET 5 | Compatible | Via .NET Standard 2.0 |
| .NET Core 2.0+ | Compatible | Via .NET Standard 2.0 |
| .NET Framework 4.8.1 | Compatible | EventCounters for metrics |
| .NET Framework 4.6.1-4.8 | Compatible | Via .NET Standard 2.0 |

## Performance Characteristics

- **Activity start**: ~5-30ns (depending on sampling)
- **Property addition**: <10ns (fast-path primitives)
- **Operation Dispose**: ~1-5μs (synchronous timing)
- **Background processing**: Non-blocking with bounded queue
- **Target overhead**: <100ns per operation (excluding Dispose)

See [benchmark results](docs/benchmarks/benchmark-report-2026-02-08.md) for detailed numbers.

## Documentation

> **New to HVO.Enterprise.Telemetry?** Start with the **[Comprehensive User Guide](docs/GUIDE.md)** — it covers everything from installation to advanced usage with examples and edge cases. See the **[FAQ](docs/FAQ.md)** for common questions.

| Document | Description |
|----------|-------------|
| **[📖 User Guide](docs/GUIDE.md)** | **Comprehensive guide** — installation, getting started, core concepts, configuration, extension packages, proxy instrumentation, advanced patterns, troubleshooting |
| **[❓ FAQ](docs/FAQ.md)** | **Frequently asked questions** — common confusion points, edge cases, migration guidance |
| [Quick Start](docs/quickstart.md) | Minimal copy-paste setup example |
| [Configuration Schema](docs/configuration-schema.md) | Complete JSON shape for `TelemetryOptions` |
| [API Reference](docs/api-reference.md) | Key types (`Telemetry`, `IOperationScope`, builders, HTTP handler) |
| [Architecture](docs/ARCHITECTURE.md) | System design, component diagrams, threading model |
| [Platform Differences](docs/DIFFERENCES.md) | .NET Framework 4.8 vs .NET 8+ comparison matrix |
| [Migration Guide](docs/MIGRATION.md) | Migrating from other telemetry libraries |
| [Roadmap](docs/ROADMAP.md) | Version compatibility, breaking change policy |
| [Design Decisions](docs/project-plan.md) | Background, rationale for key architectural decisions |
| [Versioning](docs/VERSIONING.md) | Versioning strategy and release process |
| [Testing](docs/testing.md) | Test layers, docker-compose harness, filters |
| [Pipeline Integration](docs/pipeline-integration.md) | GitHub Actions workflow, tag-based publishing, release order |
| [Benchmarks](docs/benchmarks/benchmark-report-2026-02-08.md) | Performance benchmark results |
| [Sample Config](docs/examples/telemetry.json) | Example `appsettings.json` for telemetry |
| [Sample App](samples/HVO.Enterprise.Samples.Net8/) | Weather monitoring API with full telemetry |

## Development

### Prerequisites

- .NET SDK 8.0 or later
- VS Code with C# Dev Kit (recommended)
- Dev Container support (optional but recommended)

### Building

```bash
# Build entire solution
dotnet build

# Run tests
dotnet test tests/HVO.Common.Tests/HVO.Common.Tests.csproj
dotnet test tests/HVO.Enterprise.Telemetry.Tests/HVO.Enterprise.Telemetry.Tests.csproj
```

### Design Principles

1. **Single Binary Deployment** — .NET Standard 2.0 for maximum compatibility
2. **Runtime Adaptation** — Feature detection for platform-specific capabilities
3. **Performance First** — Non-blocking, minimal allocations, <100ns overhead
4. **Functional Patterns** — `Result<T>`, `Option<T>` for robust error handling
5. **Explicit Over Implicit** — No magic, clear intent, explicit usings
6. **Zero Warnings** — All projects build with zero warnings

## Contributing

1. Follow coding standards in [.github/copilot-instructions.md](.github/copilot-instructions.md)
2. Use conventional commits: `type(scope): description`
3. Ensure all tests pass and build has zero warnings
4. Add XML documentation for all public APIs

## License

MIT

🚧 **In Development** - Core packages and infrastructure in progress