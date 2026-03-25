# HVO.Enterprise.Telemetry — Comprehensive User Guide

> Unified telemetry and observability for all .NET platforms — single binary from .NET Framework 4.8 to .NET 10+.

This guide covers everything you need to know to integrate, configure, and operate
HVO.Enterprise.Telemetry in your applications.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Package Architecture](#2-package-architecture)
3. [Installation](#3-installation)
4. [Getting Started](#4-getting-started)
5. [Core Concepts](#5-core-concepts)
6. [Configuration](#6-configuration)
7. [Extension Packages](#7-extension-packages)
8. [Proxy Instrumentation](#8-proxy-instrumentation)
9. [HTTP Client Instrumentation](#9-http-client-instrumentation)
10. [Background Job Correlation](#10-background-job-correlation)
11. [Exception Tracking](#11-exception-tracking)
12. [Health Checks and Diagnostics](#12-health-checks-and-diagnostics)
13. [Advanced Usage](#13-advanced-usage)
14. [Performance Considerations](#14-performance-considerations)
15. [Troubleshooting](#15-troubleshooting)

---

## 1. Overview

HVO.Enterprise.Telemetry is a modular .NET observability library that provides:

- **Distributed tracing** — W3C TraceContext-compatible spans via `System.Diagnostics.Activity`
- **Metrics** — Runtime-adaptive metrics that use the Meter API on .NET 6+ and EventCounters on .NET Framework
- **Structured logging enrichment** — Automatic injection of correlation IDs, trace IDs, and span IDs into log output
- **Correlation** — AsyncLocal-based correlation ID management that flows across `async`/`await` boundaries
- **Automatic instrumentation** — DispatchProxy-based method-level telemetry for interface-driven services

The library targets **.NET Standard 2.0**, which means a single binary works on .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5–10+. Runtime-adaptive features detect the host platform and use the best available API.

### How It Fits Together

```
Your Application
    │
    ├─ calls ──▶  HVO.Enterprise.Telemetry (core)
    │                 • Operation scopes & tracing
    │                 • Correlation context
    │                 • Metrics recording
    │                 • Log enrichment
    │                 • Proxy instrumentation
    │                 • Exception tracking
    │
    └─ optionally adds ──▶  Extension packages
                              • .OpenTelemetry  →  OTLP export to Jaeger, Grafana, etc.
                              • .Datadog        →  DogStatsD metrics + trace enrichment
                              • .AppInsights     →  Azure Application Insights bridge
                              • .Serilog         →  Serilog enrichers
                              • .Wcf             →  WCF message inspector instrumentation
                              • .IIS             →  IIS lifecycle management
                              • .Grpc            →  gRPC interceptors
                              • .Data.*          →  Database instrumentation
```

---

## 2. Package Architecture

### Core Package

| Package | NuGet | Purpose |
|---------|-------|---------|
| `HVO.Enterprise.Telemetry` | [![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.svg)](https://www.nuget.org/packages/HVO.Enterprise.Telemetry) | Core library — tracing, metrics, correlation, logging enrichment, proxy instrumentation, exception tracking |

This is the only **required** package. All other packages are optional extensions.

### Extension Packages

| Package | Purpose | When to Use |
|---------|---------|-------------|
| `HVO.Enterprise.Telemetry.OpenTelemetry` | OTLP export for traces and metrics | You want to send telemetry to Jaeger, Grafana Tempo, Zipkin, Honeycomb, Dynatrace, New Relic, Splunk, Elastic, or any OTLP-compatible backend |
| `HVO.Enterprise.Telemetry.Datadog` | Datadog APM integration | You use Datadog for monitoring and need DogStatsD metrics or trace enrichment |
| `HVO.Enterprise.Telemetry.AppInsights` | Azure Application Insights bridge | You use Azure Application Insights and want correlation + trace integration |
| `HVO.Enterprise.Telemetry.Serilog` | Serilog enrichers | You use Serilog as your logging framework and want native enricher integration |
| `HVO.Enterprise.Telemetry.Wcf` | WCF message inspector instrumentation | You have WCF services and want automatic W3C TraceContext propagation |
| `HVO.Enterprise.Telemetry.IIS` | IIS lifecycle management | You host in IIS and need graceful startup/shutdown integration |
| `HVO.Enterprise.Telemetry.Grpc` | gRPC server/client interceptors | You have gRPC services and want automatic span creation |
| `HVO.Enterprise.Telemetry.Data` | Shared database instrumentation base | Required by the specific `Data.*` packages below |
| `HVO.Enterprise.Telemetry.Data.EfCore` | Entity Framework Core interceptor | You use EF Core and want query-level tracing |
| `HVO.Enterprise.Telemetry.Data.AdoNet` | ADO.NET command wrapper | You use raw ADO.NET (SqlCommand, etc.) and want command-level tracing |
| `HVO.Enterprise.Telemetry.Data.Redis` | StackExchange.Redis profiling | You use Redis and want command-level tracing |
| `HVO.Enterprise.Telemetry.Data.RabbitMQ` | RabbitMQ message instrumentation | You use RabbitMQ and want publish/consume tracing with correlation propagation |

### Dependency: HVO.Core

All HVO packages depend on [HVO.Core](https://www.nuget.org/packages/HVO.Core) (published from [HVO.SDK](https://github.com/RoySalisbury/HVO.SDK)), which provides functional primitives:

- `Result<T>` / `Result<T, TEnum>` — functional error handling without exceptions
- `Option<T>` — type-safe optional values
- `OneOf<T1, T2, ...>` — discriminated unions
- `Guard` / `Ensure` — input validation

These are **transitive dependencies** — installing `HVO.Enterprise.Telemetry` automatically pulls in `HVO.Core`.

---

## 3. Installation

### .NET 8+ / .NET 10+ Projects

```bash
# Core library (always required)
dotnet add package HVO.Enterprise.Telemetry

# Pick the extensions you need:
dotnet add package HVO.Enterprise.Telemetry.OpenTelemetry   # OTLP export
dotnet add package HVO.Enterprise.Telemetry.Serilog         # Serilog enrichers
dotnet add package HVO.Enterprise.Telemetry.AppInsights     # Azure App Insights
dotnet add package HVO.Enterprise.Telemetry.Datadog         # Datadog APM
```

### .NET Framework 4.8 Projects

```powershell
# Via NuGet Package Manager Console
Install-Package HVO.Enterprise.Telemetry
Install-Package HVO.Enterprise.Telemetry.IIS          # If hosted in IIS
Install-Package HVO.Enterprise.Telemetry.Wcf          # If using WCF services
```

### Choosing the Right Packages

Ask yourself:

1. **Where does my telemetry data go?**
   - Jaeger / Grafana / Zipkin / any OTLP backend → install `.OpenTelemetry`
   - Datadog → install `.Datadog`
   - Azure Application Insights → install `.AppInsights`
   - Multiple backends → install multiple; they compose without conflict

2. **What logging framework do I use?**
   - Serilog → install `.Serilog` for native enrichers
   - Microsoft.Extensions.Logging → the core package handles enrichment automatically

3. **What platform am I on?**
   - IIS → install `.IIS`
   - WCF → install `.Wcf`
   - gRPC → install `.Grpc`

4. **What data stores do I use?**
   - Entity Framework Core → install `.Data.EfCore`
   - Raw ADO.NET → install `.Data.AdoNet`
   - Redis → install `.Data.Redis`
   - RabbitMQ → install `.Data.RabbitMQ`

---

## 4. Getting Started

HVO.Enterprise.Telemetry supports two initialization patterns:

| Pattern | Best for | Initialization |
|---------|----------|----------------|
| **Dependency Injection (DI)** | .NET 6+, ASP.NET Core, Worker Services | `services.AddTelemetry(...)` |
| **Static API** | .NET Framework 4.8, Console apps, legacy code | `Telemetry.Initialize(...)` |

Both patterns provide the same functionality; choose based on your application architecture.

### 4.1 .NET 8+ with Dependency Injection (Recommended)

**Step 1: Add NuGet packages**

```bash
dotnet add package HVO.Enterprise.Telemetry
dotnet add package HVO.Enterprise.Telemetry.OpenTelemetry  # if exporting via OTLP
```

**Step 2: Configure `appsettings.json`**

```json
{
  "Telemetry": {
    "ServiceName": "MyService",
    "ServiceVersion": "1.0.0",
    "Environment": "Development",
    "Enabled": true,
    "DefaultSamplingRate": 1.0,
    "ActivitySources": [
      "HVO.Enterprise.Telemetry",
      "MyService.*"
    ],
    "Logging": {
      "EnableCorrelationEnrichment": true
    },
    "Features": {
      "EnableHttpInstrumentation": true,
      "EnableExceptionTracking": true
    }
  }
}
```

**Step 3: Register in `Program.cs`**

```csharp
using HVO.Enterprise.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Register core telemetry from configuration
builder.Services.AddTelemetry(builder.Configuration.GetSection("Telemetry"));

// Add optional features
builder.Services.AddTelemetryLoggingEnrichment();  // Enrich ILogger with CorrelationId, TraceId
builder.Services.AddTelemetryStatistics();          // Enable diagnostics endpoint
builder.Services.AddTelemetryHealthCheck();          // Health check integration

// Add OTLP export (if using OpenTelemetry package)
builder.Services.AddOpenTelemetryExport(options =>
{
    options.Endpoint = "http://localhost:4317";  // OTLP collector
});

var app = builder.Build();
app.MapHealthChecks("/health");
app.Run();
```

**Step 4: Use in your services**

```csharp
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Correlation;

public class OrderService
{
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ITelemetryService telemetry, ILogger<OrderService> logger)
    {
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(OrderRequest request)
    {
        // Start an operation scope — automatically creates a span and correlates logs
        using var scope = _telemetry.StartOperation("OrderService.CreateOrder");

        // Add context as tags (visible in traces)
        scope.WithTag("customerId", request.CustomerId)
             .WithTag("itemCount", request.Items.Count);

        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);
        // ↑ This log automatically includes CorrelationId, TraceId, SpanId

        try
        {
            var order = await ProcessOrderAsync(request);

            scope.WithTag("orderId", order.Id)
                 .Succeed();

            return order;
        }
        catch (Exception ex)
        {
            scope.Fail(ex);  // Marks the span as failed and records the exception
            throw;
        }
    }
}
```

### 4.2 .NET Framework 4.8 with Static API

**Step 1: Install packages**

```powershell
Install-Package HVO.Enterprise.Telemetry
Install-Package HVO.Enterprise.Telemetry.IIS
```

**Step 2: Initialize in `Global.asax.cs`**

```csharp
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Configuration;

public class Global : HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        var options = new TelemetryOptions
        {
            ServiceName = "MyLegacyApp",
            ServiceVersion = "2.5.0",
            Environment = "Production",
            DefaultSamplingRate = 0.1,  // Sample 10% of operations
            Features = new FeatureFlags
            {
                EnableHttpInstrumentation = true,
                EnableExceptionTracking = true
            }
        };

        Telemetry.Initialize(options);
    }

    protected void Application_End(object sender, EventArgs e)
    {
        Telemetry.Shutdown();
    }
}
```

**Step 3: Use in your code**

```csharp
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Correlation;

public class OrderHandler
{
    public Order CreateOrder(OrderRequest request)
    {
        using var scope = Telemetry.StartOperation("OrderHandler.CreateOrder");

        scope.WithTag("customerId", request.CustomerId);

        try
        {
            var order = ProcessOrder(request);
            scope.Succeed();
            return order;
        }
        catch (Exception ex)
        {
            scope.Fail(ex);
            throw;
        }
    }
}
```

### 4.3 Builder Pattern (Advanced DI Configuration)

For more control, use the builder pattern:

```csharp
builder.Services.AddTelemetry(telemetry =>
{
    telemetry
        .Configure(options =>
        {
            options.ServiceName = "MyService";
            options.DefaultSamplingRate = 0.5;
        })
        .AddActivitySource("MyService.*")
        .AddActivitySource("SharedLibrary.*")
        .AddHttpInstrumentation(http =>
        {
            http.CaptureRequestHeaders = true;
            http.CaptureResponseHeaders = false;
        })
        .WithFirstChanceExceptionMonitoring(exc =>
        {
            exc.Enabled = true;
            exc.MaxEventsPerSecond = 50;
        });
});
```

---

## 5. Core Concepts

### 5.1 Operation Scopes

An **operation scope** is the fundamental unit of telemetry. It wraps a logical unit
of work — an API call, a database query, a message processing step — and automatically
captures:

- **Duration** (via high-resolution `Stopwatch`)
- **Success/failure status**
- **Tags** (key-value pairs attached to the span)
- **Correlation ID** (linking related operations)
- **Parent-child relationships** (nested scopes)

```csharp
// Basic usage
using var scope = telemetry.StartOperation("ProcessPayment");
scope.WithTag("amount", payment.Amount)
     .WithTag("currency", payment.Currency);

// ... do work ...

scope.Succeed();
```

#### Nesting Scopes (Parent-Child)

Scopes nest automatically. When you start a scope inside another, the inner scope
becomes a child span in the trace:

```csharp
using var parent = telemetry.StartOperation("OrderService.PlaceOrder");

// Validate inventory — appears as a child span
using var validateScope = parent.CreateChild("ValidateInventory");
var inventoryOk = await CheckInventoryAsync(order.Items);
validateScope.WithTag("inventoryAvailable", inventoryOk).Succeed();

// Process payment — another child span
using var paymentScope = parent.CreateChild("ProcessPayment");
await ChargeCustomerAsync(order);
paymentScope.Succeed();

parent.Succeed();
```

This produces a trace tree:

```
OrderService.PlaceOrder (200ms)
  ├── ValidateInventory (45ms)
  └── ProcessPayment (120ms)
```

#### Lazy Properties

Tags are evaluated immediately. For values that are expensive to compute or only
known at the end of an operation, use `WithProperty` — the factory function is
called only when the scope is disposed:

```csharp
using var scope = telemetry.StartOperation("BatchProcess");

var processedCount = 0;

// This lambda runs when the scope disposes, not now
scope.WithProperty("itemsProcessed", () => processedCount);

foreach (var item in items)
{
    Process(item);
    processedCount++;
}

scope.Succeed();
// At this point, WithProperty evaluates and records processedCount = items.Count
```

#### Handling Failures

```csharp
using var scope = telemetry.StartOperation("SendEmail");

try
{
    await emailClient.SendAsync(message);
    scope.Succeed();
}
catch (SmtpException ex)
{
    // Fail marks the span status as Error and records the exception
    scope.Fail(ex);
    throw;
}
```

#### Setting a Result

```csharp
using var scope = telemetry.StartOperation("LookupUser");

var user = await userRepo.FindAsync(userId);

scope.WithResult(user);  // Records whether the result is null/non-null
// If user is null, this is a useful signal in traces
```

#### Null Tags

Passing `null` as a tag value **removes** the tag rather than storing a null:

```csharp
scope.WithTag("userId", userId);   // Sets the tag
scope.WithTag("userId", null);     // Removes the tag
```

This is intentional — it lets you conditionally clear sensitive data without
branching:

```csharp
scope.WithTag("email", shouldRedact ? null : user.Email);
```

### 5.2 Correlation Context

Every operation in a distributed system needs a way to link related work together.
HVO.Enterprise.Telemetry provides `CorrelationContext` — an AsyncLocal-based
correlation ID that flows seamlessly through `async`/`await` boundaries.

#### Three-Tier Fallback

When you read `CorrelationContext.Current`, the system uses a three-tier fallback:

1. **Explicit value** — If you set a correlation ID via `BeginScope` or direct assignment, that value is returned
2. **Activity.Current.TraceId** — If there's an active distributed trace, the trace ID is used
3. **Auto-generated GUID** — If neither is available, a new GUID is generated and stored

This means `CorrelationContext.Current` **always returns a value** — you never need
to null-check it.

```csharp
// Always has a value — safe to use anywhere
string correlationId = CorrelationContext.Current;
_logger.LogInformation("Processing with correlation {CorrelationId}", correlationId);
```

#### Setting a Correlation ID

```csharp
// Explicitly set a correlation ID for a scope of work
using (CorrelationContext.BeginScope("order-12345"))
{
    // Everything in here uses "order-12345" as the correlation ID
    await ProcessOrderAsync();
    await SendConfirmationAsync();
    // ↑ Both of these see the same correlation ID
}
// Previous correlation ID is automatically restored here
```

#### How Correlation Flows in ASP.NET Core

Typically, you propagate correlation IDs from incoming HTTP headers. A middleware
component reads the `X-Correlation-ID` header and establishes a scope:

```csharp
// Middleware example
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();

    using (CorrelationContext.BeginScope(correlationId))
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await next();
    }
});
```

#### Static API for Correlation

The `Telemetry` static class also provides correlation helpers:

```csharp
// Set a specific correlation ID
using (Telemetry.SetCorrelationId("my-correlation-id"))
{
    // ...
}

// Generate a new correlation ID
using (Telemetry.BeginCorrelation())
{
    // A new GUID is generated and set
}
```

### 5.3 Metrics

HVO.Enterprise.Telemetry provides a runtime-adaptive metrics system:

- On **.NET 6+** → uses `System.Diagnostics.Metrics.Meter` API (standard OpenTelemetry-compatible meters)
- On **.NET Framework** → falls back to `EventCounters`

You don't need to worry about which API is used — the library detects the runtime
and selects the appropriate implementation automatically.

#### Recording Metrics

```csharp
// Via ITelemetryService (DI)
telemetry.RecordMetric("orders.created", 1);
telemetry.RecordMetric("payment.amount", order.Total);

// Via static API
Telemetry.RecordMetric("cache.hit_ratio", hitRatio);
```

#### Custom Metrics with Operation Scopes

Operation scopes automatically record:
- `operation.duration` — how long the operation took
- `operation.count` — number of operations (counter)

### 5.4 Logging Enrichment

When you call `AddTelemetryLoggingEnrichment()`, the library wraps the
`ILoggerFactory` to automatically inject context into every log entry's scope:

| Property | Source | Example |
|----------|--------|---------|
| `CorrelationId` | `CorrelationContext.Current` | `"order-12345"` |
| `TraceId` | `Activity.Current?.TraceId` | `"4bf92f3577b34da6a3ce929d0e0e4736"` |
| `SpanId` | `Activity.Current?.SpanId` | `"00f067aa0ba902b7"` |

This means every `_logger.LogInformation(...)` call in your application automatically
includes these fields — **no code changes needed in your services**.

#### Serilog Integration

If you use Serilog, install `HVO.Enterprise.Telemetry.Serilog` for native enrichers:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithTelemetry()  // Adds TraceId, SpanId, CorrelationId
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
        "| Correlation={CorrelationId} Trace={TraceId}{NewLine}{Exception}")
    .CreateLogger();
```

You can also add the enrichers individually for more control:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithActivity(                          // TraceId, SpanId, ParentId
        traceIdPropertyName: "trace_id",           // customize property names
        spanIdPropertyName: "span_id")
    .Enrich.WithCorrelation(                       // CorrelationId
        propertyName: "correlation_id",
        fallbackToActivity: true)                  // use TraceId if no explicit correlation
    .WriteTo.Console()
    .CreateLogger();
```

---

## 6. Configuration

### 6.1 TelemetryOptions Reference

All configuration is rooted in the `TelemetryOptions` class. Here is the complete
shape with defaults:

```json
{
  "Telemetry": {
    "ServiceName": "Unknown",
    "ServiceVersion": null,
    "Environment": null,
    "Enabled": true,
    "DefaultSamplingRate": 1.0,

    "ActivitySources": [
      "HVO.Enterprise.Telemetry"
    ],

    "Sampling": {
      "MyApp.Critical": {
        "Rate": 1.0,
        "AlwaysSampleErrors": true
      },
      "MyApp.Background": {
        "Rate": 0.01,
        "AlwaysSampleErrors": true
      }
    },

    "Logging": {
      "EnableCorrelationEnrichment": true,
      "MinimumLevel": "Information"
    },

    "Metrics": {
      "Enabled": true,
      "CollectionIntervalSeconds": 60
    },

    "Queue": {
      "Capacity": 10000,
      "BatchSize": 100
    },

    "Features": {
      "EnableHttpInstrumentation": false,
      "EnableProxyInstrumentation": false,
      "EnableExceptionTracking": false,
      "EnableParameterCapture": false
    },

    "ResourceAttributes": {
      "deployment.region": "us-east-1",
      "team.name": "platform"
    }
  }
}
```

### 6.2 Property Details

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServiceName` | `string` | `"Unknown"` | Logical service name for traces, metrics, and exporter tags. **Always set this.** |
| `ServiceVersion` | `string?` | `null` | Semantic version (e.g., `"1.2.3"`). Recommended for OTLP/Datadog exporters. |
| `Environment` | `string?` | `null` | Deployment environment (`Development`, `Staging`, `Production`). |
| `Enabled` | `bool` | `true` | Master switch. `false` disables all telemetry (operation scopes become no-ops). |
| `DefaultSamplingRate` | `double` | `1.0` | Probability `[0.0, 1.0]` that any operation is sampled. `1.0` = sample everything, `0.1` = sample 10%. |
| `ActivitySources` | `List<string>` | `["HVO.Enterprise.Telemetry"]` | Activity source names to listen on. Add your app's source names here. Supports wildcards like `"MyApp.*"`. |
| `Sampling` | `Dictionary<string, SamplingOptions>` | `{}` | Per-source sampling overrides, keyed by Activity source name. |
| `Logging.EnableCorrelationEnrichment` | `bool` | `true` | Inject CorrelationId/TraceId/SpanId into ILogger scopes. |
| `Logging.MinimumLevel` | `string` | `"Information"` | Minimum log level for telemetry internal logging. |
| `Metrics.Enabled` | `bool` | `true` | Enable the metrics subsystem. |
| `Metrics.CollectionIntervalSeconds` | `int` | `60` | How often (in seconds) metrics are flushed. Must be > 0. |
| `Queue.Capacity` | `int` | `10000` | Maximum items in the background processing queue. Must be ≥ 100. |
| `Queue.BatchSize` | `int` | `100` | Items per batch when flushing. Must be in `(0, Capacity]`. |
| `Features.EnableHttpInstrumentation` | `bool` | `false` | Auto-instrument outbound HTTP calls via `TelemetryHttpMessageHandler`. |
| `Features.EnableProxyInstrumentation` | `bool` | `false` | Enable DispatchProxy-based automatic method instrumentation. |
| `Features.EnableExceptionTracking` | `bool` | `false` | Track and aggregate exceptions via `ExceptionAggregator`. |
| `Features.EnableParameterCapture` | `bool` | `false` | Capture method parameters as span tags (use with care for PII). |
| `ResourceAttributes` | `Dictionary<string, object>` | `{}` | Additional OTLP resource attributes attached to all telemetry. |

### 6.3 Validation Rules

`TelemetryOptions.Validate()` is called automatically during initialization and
checks:

| Rule | Constraint |
|------|------------|
| `ServiceName` | Must not be null or empty |
| `DefaultSamplingRate` | Must be between 0.0 and 1.0 (inclusive) |
| `Queue.Capacity` | Must be ≥ 100 |
| `Queue.BatchSize` | Must be > 0 and ≤ `Capacity` |
| `Metrics.CollectionIntervalSeconds` | Must be > 0 |
| Per-source sampling rates | Each must be between 0.0 and 1.0 |

If validation fails, an `InvalidOperationException` is thrown with a description
of the problem.

### 6.4 Configuration Sources

**From `appsettings.json`:**

```csharp
services.AddTelemetry(builder.Configuration.GetSection("Telemetry"));
```

**From code:**

```csharp
services.AddTelemetry(options =>
{
    options.ServiceName = "MyService";
    options.DefaultSamplingRate = 0.5;
});
```

**From environment variables** (standard .NET configuration binding):

```bash
export Telemetry__ServiceName=MyService
export Telemetry__DefaultSamplingRate=0.5
export Telemetry__Queue__Capacity=5000
```

**Combined** (environment variables override JSON, code overrides everything):

```csharp
services.AddTelemetry(builder.Configuration.GetSection("Telemetry"));
// Then override specific settings:
services.Configure<TelemetryOptions>(o => o.Environment = "Staging");
```

### 6.5 Per-Source Sampling

You can set different sampling rates for different parts of your application:

```json
{
  "Telemetry": {
    "DefaultSamplingRate": 0.1,
    "Sampling": {
      "MyApp.PaymentService": {
        "Rate": 1.0,
        "AlwaysSampleErrors": true
      },
      "MyApp.BackgroundJobs": {
        "Rate": 0.01,
        "AlwaysSampleErrors": true
      }
    }
  }
}
```

In this example:
- Payment operations are **always** sampled (critical path)
- Background jobs are sampled at **1%** (high volume, less critical)
- Everything else is sampled at **10%** (default)
- Errors are **always** sampled regardless of rate (when `AlwaysSampleErrors` is true)

---

## 7. Extension Packages

### 7.1 OpenTelemetry Export (`HVO.Enterprise.Telemetry.OpenTelemetry`)

Exports traces and metrics via the OpenTelemetry Protocol (OTLP) to any
compatible backend.

**Compatible Backends:** Jaeger, Grafana Tempo, Zipkin, Honeycomb, Dynatrace,
New Relic, Splunk, Elastic APM, Prometheus (metrics), and any OTLP collector.

#### Setup

```csharp
using HVO.Enterprise.Telemetry.OpenTelemetry;

services.AddTelemetry(options => { options.ServiceName = "MyService"; });

services.AddOpenTelemetryExport(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.Environment = "Production";
    options.Endpoint = "http://otel-collector:4317";   // gRPC
    // options.Endpoint = "http://otel-collector:4318"; // HTTP/protobuf
    options.Transport = OtlpTransport.Grpc;            // or OtlpTransport.Http
    options.EnableTraceExport = true;
    options.EnableMetricsExport = true;
});
```

#### Environment Variable Configuration

You can also configure via standard OpenTelemetry environment variables:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
export OTEL_SERVICE_NAME=MyService
export OTEL_RESOURCE_ATTRIBUTES=deployment.environment=production,team=platform
```

Then register with no explicit configuration:

```csharp
services.AddOpenTelemetryExportFromEnvironment();
```

#### Key Types

| Type | Purpose |
|------|---------|
| `OtlpExportOptions` | Configuration for endpoint, transport, headers, service info |
| `HvoActivitySourceRegistrar` | Discovers and registers HVO ActivitySource names |
| `ServiceCollectionExtensions` | `AddOpenTelemetryExport()` / `AddOpenTelemetryExportFromEnvironment()` |

### 7.2 Datadog Integration (`HVO.Enterprise.Telemetry.Datadog`)

Provides Datadog-specific metric export via DogStatsD and trace enrichment.

#### Setup

```csharp
using HVO.Enterprise.Telemetry.Datadog;

services.AddTelemetry(options => { options.ServiceName = "MyService"; });

services.AddDatadogTelemetry(options =>
{
    options.ServiceName = "my-service";
    options.Environment = "production";
    options.EnableMetricsExporter = true;
    options.EnableTraceExporter = true;
    options.StatsdHost = "localhost";  // DogStatsD agent
    options.StatsdPort = 8125;
});
```

#### Dual-Mode Support

Datadog supports two export modes:
- **OTLP** — Use when the Datadog Agent is configured with OTLP ingest (Agent v7.35+)
- **DogStatsD** — Use for direct metric emission via UDP/UDS to the Datadog Agent

#### Key Types

| Type | Purpose |
|------|---------|
| `DatadogOptions` | Configuration (service name, environment, transport, API key) |
| `DatadogMetricsExporter` | DogStatsD metric client |
| `DatadogTraceExporter` | Activity enrichment with Datadog-specific tags |

### 7.3 Application Insights (`HVO.Enterprise.Telemetry.AppInsights`)

Bridges HVO telemetry with Azure Application Insights.

#### Setup

```csharp
using HVO.Enterprise.Telemetry.AppInsights;

services.AddTelemetry(options => { options.ServiceName = "MyService"; });

services.AddAppInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
});
```

#### Key Types

| Type | Purpose |
|------|---------|
| `ApplicationInsightsBridge` | Main bridge between HVO and App Insights |
| `ActivityTelemetryInitializer` | Converts Activity spans to App Insights telemetry |
| `CorrelationTelemetryInitializer` | Enriches App Insights with correlation IDs |

### 7.4 Serilog Enrichers (`HVO.Enterprise.Telemetry.Serilog`)

Native Serilog enrichers for Activity tracing and correlation context.

#### Setup

```csharp
using HVO.Enterprise.Telemetry.Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.WithTelemetry()  // Adds TraceId, SpanId, ParentId, CorrelationId
    .WriteTo.Console()
    .CreateLogger();
```

#### Individual Enrichers

```csharp
// Activity only (TraceId, SpanId, ParentId)
.Enrich.WithActivity()

// Correlation only (CorrelationId)
.Enrich.WithCorrelation()

// Both (equivalent to .WithTelemetry())
.Enrich.WithActivity()
.Enrich.WithCorrelation()
```

#### Custom Property Names

```csharp
.Enrich.WithActivity(
    traceIdPropertyName: "trace_id",
    spanIdPropertyName: "span_id",
    parentIdPropertyName: "parent_id")
.Enrich.WithCorrelation(
    propertyName: "correlation_id",
    fallbackToActivity: true)
```

#### Core vs Serilog Enrichment

The **core** package enriches via `ILogger.BeginScope` (works with any logging
provider). The **Serilog** package uses native `ILogEventEnricher` (more efficient
when Serilog is your primary framework). You can use both — they don't conflict.

### 7.5 WCF Instrumentation (`HVO.Enterprise.Telemetry.Wcf`)

Automatic W3C TraceContext propagation in WCF SOAP headers.

#### Setup (Server)

```csharp
// In your WCF service configuration
// The WCF extension automatically adds message inspectors that:
// 1. Extract W3C traceparent/tracestate from incoming SOAP headers
// 2. Create Activity spans for each WCF operation
// 3. Propagate trace context to outgoing WCF calls
```

### 7.6 IIS Hosting (`HVO.Enterprise.Telemetry.IIS`)

Lifecycle management for IIS-hosted applications.

#### Setup

```csharp
// In Global.asax Application_Start
Telemetry.Initialize(config => config.ForIIS());
```

The IIS extension:
- Registers with `HostingEnvironment.RegisterObject()` for graceful shutdown
- Detects IIS hosting environment automatically
- Ensures telemetry flushes before the AppDomain unloads

### 7.7 gRPC Interceptors (`HVO.Enterprise.Telemetry.Grpc`)

Server and client interceptors for gRPC services. Automatically creates spans
with `rpc.*` semantic conventions (`rpc.service`, `rpc.method`, `rpc.system`).

### 7.8 Database Instrumentation (`HVO.Enterprise.Telemetry.Data.*`)

#### Entity Framework Core

```csharp
using HVO.Enterprise.Telemetry.Data.EfCore;

services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    // The EF Core interceptor adds automatic tracing
});
```

#### ADO.NET

```csharp
using HVO.Enterprise.Telemetry.Data.AdoNet;

// Wraps SqlCommand with automatic span creation
```

#### Redis

```csharp
using HVO.Enterprise.Telemetry.Data.Redis;

// Integrates with StackExchange.Redis profiling
```

#### RabbitMQ

```csharp
using HVO.Enterprise.Telemetry.Data.RabbitMQ;

// Instruments publish/consume with correlation header propagation
```

---

## 8. Proxy Instrumentation

Proxy instrumentation **automatically** wraps every method call on an interface
with an operation scope — no manual `using var scope = ...` needed. It uses
.NET's `DispatchProxy` to create a runtime proxy.

### 8.1 Basic Setup

**Step 1: Register the proxy factory**

```csharp
services.AddTelemetryProxyFactory();
```

**Step 2: Register your service with instrumentation**

```csharp
// Instead of:
services.AddScoped<IOrderService, OrderService>();

// Use:
services.AddInstrumentedScoped<IOrderService, OrderService>();
```

That's it. Every method call on `IOrderService` now automatically gets:
- A span with the operation name `"IOrderService.MethodName"`
- Duration tracking
- Exception recording
- Parameter capture (if enabled)

### 8.2 Controlling Instrumentation with Attributes

#### Instrument All Methods (Default)

```csharp
[InstrumentClass]
public interface IOrderService
{
    Task<Order> GetOrderAsync(int orderId);        // ✅ Instrumented
    Task<List<Order>> GetAllOrdersAsync();          // ✅ Instrumented
    Task CreateOrderAsync(OrderRequest request);    // ✅ Instrumented
}
```

#### Exclude Specific Methods

```csharp
[InstrumentClass]
public interface IOrderService
{
    Task<Order> GetOrderAsync(int orderId);        // ✅ Instrumented

    [NoTelemetry]
    bool IsHealthy();                               // ❌ Skipped
}
```

#### Fine-Tune Method Instrumentation

```csharp
[InstrumentClass(ActivityKind = ActivityKind.Server)]
public interface IPaymentService
{
    [InstrumentMethod(
        OperationName = "Payment.Charge",
        CaptureParameters = true,
        CaptureReturnValue = true,
        LogLevel = LogLevel.Information)]
    Task<PaymentResult> ChargeAsync(decimal amount, string currency);

    [InstrumentMethod(CaptureParameters = false)]  // Don't capture sensitive params
    Task<bool> ValidateCardAsync(string cardNumber, string cvv);
}
```

#### Attribute Reference

**`[InstrumentClass]`** (on interfaces):

| Property | Default | Description |
|----------|---------|-------------|
| `OperationPrefix` | Interface name | Prefix for operation names |
| `ActivityKind` | `Internal` | Span kind (`Internal`, `Server`, `Client`, `Producer`, `Consumer`) |
| `CaptureParameters` | `true` | Capture parameters as span tags by default |
| `LogEvents` | `true` | Log method entry/exit events |

**`[InstrumentMethod]`** (on methods — overrides class-level settings):

| Property | Default | Description |
|----------|---------|-------------|
| `OperationName` | `"{Interface}.{Method}"` | Custom operation name |
| `ActivityKind` | `Internal` | Span kind for this method |
| `CaptureParameters` | `true` | Capture parameters |
| `CaptureReturnValue` | `false` | Capture return value as a span tag |
| `LogEvents` | `true` | Log entry/exit |
| `LogLevel` | `Debug` | Log level for events |

**`[NoTelemetry]`** (on methods):

Excludes the method from instrumentation entirely.

### 8.3 Lifetime Options

```csharp
services.AddInstrumentedTransient<IService, ServiceImpl>();  // New proxy per resolve
services.AddInstrumentedScoped<IService, ServiceImpl>();     // One proxy per scope
services.AddInstrumentedSingleton<IService, ServiceImpl>(); // One proxy for app lifetime
```

### 8.4 Async Support

The proxy correctly handles:
- `Task` return types
- `Task<T>` return types
- Synchronous methods
- `void` methods

Async methods wait for the task to complete before closing the span and recording
duration, so trace timings are accurate.

### 8.5 Important: Interface Requirement

DispatchProxy only works with **interfaces**. If your service is a concrete class
without an interface, you need to extract an interface first:

```csharp
// Won't work — no interface
services.AddInstrumentedScoped<OrderService, OrderService>(); // ❌

// Works — interface + implementation
services.AddInstrumentedScoped<IOrderService, OrderService>(); // ✅
```

---

## 9. HTTP Client Instrumentation

The `TelemetryHttpMessageHandler` is a `DelegatingHandler` that wraps `HttpClient`
calls with automatic spans.

### 9.1 Automatic (via DI)

```csharp
services.AddTelemetry(builder => builder.AddHttpInstrumentation());

services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddHttpMessageHandler<TelemetryHttpMessageHandler>();
```

### 9.2 Manual

```csharp
var handler = new TelemetryHttpMessageHandler(new HttpInstrumentationOptions
{
    CaptureRequestHeaders = true,
    CaptureResponseHeaders = false
})
{
    InnerHandler = new HttpClientHandler()
};

var client = new HttpClient(handler);
```

### 9.3 What It Captures

| Tag | Example | Description |
|-----|---------|-------------|
| `http.method` | `GET` | HTTP method |
| `http.url` | `https://api.example.com/users` | Request URL (sensitive parts redacted) |
| `http.status_code` | `200` | Response status code |
| `http.request.content_length` | `1024` | Request body size |
| `http.response.content_length` | `4096` | Response body size |

W3C `traceparent` and `tracestate` headers are automatically propagated to
outgoing requests, enabling end-to-end distributed tracing.

---

## 10. Background Job Correlation

When you queue work for background processing (e.g., via `Task.Run`, Hangfire,
or message queues), the correlation context from the original request is lost
because the work runs on a different thread/context.

`BackgroundJobContext` solves this by capturing and restoring the full correlation
state.

### 10.1 Capture and Restore

```csharp
public class OrderProcessor
{
    public void EnqueueOrder(Order order)
    {
        // Capture the current context (correlation ID, trace IDs, user context)
        var context = BackgroundJobContext.Capture();

        // Queue the work — context would normally be lost
        _ = Task.Run(async () =>
        {
            // Restore the captured context
            using (context.Restore())
            {
                // CorrelationContext.Current now returns the original correlation ID
                await ProcessOrderAsync(order);
            }
        });
    }
}
```

### 10.2 With Custom Metadata

```csharp
var context = BackgroundJobContext.Capture(new Dictionary<string, object>
{
    ["queueName"] = "orders",
    ["priority"] = "high",
    ["retryCount"] = 0
});
```

### 10.3 Manual Construction (for Deserialization)

When receiving a background job from a message queue, you may need to
reconstruct the context from serialized data:

```csharp
var context = BackgroundJobContext.FromValues(
    correlationId: message.Headers["X-Correlation-ID"],
    parentActivityId: message.Headers["X-Trace-Parent"],
    enqueuedAt: message.Timestamp);

using (context.Restore())
{
    await ProcessMessageAsync(message);
}
```

### 10.4 What Gets Captured

| Property | Description |
|----------|-------------|
| `CorrelationId` | The current `CorrelationContext.Current` value |
| `ParentActivityId` | `Activity.Current?.Id` — for parent-child trace linking |
| `ParentSpanId` | `Activity.Current?.SpanId` — for span reference |
| `UserContext` | Key-value pairs from the current user context |
| `EnqueuedAt` | Timestamp when `Capture()` was called |
| `CustomMetadata` | Arbitrary metadata you pass to `Capture()` |

---

## 11. Exception Tracking

### 11.1 Exception Aggregation

The `ExceptionAggregator` collects exceptions, groups them by **fingerprint**
(a hash of the exception type and stack trace), and tracks occurrence counts
and timing.

```csharp
// Via static API
Telemetry.TrackException(exception);

// Via ITelemetryService
telemetry.TrackException(exception);

// Via operation scope
scope.Fail(exception);          // Records exception + marks scope as failed
scope.RecordException(exception); // Records exception without marking as failed
```

#### Querying Exception Statistics

```csharp
var aggregator = Telemetry.GetExceptionAggregator();

// Total exceptions tracked
long total = aggregator.TotalExceptions;

// Error rates
double perMinute = aggregator.GetGlobalErrorRatePerMinute();
double perHour = aggregator.GetGlobalErrorRatePerHour();
double percentage = aggregator.GetGlobalErrorRatePercentage(totalOperations: 10000);

// Exception groups (grouped by fingerprint)
foreach (var group in aggregator.GetGroups())
{
    Console.WriteLine($"{group.Fingerprint}: {group.Count} occurrences");
    Console.WriteLine($"  Type: {group.FirstException.GetType().Name}");
    Console.WriteLine($"  First: {group.FirstOccurrence}");
    Console.WriteLine($"  Last: {group.LastOccurrence}");
}
```

### 11.2 First-Chance Exception Monitoring

Enable this to detect **all** exceptions the instant they're thrown — including
exceptions that are caught and suppressed:

```csharp
services.AddTelemetry(builder => builder
    .WithFirstChanceExceptionMonitoring(options =>
    {
        options.Enabled = true;
        options.MaxEventsPerSecond = 50;  // Rate limit to avoid flooding
    }));
```

> **Warning:** First-chance exception monitoring has performance implications.
> Every `throw` in the process triggers the handler. Use `MaxEventsPerSecond`
> to limit impact.

---

## 12. Health Checks and Diagnostics

### 12.1 Health Check

Register the telemetry health check to monitor telemetry subsystem health:

```csharp
services.AddTelemetryHealthCheck();

// Or with custom options:
services.AddTelemetryHealthCheck(new TelemetryHealthCheckOptions
{
    DegradedQueueDepthPercent = 70,   // Report "Degraded" when queue is 70% full
    UnhealthyQueueDepthPercent = 90   // Report "Unhealthy" when queue is 90% full
});

// Map health check endpoints
app.MapHealthChecks("/health");
```

The health check evaluates:
- Is the telemetry service initialized and running?
- Is the background processing queue backing up?
- Are there error rates exceeding thresholds?

### 12.2 Telemetry Statistics

```csharp
services.AddTelemetryStatistics();
```

Then inspect at runtime:

```csharp
// Via DI
var stats = serviceProvider.GetRequiredService<ITelemetryStatistics>();

// Via static API
var stats = Telemetry.Statistics;

// Snapshot for serialization/display
var snapshot = stats.GetSnapshot();
```

The `ITelemetryStatistics` interface provides real-time data about:
- Total operations started/completed
- Active operations count
- Queue depth
- Exception counts
- Per-source activity statistics

### 12.3 Diagnostics Endpoint

The sample application demonstrates a diagnostics endpoint that returns live
telemetry statistics as JSON:

```csharp
app.MapGet("/diagnostics", (ITelemetryStatistics stats) =>
{
    return Results.Ok(new
    {
        stats.TotalOperations,
        stats.ActiveOperations,
        stats.QueueDepth,
        stats.ExceptionCount
    });
});
```

---

## 13. Advanced Usage

### 13.1 Multiple Activity Sources

Organize your application telemetry into logical groups:

```csharp
services.AddTelemetry(builder => builder
    .AddActivitySource("MyApp.Api")           // API layer
    .AddActivitySource("MyApp.Domain")        // Business logic
    .AddActivitySource("MyApp.Infrastructure") // Data access
);
```

This lets you:
- Set different sampling rates per layer
- Filter traces by source in your backend
- Turn off verbose layers in production

### 13.2 Custom Resource Attributes

Add metadata that appears on every span, metric, and log:

```json
{
  "Telemetry": {
    "ResourceAttributes": {
      "deployment.region": "us-east-1",
      "deployment.cluster": "prod-a",
      "team.name": "platform-team",
      "team.oncall": "platform-oncall@company.com"
    }
  }
}
```

### 13.3 Conditional Sampling

Use the `AlwaysSampleErrors` flag to ensure errors are never dropped by sampling:

```json
{
  "Telemetry": {
    "DefaultSamplingRate": 0.01,
    "Sampling": {
      "MyApp.*": {
        "Rate": 0.01,
        "AlwaysSampleErrors": true
      }
    }
  }
}
```

With this configuration, only 1% of successful operations are sampled, but
**every failed operation** is captured.

### 13.4 Multi-Level Configuration

HVO supports a 5-level configuration hierarchy (most specific wins):

1. **Global defaults** — `TelemetryOptions` properties
2. **Namespace-level** — Sampling by activity source name
3. **Type-level** — `[InstrumentClass]` attribute on interfaces
4. **Method-level** — `[InstrumentMethod]` attribute on methods
5. **Call-site** — Direct scope configuration (`WithTag`, etc.)

Example combining levels:

```csharp
// Level 1: Global defaults (appsettings.json)
// "DefaultSamplingRate": 0.1

// Level 2: Per-namespace sampling (appsettings.json)
// "Sampling": { "PaymentService": { "Rate": 1.0 } }

// Level 3: Type-level defaults
[InstrumentClass(CaptureParameters = true, ActivityKind = ActivityKind.Server)]
public interface IPaymentService
{
    // Level 4: Method-level override
    [InstrumentMethod(CaptureReturnValue = true)]
    Task<PaymentResult> ChargeAsync(decimal amount, string currency);

    // Level 5: Call-site configuration in the implementation
    // scope.WithTag("gateway", "stripe");
}
```

### 13.5 PII Redaction

The library includes PII detection and redaction for parameter capture:

```csharp
[InstrumentClass]
public interface IUserService
{
    // These parameters will have their values redacted in spans:
    Task CreateUserAsync(
        string username,
        [SensitiveData] string password,
        [SensitiveData] string socialSecurityNumber);
}
```

When `CaptureParameters` is true, the span will show:
```
username = "john.doe"
password = "[REDACTED]"
socialSecurityNumber = "[REDACTED]"
```

### 13.6 Static and DI Side by Side

If your application uses both DI and non-DI code (common in migration
scenarios), both work simultaneously:

```csharp
// Program.cs — DI initialization
services.AddTelemetry(options => { options.ServiceName = "MyApp"; });

// Legacy code — static API works too
using var scope = Telemetry.StartOperation("LegacyOperation");
```

The DI registration sets up the static `Telemetry` class automatically via
the `TelemetryLifetimeHostedService`. You don't need to call
`Telemetry.Initialize()` separately.

---

## 14. Performance Considerations

### 14.1 Overhead Benchmarks

| Operation | Typical Latency | Allocation |
|-----------|----------------|------------|
| Start an Activity | ~5–30 ns | 0 bytes (pooled) |
| Add a tag (primitive) | <10 ns | 0 bytes |
| Add a tag (string) | ~10–20 ns | String allocation |
| Add a lazy property | <10 ns | Delegate capture |
| Dispose scope (success) | ~1–5 μs | Minimal |
| Background queue enqueue | <50 ns | 0 bytes (bounded channel) |

**Target overhead: <100 ns per operation** (excluding scope disposal).

### 14.2 Production Recommendations

**Sampling:** Set `DefaultSamplingRate` to `0.01`–`0.1` (1–10%) for high-volume
services. Use `AlwaysSampleErrors: true` so errors are never dropped.

**Queue Capacity:** The default of `10000` is suitable for most applications.
For very high-throughput services (>10K ops/sec), increase to `50000`.

**Feature Flags:** Only enable features you actually use:
- `EnableParameterCapture` adds overhead to every proxied method call
- `EnableHttpInstrumentation` wraps every HTTP client call
- `EnableExceptionTracking` adds to exception-handling cost

**Tag Cardinality:** Avoid using high-cardinality values as tag keys:
```csharp
// ❌ Bad — creates a new tag per user ID (unbounded cardinality)
scope.WithTag($"user.{userId}", true);

// ✅ Good — bounded cardinality
scope.WithTag("userId", userId);
```

**Lazy Properties for Expensive Computations:**
```csharp
// ❌ Computes JSON even if the span is not sampled
scope.WithTag("response", JsonSerializer.Serialize(response));

// ✅ Only computed on disposal, and only if span is active
scope.WithProperty("response", () => JsonSerializer.Serialize(response));
```

---

## 15. Troubleshooting

### Telemetry Is Not Appearing

1. **Is telemetry enabled?**
   Check that `Enabled` is `true` in your configuration:
   ```json
   { "Telemetry": { "Enabled": true } }
   ```

2. **Is the service initialized?**
   ```csharp
   Console.WriteLine(Telemetry.IsInitialized); // Should be true
   ```

3. **Are your Activity sources registered?**
   If you use custom activity source names, make sure they're in the
   `ActivitySources` list:
   ```json
   { "Telemetry": { "ActivitySources": ["HVO.Enterprise.Telemetry", "MyApp.*"] } }
   ```

4. **Is sampling dropping everything?**
   Temporarily set `DefaultSamplingRate` to `1.0` to sample all operations.

5. **Is the exporter configured?**
   If using `.OpenTelemetry`, verify the endpoint is reachable:
   ```bash
   curl http://otel-collector:4317
   ```

### Correlation IDs Are Not Flowing

1. **Across async/await:** Correlation uses `AsyncLocal<T>`, which flows
   automatically with `async`/`await`. If you use `Task.Run` or
   `ThreadPool.QueueUserWorkItem`, use `BackgroundJobContext.Capture()`
   and `.Restore()` to propagate context manually.

2. **Across HTTP boundaries:** Ensure `X-Correlation-ID` header propagation
   middleware is configured:
   ```csharp
   app.Use(async (context, next) =>
   {
       var id = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? CorrelationContext.Current;
       using (CorrelationContext.BeginScope(id))
       {
           context.Response.Headers["X-Correlation-ID"] = id;
           await next();
       }
   });
   ```

3. **Across message queues:** Serialize the correlation ID in your message
   headers and use `BackgroundJobContext.FromValues()` on the consumer side.

### Health Check Reports Unhealthy

The telemetry health check monitors the background processing queue:

- **Healthy** — Queue depth below `DegradedQueueDepthPercent` (default: 70%)
- **Degraded** — Queue depth between 70%–90% of capacity
- **Unhealthy** — Queue depth above `UnhealthyQueueDepthPercent` (default: 90%)

If the queue is filling up:
- Increase `Queue.Capacity`
- Check if your exporter backend (OTLP collector, Datadog agent) is reachable
- Reduce telemetry volume with lower sampling rates

### Proxy Instrumentation Not Working

1. **Is `AddTelemetryProxyFactory()` called?**
   ```csharp
   services.AddTelemetryProxyFactory();  // Must be called before AddInstrumented*
   ```

2. **Are you using an interface?**
   DispatchProxy requires an interface. Concrete classes can't be proxied.

3. **Is the feature flag enabled?**
   ```json
   { "Telemetry": { "Features": { "EnableProxyInstrumentation": true } } }
   ```

### Exception: "Telemetry has not been initialized"

This `InvalidOperationException` is thrown when you call `Telemetry.StartOperation()`,
`Telemetry.TrackException()`, or other methods before initialization.

**Fix for DI apps:** Ensure `AddTelemetry()` is called in `Program.cs` and the
hosted service has started (this happens automatically).

**Fix for static API apps:** Call `Telemetry.Initialize(options)` before using
other methods.

### Duplicate Registrations

`AddTelemetry()` is **idempotent** — calling it multiple times is safe and won't
create duplicate registrations. The same applies to `AddOpenTelemetryExport()`
and `AddDatadogTelemetry()`.

---

## Further Reading

| Document | Description |
|----------|-------------|
| [Quick Start](quickstart.md) | Copy-paste minimal setup |
| [Configuration Schema](configuration-schema.md) | Full JSON schema reference |
| [API Reference](api-reference.md) | Complete public API documentation |
| [Architecture](ARCHITECTURE.md) | System design, component diagrams, threading model |
| [Platform Differences](DIFFERENCES.md) | .NET Framework vs .NET 8+ comparison |
| [Migration Guide](MIGRATION.md) | Migrating from Application Insights, Serilog, OpenTelemetry, or custom solutions |
| [FAQ](FAQ.md) | Frequently asked questions and edge cases |
| [Benchmarks](benchmarks/benchmark-report-2026-02-08.md) | Performance benchmark results |
| [Sample Application](../samples/HVO.Enterprise.Samples.Net8/) | Weather monitoring API with full telemetry integration |
