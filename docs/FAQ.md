# HVO.Enterprise.Telemetry — Frequently Asked Questions

## Table of Contents

### General
- [What is HVO.Enterprise.Telemetry?](#what-is-hvoenterprisetelemetry)
- [What does "single binary" mean?](#what-does-single-binary-mean)
- [What is the relationship between HVO.Core and HVO.Enterprise.Telemetry?](#what-is-the-relationship-between-hvocore-and-hvoenterprisetelemetry)
- [Which .NET versions are supported?](#which-net-versions-are-supported)
- [Do I need to install all the extension packages?](#do-i-need-to-install-all-the-extension-packages)

### Getting Started
- [Should I use the Static API or Dependency Injection?](#should-i-use-the-static-api-or-dependency-injection)
- [What is the minimum code needed to get telemetry working?](#what-is-the-minimum-code-needed-to-get-telemetry-working)
- [How does HVO telemetry relate to OpenTelemetry?](#how-does-hvo-telemetry-relate-to-opentelemetry)

### Correlation
- [What is a correlation ID and why do I need one?](#what-is-a-correlation-id-and-why-do-i-need-one)
- [Where does the correlation ID come from?](#where-does-the-correlation-id-come-from)
- [Does the correlation ID flow through async/await?](#does-the-correlation-id-flow-through-asyncawait)
- [How do I propagate correlation across HTTP calls?](#how-do-i-propagate-correlation-across-http-calls)
- [How do I propagate correlation to background jobs?](#how-do-i-propagate-correlation-to-background-jobs)

### Operation Scopes
- [What happens if I forget to call Succeed() or Fail()?](#what-happens-if-i-forget-to-call-succeed-or-fail)
- [What happens if I pass null as a tag value?](#what-happens-if-i-pass-null-as-a-tag-value)
- [What is the difference between WithTag and WithProperty?](#what-is-the-difference-between-withtag-and-withproperty)
- [Can I nest operation scopes?](#can-i-nest-operation-scopes)

### Configuration
- [What is the difference between DefaultSamplingRate and per-source Sampling?](#what-is-the-difference-between-defaultsamplingrate-and-per-source-sampling)
- [What happens if I set Enabled to false?](#what-happens-if-i-set-enabled-to-false)
- [Can I change configuration at runtime?](#can-i-change-configuration-at-runtime)
- [What are Feature Flags and which should I enable?](#what-are-feature-flags-and-which-should-i-enable)

### Proxy Instrumentation
- [Why does proxy instrumentation require interfaces?](#why-does-proxy-instrumentation-require-interfaces)
- [Can I exclude specific methods from instrumentation?](#can-i-exclude-specific-methods-from-instrumentation)
- [Does proxy instrumentation handle async methods?](#does-proxy-instrumentation-handle-async-methods)

### Exporters and Backends
- [Can I export to multiple backends at once?](#can-i-export-to-multiple-backends-at-once)
- [How do I configure OTLP export with environment variables only?](#how-do-i-configure-otlp-export-with-environment-variables-only)

### Performance
- [What is the overhead of adding telemetry?](#what-is-the-overhead-of-adding-telemetry)
- [How do I reduce telemetry overhead in production?](#how-do-i-reduce-telemetry-overhead-in-production)
- [What happens if the background queue fills up?](#what-happens-if-the-background-queue-fills-up)

### Migration
- [I am using Application Insights. Can I switch gradually?](#i-am-using-application-insights-can-i-switch-gradually)
- [I am using Serilog for structured logging. How does this fit?](#i-am-using-serilog-for-structured-logging-how-does-this-fit)

### Edge Cases
- [What happens if I call Initialize() twice?](#what-happens-if-i-call-initialize-twice)
- [What happens if I call AddTelemetry() multiple times?](#what-happens-if-i-call-addtelemetry-multiple-times)
- [What happens if telemetry is not initialized and I call StartOperation()?](#what-happens-if-telemetry-is-not-initialized-and-i-call-startoperation)
- [What happens during application shutdown?](#what-happens-during-application-shutdown)
- [Is this library thread-safe?](#is-this-library-thread-safe)

---

## General

### What is HVO.Enterprise.Telemetry?

HVO.Enterprise.Telemetry is a modular .NET library that provides unified
observability — distributed tracing, metrics, structured logging enrichment,
and automatic correlation — across all .NET platforms, from .NET Framework 4.8
to .NET 10+.

It wraps `System.Diagnostics.Activity` (the standard .NET tracing API) with a
higher-level, easier-to-use API while remaining compatible with the OpenTelemetry
ecosystem.

### What does "single binary" mean?

The core library targets .NET Standard 2.0, which means a single compiled DLL
runs on:
- .NET Framework 4.6.1 and later
- .NET Core 2.0 and later
- .NET 5, 6, 7, 8, 9, 10

You don't need different builds for different platforms. The library detects the
runtime and adapts — for example, using the Meter API on .NET 6+ and falling back
to EventCounters on .NET Framework.

### What is the relationship between HVO.Core and HVO.Enterprise.Telemetry?

**HVO.Core** is a separate NuGet package (published from the
[HVO.SDK](https://github.com/RoySalisbury/HVO.SDK) repository) that provides
shared functional primitives:

- `Result<T>` — functional error handling without exceptions
- `Option<T>` — type-safe optional values
- `OneOf<T1, T2>` — discriminated unions
- `Guard` / `Ensure` — input validation

**HVO.Enterprise.Telemetry** depends on `HVO.Core` (it's a transitive NuGet
dependency), so installing the telemetry package automatically pulls it in.
You can use `Result<T>` and other primitives in your own code without a
separate install.

### Which .NET versions are supported?

| Framework | Support Level | Notes |
|-----------|--------------|-------|
| .NET 10 | Full | Latest features |
| .NET 8 | Full | Recommended for new projects |
| .NET 6–7 | Full | Meter API for metrics |
| .NET 5 | Compatible | Via .NET Standard 2.0 |
| .NET Core 2.0–3.1 | Compatible | Via .NET Standard 2.0 |
| .NET Framework 4.8.1 | Compatible | EventCounters fallback for metrics |
| .NET Framework 4.6.1–4.8 | Compatible | Via .NET Standard 2.0 |

### Do I need to install all the extension packages?

No. Only install `HVO.Enterprise.Telemetry` (the core package). Extension
packages are optional — install them based on your specific needs:

- Use Datadog? → Install `.Datadog`
- Use Azure Application Insights? → Install `.AppInsights`
- Want OTLP export? → Install `.OpenTelemetry`
- Use Serilog? → Install `.Serilog`
- Host in IIS? → Install `.IIS`
- Have WCF services? → Install `.Wcf`
- Use EF Core? → Install `.Data.EfCore`

The core package works standalone — it tracks operations, manages correlation,
records metrics, and enriches logs without any extension packages.

---

## Getting Started

### Should I use the Static API or Dependency Injection?

| Scenario | Use | Why |
|----------|-----|-----|
| ASP.NET Core (.NET 6+) | DI (`services.AddTelemetry()`) | Standard DI pattern, automatic lifecycle management |
| Worker Services | DI | Hosted service handles startup/shutdown |
| .NET Framework + IIS | Static (`Telemetry.Initialize()`) | No built-in DI container in ASP.NET MVC/WebForms |
| .NET Framework + WCF | Static | WCF does not use Microsoft DI |
| Console applications | Either | If you already use `HostBuilder`, use DI; otherwise use static |
| Class libraries | Neither directly | Accept `ITelemetryService` via constructor injection; let the host decide |

**Important:** If you use DI, the static `Telemetry` class is automatically
configured by the hosted service — you can still use `Telemetry.StartOperation()`
from non-DI code. You don't need to call `Telemetry.Initialize()` separately.

### What is the minimum code needed to get telemetry working?

**DI (3 lines):**

```csharp
// Program.cs
builder.Services.AddTelemetry(o => o.ServiceName = "MyApp");

// In your service
using var scope = telemetry.StartOperation("DoWork");
scope.Succeed();
```

**Static (4 lines):**

```csharp
// Startup
Telemetry.Initialize(new TelemetryOptions { ServiceName = "MyApp" });

// In your code
using var scope = Telemetry.StartOperation("DoWork");
scope.Succeed();

// Shutdown
Telemetry.Shutdown();
```

### How does HVO telemetry relate to OpenTelemetry?

HVO.Enterprise.Telemetry is **built on top of** the .NET OpenTelemetry
primitives (`System.Diagnostics.Activity`, `System.Diagnostics.ActivitySource`,
`System.Diagnostics.Metrics.Meter`). It is **not** a replacement for
OpenTelemetry — it's a higher-level API that makes OpenTelemetry easier to
use in enterprise .NET applications.

- **Without** the `.OpenTelemetry` extension: Traces exist as in-process
  Activities. They're visible via the health check and statistics endpoints,
  but not exported anywhere.
- **With** the `.OpenTelemetry` extension: Traces and metrics are exported via
  OTLP to your chosen backend (Jaeger, Grafana, etc.).

You can also use the `.Datadog` or `.AppInsights` extensions instead of (or
alongside) `.OpenTelemetry` — they're not mutually exclusive.

---

## Correlation

### What is a correlation ID and why do I need one?

A correlation ID is a unique identifier that links together all operations
related to a single logical request. When a user hits your API, the same
correlation ID follows the request through:

1. The API controller
2. Service layer calls
3. Database queries
4. HTTP calls to other services
5. Background job processing
6. Log entries

Without correlation, finding all log entries and traces for a single user
request in a distributed system requires guesswork. With correlation, you
filter by one ID and see everything.

### Where does the correlation ID come from?

`CorrelationContext.Current` uses a three-tier fallback:

1. **Explicitly set value** — Via `CorrelationContext.BeginScope("my-id")` or
   direct assignment. This takes highest priority.
2. **Activity.Current.TraceId** — If there's an active distributed trace
   (e.g., from an incoming HTTP request with a `traceparent` header), the
   W3C trace ID is used. This gives you automatic correlation in distributed
   tracing scenarios.
3. **Auto-generated GUID** — If neither of the above is available, a new
   GUID is generated and stored in `AsyncLocal<T>`. This ensures you always
   have a correlation ID.

**The key insight:** You never need to null-check `CorrelationContext.Current`.
It always returns a value.

### Does the correlation ID flow through async/await?

Yes. `CorrelationContext` is backed by `AsyncLocal<T>`, which is the standard
.NET mechanism for flowing context through async/await chains. This means:

```csharp
using (CorrelationContext.BeginScope("my-id"))
{
    // "my-id" is available here
    await DoWorkAsync();
    // Still "my-id" here, even after the await
}
```

It also flows into `Task.WhenAll`, `Task.WhenAny`, and other standard async
patterns. The only case where it **doesn't** flow automatically is `Task.Run`
and `ThreadPool.QueueUserWorkItem` — see the next question.

### How do I propagate correlation across HTTP calls?

**Outgoing calls:** Use `TelemetryHttpMessageHandler`, which automatically adds
W3C `traceparent` and `tracestate` headers to outgoing HTTP requests:

```csharp
services.AddHttpClient("MyApi")
    .AddHttpMessageHandler<TelemetryHttpMessageHandler>();
```

**Incoming calls:** Add middleware to extract the correlation ID from the
request header:

```csharp
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? CorrelationContext.Current;
    using (CorrelationContext.BeginScope(correlationId))
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await next();
    }
});
```

### How do I propagate correlation to background jobs?

Use `BackgroundJobContext`:

```csharp
// At the call site (where you queue the work)
var context = BackgroundJobContext.Capture();

// In the background job (different thread/process)
using (context.Restore())
{
    // CorrelationContext.Current is now the original value
    DoBackgroundWork();
}
```

For message queues (RabbitMQ, Azure Service Bus, etc.), serialize the context
into message headers and reconstruct it on the consumer:

```csharp
// Producer
var context = BackgroundJobContext.Capture();
message.Headers["X-Correlation-ID"] = context.CorrelationId;
message.Headers["X-Trace-Parent"] = context.ParentActivityId;

// Consumer
var context = BackgroundJobContext.FromValues(
    correlationId: message.Headers["X-Correlation-ID"],
    parentActivityId: message.Headers["X-Trace-Parent"]);
using (context.Restore()) { /* process message */ }
```

---

## Operation Scopes

### What happens if I forget to call Succeed() or Fail()?

The scope still disposes correctly. The operation will be recorded with a
neutral status — neither explicitly succeeded nor failed. Duration and tags
are still captured.

For best practice, always call `Succeed()` or `Fail()` to make the trace
status explicit:

```csharp
using var scope = telemetry.StartOperation("ProcessOrder");
try
{
    await ProcessAsync();
    scope.Succeed();  // Explicitly mark success
}
catch (Exception ex)
{
    scope.Fail(ex);   // Explicitly mark failure
    throw;
}
```

### What happens if I pass null as a tag value?

Passing `null` as a tag value **removes** the tag:

```csharp
scope.WithTag("userId", "12345");  // Tag is set
scope.WithTag("userId", null);     // Tag is removed
```

This is by design — it allows conditional tag clearing:

```csharp
// If shouldRedact is true, the email tag is removed rather than stored as null
scope.WithTag("email", shouldRedact ? null : user.Email);
```

### What is the difference between WithTag and WithProperty?

| Feature | `WithTag(key, value)` | `WithProperty(key, factory)` |
|---------|----------------------|------------------------------|
| Evaluation | Immediate | Deferred (on scope disposal) |
| Use when | Value is known now | Value is expensive or determined later |
| Allocation | Value is stored immediately | Only the delegate is stored |
| Example | `scope.WithTag("orderId", 42)` | `scope.WithProperty("itemCount", () => items.Count)` |

**Use `WithProperty` for:**
- Values that change during the operation (e.g., counters)
- Values expensive to compute (e.g., JSON serialization)
- Values only known at the end of the operation

```csharp
using var scope = telemetry.StartOperation("BatchImport");

var importedCount = 0;
scope.WithProperty("importedCount", () => importedCount);  // Evaluated on dispose

foreach (var record in records)
{
    Import(record);
    importedCount++;
}
scope.Succeed();
// importedCount is now recorded with the final value
```

### Can I nest operation scopes?

Yes, and it happens automatically. When you start a scope inside another,
the inner scope becomes a child span:

```csharp
using var parent = telemetry.StartOperation("PlaceOrder");

using (var child1 = parent.CreateChild("ValidateInventory"))
{
    // child1 is a child span of parent
    child1.Succeed();
}

using (var child2 = parent.CreateChild("ProcessPayment"))
{
    // child2 is another child span of parent
    child2.Succeed();
}

parent.Succeed();
```

You can also create child scopes implicitly — if `Activity.Current` is set
(which it is inside a scope), any new scope created within becomes a child
in the trace hierarchy.

---

## Configuration

### What is the difference between DefaultSamplingRate and per-source Sampling?

`DefaultSamplingRate` applies to **all** Activity sources unless overridden.
Per-source `Sampling` entries override the default for specific sources.

Example:

```json
{
  "Telemetry": {
    "DefaultSamplingRate": 0.1,
    "Sampling": {
      "MyApp.PaymentService": { "Rate": 1.0 },
      "MyApp.BackgroundJobs": { "Rate": 0.01 }
    }
  }
}
```

| Source | Effective Rate | Why |
|--------|---------------|-----|
| `MyApp.PaymentService` | 100% | Per-source override |
| `MyApp.BackgroundJobs` | 1% | Per-source override |
| `MyApp.OrderService` | 10% | Falls back to default |
| Any other source | 10% | Falls back to default |

### What happens if I set Enabled to false?

All telemetry becomes a no-op:

- `StartOperation()` returns a `NoOpOperationScope` — zero overhead
- `TrackException()`, `TrackEvent()`, `RecordMetric()` do nothing
- Correlation context still works (it's lightweight)
- Health checks report "Healthy" (nothing is broken, just disabled)

This is a master kill switch for emergencies — you can disable telemetry
without redeploying by changing configuration.

### Can I change configuration at runtime?

Yes, if you use the `IConfiguration`-based overload:

```csharp
services.AddTelemetry(builder.Configuration.GetSection("Telemetry"));
```

This registers an `IOptionsMonitor<TelemetryOptions>`, which reacts to
configuration file changes (e.g., modifying `appsettings.json` while the
app is running). Sampling rates, feature flags, and other settings update
without a restart.

### What are Feature Flags and which should I enable?

| Feature Flag | Default | Performance Cost | Recommended For |
|-------------|---------|-----------------|-----------------|
| `EnableHttpInstrumentation` | `true` | Low (~50ns per HTTP call) | Apps making outbound HTTP calls |
| `EnableProxyInstrumentation` | `true` | Low (~100ns per proxied call) | Apps using interface-based DI |
| `EnableExceptionTracking` | `true` | Negligible (on exception only) | All production apps |
| `EnableParameterCapture` | `false` | Medium (depends on parameter size) | Development/debugging; be careful in production (PII risk) |

**For production, we recommend:**

```json
{
  "Features": {
    "EnableHttpInstrumentation": true,
    "EnableExceptionTracking": true,
    "EnableProxyInstrumentation": true,
    "EnableParameterCapture": false
  }
}
```

---

## Proxy Instrumentation

### Why does proxy instrumentation require interfaces?

.NET's `DispatchProxy` (which powers the automatic instrumentation) can only
intercept method calls on **interfaces**, not on concrete classes. This is a
.NET runtime limitation.

If your services are registered as concrete classes:

```csharp
// Won't work with proxy instrumentation
services.AddScoped<OrderService>();
```

Extract an interface:

```csharp
public interface IOrderService
{
    Task<Order> GetOrderAsync(int id);
    Task CreateOrderAsync(OrderRequest request);
}

public class OrderService : IOrderService { /* ... */ }

// Now works
services.AddInstrumentedScoped<IOrderService, OrderService>();
```

### Can I exclude specific methods from instrumentation?

Yes, use the `[NoTelemetry]` attribute:

```csharp
[InstrumentClass]
public interface IMyService
{
    Task<Data> GetDataAsync(int id);   // ✅ Instrumented

    [NoTelemetry]
    bool IsHealthy();                   // ❌ Excluded

    [NoTelemetry]
    string GetVersion();                // ❌ Excluded
}
```

### Does proxy instrumentation handle async methods?

Yes. The proxy detects `Task` and `Task<T>` return types and awaits
completion before closing the span. This means:

- Span duration accurately reflects async operation time
- Exceptions thrown by async methods are captured
- Return values (if `CaptureReturnValue = true`) are captured after completion

```csharp
[InstrumentClass]
public interface IOrderService
{
    // Sync — span closes when method returns
    Order GetOrder(int id);

    // Async — span closes when Task completes
    Task<Order> GetOrderAsync(int id);

    // Void async — span closes when Task completes
    Task ProcessOrderAsync(Order order);
}
```

---

## Exporters and Backends

### Can I export to multiple backends at once?

Yes. Extension packages are composable — you can register multiple exporters:

```csharp
services.AddTelemetry(o => o.ServiceName = "MyService");

// Export traces/metrics to OTLP (Jaeger, Grafana, etc.)
services.AddOpenTelemetryExport(o => o.Endpoint = "http://otel-collector:4317");

// Also export metrics to Datadog via DogStatsD
services.AddDatadogTelemetry(o =>
{
    o.EnableMetricsExporter = true;
    o.EnableTraceExporter = false;  // Use OTLP for traces
});

// Also enrich App Insights
services.AddAppInsightsTelemetry();
```

### How do I configure OTLP export with environment variables only?

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
export OTEL_SERVICE_NAME=my-service
export OTEL_RESOURCE_ATTRIBUTES=deployment.environment=production,team=platform
```

```csharp
services.AddTelemetry(o => o.ServiceName = "my-service");
services.AddOpenTelemetryExportFromEnvironment();
```

The `AddOpenTelemetryExportFromEnvironment()` method reads all configuration
from standard OpenTelemetry environment variables.

---

## Performance

### What is the overhead of adding telemetry?

| Operation | Overhead |
|-----------|----------|
| Starting a sampled operation | ~5–30 ns |
| Starting an unsampled operation | ~5 ns (early exit) |
| Adding a tag | <10 ns |
| Recording a metric | <50 ns |
| Disposing a scope | ~1–5 μs |
| Background queue enqueue | <50 ns |

The target is **<100 ns per operation** on the hot path (excluding disposal).

For reference, a typical HTTP request takes 1–100 **milliseconds**, so
telemetry adds <0.001% overhead.

### How do I reduce telemetry overhead in production?

1. **Lower sampling rates** — `DefaultSamplingRate: 0.01` (1%) drops most
   operations, dramatically reducing overhead and export volume.

2. **Use `AlwaysSampleErrors: true`** — Ensures you never miss errors even
   with low sampling.

3. **Disable unused features** — Only enable Feature Flags you actually need.

4. **Use `WithProperty` for expensive values** — Defers computation to
   scope disposal time (NoOp scopes skip evaluation entirely).

5. **Watch tag cardinality** — Avoid dynamically generated tag keys. Use
   a fixed set of keys with variable values.

### What happens if the background queue fills up?

The background processing queue has a bounded capacity (default: 10,000 items).
When the queue is full, the **oldest queued items are dropped** to make room
for **new items**, and enqueue remains non-blocking. This is intentional —
telemetry should never slow down your application.

If the queue is consistently filling up:
- Your exporter backend may be slow or unreachable
- You may need to increase `Queue.Capacity`
- You may need to lower sampling rates to reduce volume

The health check monitors queue depth and reports `Degraded` or `Unhealthy`
when the queue fills past configurable thresholds.

---

## Migration

### I am using Application Insights. Can I switch gradually?

Yes. Install both packages and run them side by side:

```csharp
// Keep your existing App Insights setup
services.AddApplicationInsightsTelemetry(config);

// Add HVO telemetry alongside it
services.AddTelemetry(o => o.ServiceName = "MyService");
services.AddAppInsightsTelemetry();  // Bridges HVO → App Insights

// Gradually migrate your tracking code:
// Before: telemetryClient.TrackEvent("OrderCreated");
// After:  telemetry.TrackEvent("OrderCreated");
```

See the [Migration Guide](MIGRATION.md) for step-by-step instructions.

### I am using Serilog for structured logging. How does this fit?

HVO.Enterprise.Telemetry does **not** replace Serilog. Instead, it enriches
your Serilog logs with telemetry context:

```csharp
// Keep your Serilog setup
Log.Logger = new LoggerConfiguration()
    .Enrich.WithTelemetry()           // ← Add this: TraceId, SpanId, CorrelationId
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

// Your existing Serilog logging code stays the same
Log.Information("Order {OrderId} created for {Customer}", order.Id, customer.Name);
// ↑ This now includes TraceId, SpanId, CorrelationId automatically
```

---

## Edge Cases

### What happens if I call Initialize() twice?

The second call returns `false` and does nothing. The telemetry service is
already initialized and continues running with the original configuration.

```csharp
var first = Telemetry.Initialize(options1);   // true — initialized
var second = Telemetry.Initialize(options2);  // false — already initialized, no-op
```

This is intentional for safety in scenarios where multiple libraries or
startup paths might try to initialize telemetry.

### What happens if I call AddTelemetry() multiple times?

`AddTelemetry()` is **idempotent**. Calling it multiple times does not create
duplicate service registrations. This is safe in modular applications where
multiple libraries might independently register telemetry.

```csharp
// Both calls are safe — no duplicate registrations
services.AddTelemetry(o => o.ServiceName = "MyApp");
services.AddTelemetry(o => o.Environment = "Production");
// The last Configure delegate wins for overlapping properties
```

### What happens if telemetry is not initialized and I call StartOperation()?

**Static API:** Throws `InvalidOperationException` indicating telemetry has not
been initialized, with guidance to call `Telemetry.Initialize()` or register
telemetry via `AddTelemetry()`.

```csharp
// This throws if Initialize() hasn't been called
var scope = Telemetry.StartOperation("DoWork"); // ❌ InvalidOperationException
```

**DI API:** If `ITelemetryService` is injected, the service is always
initialized (the hosted service handles it).

### What happens during application shutdown?

The library handles shutdown gracefully:

1. **DI mode:** The `TelemetryLifetimeHostedService.StopAsync()` method is
   called by the host. It flushes the background queue and exports all
   pending telemetry.

2. **Static mode:** `Telemetry.Shutdown()` flushes pending data. Additionally,
   `AppDomain.CurrentDomain.ProcessExit` and `AppDomain.CurrentDomain.DomainUnload`
   handlers are registered to catch unexpected shutdowns.

3. **IIS mode (with `.IIS` package):** The `IisLifecycleManager` registers
   with `HostingEnvironment.RegisterObject()` so IIS can signal graceful
   shutdown before recycling the AppDomain.

### Is this library thread-safe?

Yes. All public APIs are designed for concurrent use:

- `CorrelationContext` uses `AsyncLocal<T>` (thread-safe by design)
- `ExceptionAggregator` uses `ConcurrentDictionary` for lock-free operations
- The background queue uses `System.Threading.Channels.Channel` (lock-free, bounded)
- `TelemetryStatistics` uses `Interlocked` operations for atomic counters
- Operation scopes are designed to be used within a single async chain (not shared between threads)

The one rule: don't share a single `IOperationScope` instance across threads.
Each scope should be used by one logical flow of execution (one async chain).
