# API Reference

This guide summarizes the primary public APIs exposed by `HVO.Enterprise.Telemetry` so you can quickly locate the types referenced in Issue #90 without reverse engineering assemblies. For full XML documentation, ensure you install the latest NuGet package (ships `.xml` files alongside the DLL starting with this change).

## Table of contents

1. [Entry points (`Telemetry` static API)](#telemetry-static-api)
2. [Dependency Injection extensions](#dependency-injection)
3. [TelemetryBuilder fluent configuration](#telemetrybuilder)
4. [Operation scopes (`IOperationScope`)](#operation-scopes)
5. [Correlation and context](#correlation-context)
6. [HTTP instrumentation (`TelemetryHttpMessageHandler`)](#http-instrumentation)
7. [Statistics and health checks](#diagnostics)

## Telemetry static API

Defined in [Telemetry.cs](../src/HVO.Enterprise.Telemetry/Telemetry.cs). Use this when DI is not available (e.g., ASP.NET 4.8, console utilities).

| Member | Description |
|--------|-------------|
| `bool Initialize()` | Initializes telemetry with default options. Returns `false` if already initialized. |
| `bool Initialize(TelemetryOptions options)` | Initializes with explicit options (throws if `options` is `null` or invalid). |
| `bool Initialize(TelemetryOptions options, ILoggerFactory loggerFactory)` | Adds custom logging for internal diagnostics. |
| `void Shutdown()` | Flushes buffers and detaches AppDomain hooks. Safe to call multiple times. |
| `Activity? CurrentActivity` | Shortcut to `Activity.Current`. |
| `string? CurrentCorrelationId` | Returns the AsyncLocal correlation ID, if any. |
| `ITelemetryStatistics Statistics` | Accesses live counters (queue depth, operations per second, etc.). |
| `IOperationScope StartOperation(string operationName)` | Creates a timed scope that automatically records duration, tags, and status. |

Example:

```csharp
Telemetry.Initialize(new TelemetryOptions
{
    ServiceName = "Legacy.Api",
    DefaultSamplingRate = 0.5
});

using var scope = Telemetry.StartOperation("ImportOrders");
scope.WithTag("tenant", tenantId);
try
{
    await importer.RunAsync();
    scope.Succeed();
}
catch (Exception ex)
{
    scope.RecordException(ex);
    scope.Fail("Import failed");
    throw;
}
```

## Dependency injection

Extension methods live in [TelemetryServiceCollectionExtensions.cs](../src/HVO.Enterprise.Telemetry/TelemetryServiceCollectionExtensions.cs).

| Method | Purpose |
|--------|---------|
| `AddTelemetry(Action<TelemetryOptions>? configure = null)` | Registers the telemetry service, hosted service, correlation provider, and statistics singleton. Accepts an optional delegate to mutate options. |
| `AddTelemetry(IConfiguration section)` | Binds `TelemetryOptions` from configuration (see [configuration-schema.md](configuration-schema.md)). |
| `AddTelemetry(Action<TelemetryBuilder> configure)` | Registers core services, then exposes a fluent builder for advanced configuration. |

Related helpers:

| Method | Description |
|--------|-------------|
| `IServiceCollection AddTelemetryLoggingEnrichment(Action<TelemetryLoggerOptions>? configure = null)` | Wraps `ILoggerFactory` to push `TraceId`, `SpanId`, `CorrelationId`, etc. into every log scope. |
| `IServiceCollection AddTelemetryStatistics()` | Registers `ITelemetryStatistics` so it can be injected into background services, diagnostics endpoints, etc. |
| `IServiceCollection AddTelemetryHealthCheck(TelemetryHealthCheckOptions? options = null)` | Adds the built-in `IHealthCheck` that surfaces queue depth, error rate, and exporter status. |

## TelemetryBuilder

`TelemetryBuilder` (see [TelemetryBuilder.cs](../src/HVO.Enterprise.Telemetry/TelemetryBuilder.cs)) is returned by the `AddTelemetry(Action<TelemetryBuilder>)` overload.

Common members:

| Method | Description |
|--------|-------------|
| `Configure(Action<TelemetryOptions>)` | Further mutate `TelemetryOptions` via the standard `Options` pipeline. |
| `AddActivitySource(string name)` | Adds an `ActivitySource` name so the operation scope factory can attach to your custom instrumentation. |
| `AddHttpInstrumentation(Action<HttpInstrumentationOptions>? configure = null)` | Enables outgoing HTTP client instrumentation and W3C header propagation. |
| `WithFirstChanceExceptionMonitoring(Action<FirstChanceExceptionOptions>? configure = null)` | Hooks `AppDomain.CurrentDomain.FirstChanceException` to capture suppressed exceptions for diagnostics. |
| `WithDatadogExporter(...)`, `WithAppInsights(...)`, `WithOpenTelemetry(...)` | Provided by extension packages under [`src/HVO.Enterprise.Telemetry.*`](../src/). |

## Operation scopes

`IOperationScope` and friends live in [IOperationScope.cs](../src/HVO.Enterprise.Telemetry/Abstractions/IOperationScope.cs).

| Member | Description |
|--------|-------------|
| `IOperationScope WithTag(string name, object? value)` | Adds/updates a single tag. Passing `null` removes the tag (Issue #91 behavior). |
| `IOperationScope WithTags(IEnumerable<KeyValuePair<string, object?>> tags)` | Bulk-tag helper. |
| `IOperationScope WithProperty(string name, object? value)` | Records structured data that should not be exported as span tags (e.g., large payload excerpts). |
| `IOperationScope Succeed(string? message = null)` | Marks the Activity as successful with an optional status message. |
| `IOperationScope Fail(string? message = null)` | Marks the Activity as failed without throwing. |
| `IOperationScope RecordException(Exception ex)` | Attaches exception metadata and increments error counters. |
| `IOperationScope CreateChild(string name)` | Creates a nested Activity/operation scope that inherits correlation context. |

Operation scopes automatically dispose activities, calculate duration, and emit structured logs when `AddTelemetryLoggingEnrichment` is active.

## Correlation context

`CorrelationContext` (see [CorrelationContext.cs](../src/HVO.Enterprise.Telemetry/Correlation/CorrelationContext.cs)) manages AsyncLocal state.

Key members:

- `string? Current` — Returns the current correlation ID.
- `IDisposable BeginScope(string? correlationId = null)` — Pushes the specified ID (or auto-generates one) and restores the previous value when disposed.
- `string Ensure()` — Returns the current ID, generating one if necessary.

Use this when you need to flow custom correlation IDs from message headers or job queues.

## HTTP instrumentation

`TelemetryHttpMessageHandler` (see [TelemetryHttpMessageHandler.cs](../src/HVO.Enterprise.Telemetry/Http/TelemetryHttpMessageHandler.cs)) is a ready-to-use `DelegatingHandler` that emits spans around outbound HTTP calls.

```csharp
var handler = new TelemetryHttpMessageHandler(new HttpInstrumentationOptions
{
    CaptureRequestHeaders = true,
    CaptureResponseHeaders = false
})
{
    InnerHandler = new HttpClientHandler()
};

var client = new HttpClient(handler)
{
    BaseAddress = new Uri("https://api.example.com")
};

var response = await client.GetAsync("/orders/42");
```

It records standard OpenTelemetry semantic conventions (`http.method`, `http.url`, `http.status_code`) and injects `traceparent` / `tracestate` headers. On .NET 8+, pair it with `IHttpClientFactory` by registering `AddHttpInstrumentation()` via `TelemetryBuilder`.

## Diagnostics

The health/diagnostics helpers live in [HealthChecks/](../src/HVO.Enterprise.Telemetry/HealthChecks/).

- `TelemetryStatistics` implements `ITelemetryStatistics` and exposes queue depth, dropped payload counts, error rates, etc.
- `TelemetryHealthCheck` consumes `ITelemetryStatistics` and reports `Healthy`, `Degraded`, or `Unhealthy` based on `TelemetryHealthCheckOptions` thresholds.

Example registration:

```csharp
builder.Services
    .AddTelemetry(options => options.ServiceName = "Orders")
    .AddTelemetryLoggingEnrichment();

builder.Services.AddTelemetryStatistics();
builder.Services.AddTelemetryHealthCheck(new TelemetryHealthCheckOptions
{
    DegradedQueueDepthPercent = 70,
    UnhealthyQueueDepthPercent = 90
});

builder.Services.AddHealthChecks()
    .AddCheck<TelemetryHealthCheck>("telemetry");
```

Refer to [quickstart.md](quickstart.md) for an end-to-end walkthrough and [configuration-schema.md](configuration-schema.md) for configuration binding details.
