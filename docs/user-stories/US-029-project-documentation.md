# US-029: Project Documentation

**Status**: ❌ Not Started  
**Category**: Documentation  
**Effort**: 8 story points  
**Sprint**: 10

## Description

As a **developer adopting HVO.Enterprise**,  
I want **comprehensive documentation covering setup, migration, architecture, and differences between platforms**,  
So that **I can quickly understand the library, migrate existing applications, and make informed decisions about usage**.

## Acceptance Criteria

1. **README Documentation**
   - [ ] Root README.md with project overview and quick start
   - [ ] Clear installation instructions for all platforms
   - [ ] Basic usage examples for common scenarios
   - [ ] Links to detailed documentation
   - [ ] Badge integration (build status, coverage, version)

2. **DIFFERENCES.md**
   - [ ] Platform feature comparison matrix (.NET Framework 4.8 vs .NET 8+)
   - [ ] Behavioral differences documented
   - [ ] Performance characteristics by platform
   - [ ] Fallback strategies explained
   - [ ] Best practices per platform

3. **MIGRATION.md**
   - [ ] Migration paths from common telemetry libraries
   - [ ] Step-by-step migration guides
   - [ ] Code transformation examples
   - [ ] Common pitfalls and solutions
   - [ ] Testing strategies during migration

4. **ARCHITECTURE.md**
   - [ ] System architecture overview
   - [ ] Component interaction diagrams
   - [ ] Extension point documentation
   - [ ] Performance design decisions
   - [ ] Threading and concurrency model

5. **ROADMAP.md**
   - [ ] Current feature status
   - [ ] Planned features with timeline
   - [ ] Breaking change policy
   - [ ] Deprecation schedule
   - [ ] Version compatibility matrix

6. **API Documentation**
   - [ ] All public APIs have XML documentation
   - [ ] Code examples in XML comments
   - [ ] DocFX or similar API reference site
   - [ ] Links between related APIs

## Technical Requirements

### README.md Structure

```markdown
# HVO.Enterprise.Telemetry

[![Build Status](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/workflows/CI/badge.svg)](...)
[![NuGet](https://img.shields.io/nuget/v/HVO.Enterprise.Telemetry.svg)](...)
[![Test Coverage](https://img.shields.io/codecov/c/github/RoySalisbury/HVO.Enterprise.Telemetry)](...)

> Unified telemetry and observability for all .NET platforms - single binary from .NET Framework 4.8 to .NET 10+

## Features

- **🔄 Automatic Correlation** - AsyncLocal-based correlation across async boundaries
- **📊 Adaptive Metrics** - Meter API (.NET 6+) with EventCounters fallback (.NET Framework)
- **📈 Distributed Tracing** - W3C TraceContext with OpenTelemetry integration
- **⚡ High Performance** - <100ns overhead, lock-free queues, zero-allocation hot paths
- **🔌 Extensible** - Platform-specific extensions (IIS, WCF, Database, etc.)
- **📦 Single Binary** - .NET Standard 2.0 for universal deployment

## Quick Start

### Installation

```bash
# Core library (required)
dotnet add package HVO.Enterprise.Telemetry

# Extension packages (optional)
dotnet add package HVO.Enterprise.Telemetry.Iis
dotnet add package HVO.Enterprise.Telemetry.Wcf
dotnet add package HVO.Enterprise.Telemetry.Database
```

### Basic Usage

#### ASP.NET Core (.NET 8+)

```csharp
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add HVO telemetry with all features
builder.Services.AddTelemetry(options =>
{
    options.ServiceName = "MyApi";
    options.EnableAutoInstrumentation = true;
    options.MetricsEnabled = true;
});

var app = builder.Build();
app.Run();
```

#### ASP.NET (.NET Framework 4.8)

```csharp
using HVO.Enterprise.Telemetry;
using System.Web.Http;

public class WebApiApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        // Static initialization for non-DI scenarios
        Telemetry.Initialize(options =>
        {
            options.ServiceName = "MyLegacyApi";
            options.EnableAutoInstrumentation = true;
        });
        
        GlobalConfiguration.Configure(WebApiConfig.Register);
    }
    
    protected void Application_End()
    {
        // Graceful shutdown
        Telemetry.Shutdown();
    }
}
```

#### Manual Instrumentation

```csharp
using HVO.Enterprise.Telemetry;
using System.Diagnostics;

public class OrderService
{
    private static readonly ActivitySource ActivitySource = 
        new ActivitySource("MyApp.OrderService");

    public async Task<Result<Order>> ProcessOrderAsync(int orderId)
    {
        // Create operation scope with automatic timing
        using var operation = ActivitySource.StartOperation("ProcessOrder");
        operation.SetTag("orderId", orderId);
        
        try
        {
            var order = await _repository.GetOrderAsync(orderId);
            if (order == null)
            {
                return Result<Order>.Failure(new NotFoundException());
            }
            
            await _paymentService.ChargeAsync(order);
            operation.SetTag("amount", order.Total);
            
            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            operation.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result<Order>.Failure(ex); // Explicit conversion required
        }
    }
}
```

## Platform Support

| Platform | Version | Status | Metrics API | Notes |
|----------|---------|--------|-------------|-------|
| .NET 8+ | 8.0+ | ✅ Full | Meter | All features, best performance |
| .NET 6-7 | 6.0-7.0 | ✅ Full | Meter | All features |
| .NET 5 | 5.0 | ✅ Full | EventCounters | All features |
| .NET Core | 2.0-3.1 | ✅ Full | EventCounters | All features |
| .NET Standard | 2.0+ | ✅ Full | EventCounters | Runtime feature detection |
| .NET Framework | 4.8+ | ✅ Full | EventCounters | IIS integration available |

## Documentation

- [**Getting Started**](./docs/getting-started.md) - Setup and basic concepts
- [**Platform Differences**](./docs/DIFFERENCES.md) - Feature comparison across platforms
- [**Migration Guide**](./docs/MIGRATION.md) - Migrate from other telemetry libraries
- [**Architecture**](./docs/ARCHITECTURE.md) - System design and internals
- [**API Reference**](./docs/api/index.html) - Complete API documentation
- [**Roadmap**](./docs/ROADMAP.md) - Future features and timeline

## Extensions

| Package | Description | Platforms |
|---------|-------------|-----------|
| `HVO.Enterprise.Telemetry.Iis` | IIS lifecycle integration | .NET Framework 4.8+ |
| `HVO.Enterprise.Telemetry.Wcf` | WCF service instrumentation | .NET Framework 4.8+ |
| `HVO.Enterprise.Telemetry.Database` | EF, Dapper, ADO.NET tracking | All platforms |
| `HVO.Enterprise.Telemetry.Serilog` | Serilog enrichers | All platforms |
| `HVO.Enterprise.Telemetry.AppInsights` | Application Insights bridge | All platforms |
| `HVO.Enterprise.Telemetry.Datadog` | Datadog exporter | All platforms |

## Performance

- **Overhead**: <100ns per telemetry operation
- **Throughput**: >1M operations/second
- **Memory**: <1MB baseline overhead
- **Queue**: >10K items/second background processing

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for development setup and guidelines.

## License

MIT - See [LICENSE](./LICENSE) for details.
```

### DIFFERENCES.md Content

```markdown
# Platform Differences

HVO.Enterprise.Telemetry targets .NET Standard 2.0 for maximum compatibility while providing adaptive features based on the runtime platform. This document explains behavioral differences across platforms.

## Feature Matrix

| Feature | .NET Framework 4.8 | .NET Standard 2.0 | .NET 5-7 | .NET 8+ |
|---------|-------------------|-------------------|----------|---------|
| **Distributed Tracing** | ✅ Activity API | ✅ Activity API | ✅ Activity API | ✅ Activity API |
| **W3C TraceContext** | ✅ Manual | ✅ Manual | ✅ Automatic | ✅ Automatic |
| **Metrics API** | EventCounters | EventCounters | Meter (via fallback) | Meter |
| **Correlation** | AsyncLocal | AsyncLocal | AsyncLocal | AsyncLocal |
| **Configuration Hot Reload** | Manual FileWatcher | Manual FileWatcher | IOptionsMonitor | IOptionsMonitor |
| **Health Checks** | Custom | Custom | IHealthCheck | IHealthCheck |
| **DispatchProxy** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| **Source Generators** | ❌ No | ❌ No | ⚠️ Limited | ✅ Yes |
| **Span\<T\> Optimizations** | ❌ No | ❌ No | ✅ Yes | ✅ Yes |

## Metrics API Differences

### .NET 8+ (Meter API)

```csharp
using System.Diagnostics.Metrics;

public class OrderMetrics
{
    private static readonly Meter Meter = new Meter("MyApp.Orders");
    private static readonly Counter<long> OrdersProcessed = 
        Meter.CreateCounter<long>("orders.processed");
    
    public void RecordOrder()
    {
        OrdersProcessed.Add(1, new KeyValuePair<string, object?>("status", "success"));
    }
}
```

**Characteristics**:
- Native OpenTelemetry support
- Low allocation (ValueTask-based)
- Full async support
- Better tooling integration

### .NET Framework 4.8 (EventCounters)

```csharp
using System.Diagnostics.Tracing;

[EventSource(Name = "MyApp.Orders")]
public sealed class OrderMetrics : EventSource
{
    public static readonly OrderMetrics Instance = new OrderMetrics();
    
    private EventCounter _ordersProcessedCounter;
    
    private OrderMetrics()
    {
        _ordersProcessedCounter = new EventCounter("orders.processed", this);
    }
    
    public void RecordOrder()
    {
        _ordersProcessedCounter.WriteMetric(1);
    }
    
    protected override void Dispose(bool disposing)
    {
        _ordersProcessedCounter?.Dispose();
        base.Dispose(disposing);
    }
}
```

**Characteristics**:
- ETW integration on Windows
- Requires explicit disposal
- Less flexible aggregation
- Good performance, but higher ceremony

### Runtime Detection

HVO.Enterprise automatically detects the platform and uses the appropriate metrics API:

```csharp
// Library code - you write once, runs everywhere
public class TelemetryMetrics
{
    public void RecordDuration(TimeSpan duration)
    {
        // Automatically uses Meter on .NET 6+ or EventCounters on older platforms
        MetricsAdapter.RecordDuration("operation.duration", duration);
    }
}
```

## Configuration Differences

### .NET 8+ (IConfiguration + IOptionsMonitor)

```csharp
// Automatic hot reload support
builder.Services.Configure<TelemetryOptions>(
    builder.Configuration.GetSection("Telemetry"));

// Options automatically update when appsettings.json changes
```

### .NET Framework 4.8 (Manual FileWatcher)

```csharp
// Requires explicit file watcher setup
Telemetry.Initialize(options =>
{
    options.EnableConfigurationHotReload = true;
    options.ConfigurationFilePath = "telemetry.json";
});

// Library monitors file changes and reloads automatically
```

## Threading Model Differences

### Async/Await Correlation

**All Platforms**: AsyncLocal-based correlation works identically

```csharp
// Works everywhere - correlation flows across await
using var scope = CorrelationScope.Create();
await ProcessAsync(); // Correlation ID flows automatically
```

### Thread Pool Behavior

**Modern .NET (.NET 5+)**:
- Better thread pool scaling
- Faster warm-up times
- Background processing more responsive

**.NET Framework 4.8**:
- Conservative thread pool growth
- May need tuning for high-throughput scenarios
- Use `ThreadPool.SetMinThreads()` if needed

## HTTP Instrumentation Differences

### .NET 8+ (HttpClientFactory + DiagnosticListener)

```csharp
builder.Services
    .AddHttpClient("orders")
    .AddTelemetry(); // Automatic W3C header injection
```

### .NET Framework 4.8 (DelegatingHandler)

```csharp
var client = new HttpClient(new TelemetryHttpMessageHandler())
{
    BaseAddress = new Uri("https://api.example.com")
};
```

## Performance Characteristics

### Memory Allocation

| Operation | .NET Framework 4.8 | .NET 8+ |
|-----------|-------------------|---------|
| Activity Creation | ~200 bytes | ~120 bytes |
| Scope Creation | ~150 bytes | ~80 bytes |
| Metric Recording | ~50 bytes | ~0 bytes (Span) |
| Queue Item | ~100 bytes | ~60 bytes |

### Throughput

| Scenario | .NET Framework 4.8 | .NET 8+ |
|----------|-------------------|---------|
| Activity Start/Stop | ~800K/sec | ~1.2M/sec |
| Metric Recording | ~1M/sec | ~2M/sec |
| Queue Processing | ~8K/sec | ~12K/sec |

## IIS Integration Differences

### .NET Framework 4.8 (IRegisteredObject)

```csharp
// Full IIS integration available
public class Startup
{
    protected void Application_Start()
    {
        Telemetry.Initialize(options =>
        {
            options.EnableIisIntegration = true; // Auto-registers with HostingEnvironment
        });
    }
}
```

### .NET 8+ (IHostApplicationLifetime)

```csharp
// Uses modern host lifetime
builder.Services.AddTelemetry(); // Automatically hooks IHostApplicationLifetime
```

## Best Practices by Platform

### .NET Framework 4.8

1. **Always call Telemetry.Shutdown()** in Application_End
2. **Use ThreadPool tuning** for high-throughput scenarios
3. **Monitor EventCounters** via PerfView or dotnet-counters
4. **Test AppDomain unload** scenarios carefully
5. **Use IIS integration** for proper lifecycle management

### .NET 8+

1. **Use DI-based initialization** with AddTelemetry()
2. **Leverage IHostApplicationLifetime** for shutdown
3. **Use System.Text.Json** for configuration
4. **Enable health checks** for monitoring
5. **Use OpenTelemetry exporters** for observability platforms

## Migration Path

### From .NET Framework 4.8 to .NET 8+

1. **Replace static initialization** with DI:
   ```csharp
   // Before (.NET Framework)
   Telemetry.Initialize(options => { ... });
   
   // After (.NET 8+)
   builder.Services.AddTelemetry(options => { ... });
   ```

2. **Update lifecycle management**:
   ```csharp
   // Before
   Application_End() { Telemetry.Shutdown(); }
   
   // After - automatic via IHostApplicationLifetime
   ```

3. **Switch to ILogger** from custom logging:
   ```csharp
   // Before
   TelemetryLogger.Log("message");
   
   // After
   _logger.LogInformation("message"); // Auto-enriched with correlation
   ```

## Troubleshooting

### Issue: Metrics not appearing (.NET Framework 4.8)

**Solution**: Enable EventCounters in your monitoring tool:
```bash
dotnet-counters monitor --process-id <PID> --counters MyApp.Orders
```

### Issue: Correlation lost across async boundaries

**Solution**: Ensure using latest Activity API:
```csharp
// Use Activity.Current, not CallContext
var currentActivity = Activity.Current;
```

### Issue: High memory usage on .NET Framework 4.8

**Solution**: Tune queue size and processing rate:
```csharp
Telemetry.Initialize(options =>
{
    options.QueueCapacity = 5000; // Reduce from default 10000
    options.MaxBatchSize = 50;    // Process smaller batches
});
```

## See Also

- [Migration Guide](./MIGRATION.md)
- [Architecture Document](./ARCHITECTURE.md)
- [Performance Tuning Guide](./performance-tuning.md)
```

### MIGRATION.md Content

```markdown
# Migration Guide

This guide helps you migrate from existing telemetry and logging libraries to HVO.Enterprise.Telemetry.

## Table of Contents

1. [From Application Insights](#from-application-insights)
2. [From Serilog](#from-serilog)
3. [From NLog](#from-nlog)
4. [From Custom Telemetry](#from-custom-telemetry)
5. [From OpenTelemetry SDK](#from-opentelemetry-sdk)
6. [Testing During Migration](#testing-during-migration)

## From Application Insights

### Before (Application Insights SDK)

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetry(options =>
        {
            options.InstrumentationKey = "your-key";
        });
    }
}

public class OrderService
{
    private readonly TelemetryClient _telemetry;
    
    public OrderService(TelemetryClient telemetry)
    {
        _telemetry = telemetry;
    }
    
    public async Task ProcessOrderAsync(Order order)
    {
        using var operation = _telemetry.StartOperation<RequestTelemetry>("ProcessOrder");
        operation.Telemetry.Properties["orderId"] = order.Id.ToString();
        
        try
        {
            await _processor.ProcessAsync(order);
            operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
            operation.Telemetry.Success = false;
            throw;
        }
    }
}
```

### After (HVO.Enterprise.Telemetry)

```csharp
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Extensions;
using System.Diagnostics;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Core telemetry
        services.AddTelemetry(options =>
        {
            options.ServiceName = "OrderService";
            options.EnableAutoInstrumentation = true;
        });
        
        // Optional: Bridge to Application Insights
        services.AddApplicationInsightsTelemetryBridge(options =>
        {
            options.InstrumentationKey = "your-key";
        });
    }
}

public class OrderService
{
    private static readonly ActivitySource ActivitySource = 
        new ActivitySource("OrderService");
    
    public async Task ProcessOrderAsync(Order order)
    {
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("orderId", order.Id);
        
        try
        {
            await _processor.ProcessAsync(order);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Migration Steps

1. **Install packages**:
   ```bash
   dotnet add package HVO.Enterprise.Telemetry
   dotnet add package HVO.Enterprise.Telemetry.AppInsights
   ```

2. **Update initialization** from `AddApplicationInsightsTelemetry()` to `AddTelemetry()`

3. **Replace TelemetryClient** with ActivitySource pattern

4. **Update custom properties** from `operation.Telemetry.Properties[]` to `activity.SetTag()`

5. **Replace exception tracking** from `TrackException()` to `activity.RecordException()`

6. **Test** - Bridge ensures data still flows to Application Insights

### Key Differences

| Application Insights | HVO.Enterprise.Telemetry |
|---------------------|-------------------------|
| `TelemetryClient` | `ActivitySource` |
| `StartOperation<T>()` | `StartActivity()` |
| `Properties[]` | `SetTag()` |
| `TrackException()` | `RecordException()` |
| `Success = true` | `SetStatus(ActivityStatusCode.Ok)` |

## From Serilog

### Before (Serilog)

```csharp
using Serilog;
using Serilog.Context;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/app.log")
            .CreateLogger();
            
        services.AddLogging(builder => builder.AddSerilog());
    }
}

public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public async Task ProcessOrderAsync(Order order)
    {
        using (LogContext.PushProperty("OrderId", order.Id))
        using (LogContext.PushProperty("CorrelationId", Guid.NewGuid()))
        {
            _logger.LogInformation("Processing order {OrderId}", order.Id);
            
            var sw = Stopwatch.StartNew();
            await _processor.ProcessAsync(order);
            sw.Stop();
            
            _logger.LogInformation("Order processed in {Duration}ms", sw.ElapsedMilliseconds);
        }
    }
}
```

### After (HVO.Enterprise.Telemetry + Serilog)

```csharp
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Extensions;
using Serilog;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // HVO telemetry
        services.AddTelemetry(options =>
        {
            options.ServiceName = "OrderService";
            options.EnableAutoInstrumentation = true;
        });
        
        // Serilog with HVO enrichers
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithTelemetryContext() // HVO enricher - adds correlation, activity
            .WriteTo.Console()
            .WriteTo.File("logs/app.log")
            .CreateLogger();
            
        services.AddLogging(builder => builder.AddSerilog());
    }
}

public class OrderService
{
    private static readonly ActivitySource ActivitySource = 
        new ActivitySource("OrderService");
    
    private readonly ILogger<OrderService> _logger;
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Activity creates correlation automatically
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("orderId", order.Id);
        
        // Correlation flows to logs automatically via enricher
        _logger.LogInformation("Processing order {OrderId}", order.Id);
        
        await _processor.ProcessAsync(order);
        
        _logger.LogInformation("Order processed in {Duration}ms", 
            activity?.Duration.TotalMilliseconds);
    }
}
```

### Migration Steps

1. **Install packages**:
   ```bash
   dotnet add package HVO.Enterprise.Telemetry
   dotnet add package HVO.Enterprise.Telemetry.Serilog
   ```

2. **Add HVO telemetry** alongside existing Serilog

3. **Add enricher** `.Enrich.WithTelemetryContext()` to Serilog configuration

4. **Replace manual correlation** with automatic Activity-based correlation

5. **Remove LogContext.PushProperty** for correlation (now automatic)

6. **Keep using ILogger** - now automatically enriched with telemetry context

### Benefits

- **Automatic correlation** - no manual `LogContext.PushProperty` needed
- **Structured timing** - Activity.Duration instead of manual Stopwatch
- **Distributed tracing** - W3C TraceContext propagates across services
- **Keep Serilog** - continue using existing sinks and configuration

## From NLog

### Before (NLog)

```csharp
using NLog;
using NLog.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        
        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Application failed to start");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}

public class OrderService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task ProcessOrderAsync(Order order)
    {
        var correlationId = Guid.NewGuid();
        MappedDiagnosticsLogicalContext.Set("CorrelationId", correlationId);
        
        Logger.Info("Processing order {0}", order.Id);
        
        var sw = Stopwatch.StartNew();
        await _processor.ProcessAsync(order);
        sw.Stop();
        
        Logger.Info("Order processed in {0}ms", sw.ElapsedMilliseconds);
        
        MappedDiagnosticsLogicalContext.Remove("CorrelationId");
    }
}
```

### After (HVO.Enterprise.Telemetry + NLog)

```csharp
using HVO.Enterprise.Telemetry;
using Microsoft.Extensions.Hosting;
using NLog.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        
        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Application failed to start");
            throw;
        }
        finally
        {
            Telemetry.Shutdown();
            LogManager.Shutdown();
        }
    }
    
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddTelemetry(options =>
                {
                    options.ServiceName = "OrderService";
                });
            })
            .UseNLog();
}

public class OrderService
{
    private static readonly ActivitySource ActivitySource = 
        new ActivitySource("OrderService");
    
    private readonly ILogger<OrderService> _logger;
    
    public async Task ProcessOrderAsync(Order order)
    {
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("orderId", order.Id);
        
        // Correlation automatic via Activity.Current
        _logger.LogInformation("Processing order {OrderId}", order.Id);
        
        await _processor.ProcessAsync(order);
        
        _logger.LogInformation("Order processed in {Duration}ms", 
            activity?.Duration.TotalMilliseconds);
    }
}
```

**NLog Configuration** (nlog.config):

```xml
<nlog>
  <extensions>
    <add assembly="HVO.Enterprise.Telemetry.NLog" />
  </extensions>
  
  <targets>
    <target name="console" xsi:type="Console"
            layout="${longdate}|${level}|${activityid}|${tracecontext}|${message} ${exception}" />
  </targets>
  
  <rules>
    <logger name="*" minlevel="Info" writeTo="console" />
  </rules>
</nlog>
```

### Migration Steps

1. **Install packages**:
   ```bash
   dotnet add package HVO.Enterprise.Telemetry
   ```

2. **Add telemetry** to host configuration

3. **Update NLog config** to include Activity context (`${activityid}`, `${tracecontext}`)

4. **Replace MDLC** with Activity tags

5. **Use ILogger** instead of NLog Logger directly (optional but recommended)

## From Custom Telemetry

### Before (Custom Implementation)

```csharp
public class CustomTelemetry
{
    private static readonly AsyncLocal<string> CorrelationId = new AsyncLocal<string>();
    
    public static IDisposable StartOperation(string operationName)
    {
        var id = Guid.NewGuid().ToString();
        CorrelationId.Value = id;
        
        var sw = Stopwatch.StartNew();
        
        LogToFile($"START {operationName} [{id}]");
        
        return new OperationScope(operationName, id, sw);
    }
    
    private class OperationScope : IDisposable
    {
        private readonly string _name;
        private readonly string _id;
        private readonly Stopwatch _stopwatch;
        
        public OperationScope(string name, string id, Stopwatch sw)
        {
            _name = name;
            _id = id;
            _stopwatch = sw;
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            LogToFile($"END {_name} [{_id}] {_stopwatch.ElapsedMilliseconds}ms");
        }
    }
    
    private static void LogToFile(string message)
    {
        File.AppendAllText("telemetry.log", 
            $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
    }
}

// Usage
using (CustomTelemetry.StartOperation("ProcessOrder"))
{
    await ProcessAsync();
}
```

### After (HVO.Enterprise.Telemetry)

```csharp
using HVO.Enterprise.Telemetry;
using System.Diagnostics;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTelemetry(options =>
        {
            options.ServiceName = "OrderService";
            options.EnableAutoInstrumentation = true;
        });
        
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddFile("telemetry.log"); // Or any logger
        });
    }
}

// Usage - much simpler, more powerful
private static readonly ActivitySource ActivitySource = 
    new ActivitySource("OrderService");

using var activity = ActivitySource.StartActivity("ProcessOrder");
await ProcessAsync();
// Automatic timing, correlation, distributed tracing, metrics
```

### Migration Benefits

| Custom Telemetry | HVO.Enterprise.Telemetry |
|-----------------|-------------------------|
| Manual correlation ID | Automatic via AsyncLocal |
| Manual timing | Automatic via Activity |
| File logging only | Any logger + exporters |
| No distributed tracing | W3C TraceContext support |
| No metrics | Built-in metrics support |
| Manual scope management | Automatic with using pattern |

## From OpenTelemetry SDK

If you're already using OpenTelemetry SDK directly, HVO.Enterprise provides a higher-level API with less ceremony:

### Before (OpenTelemetry SDK)

```csharp
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("OrderService"))
                    .AddSource("OrderService")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter();
            });
    }
}
```

### After (HVO.Enterprise.Telemetry)

```csharp
using HVO.Enterprise.Telemetry;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTelemetry(options =>
        {
            options.ServiceName = "OrderService";
            options.EnableAutoInstrumentation = true;
            options.AddOtlpExporter(endpoint => "http://collector:4317");
        });
    }
}
```

**Benefits**: Simpler configuration, .NET Framework compatibility, automatic instrumentation

## Testing During Migration

### Dual-Write Strategy

Run both old and new telemetry side-by-side during migration:

```csharp
public class OrderService
{
    private readonly TelemetryClient _legacyTelemetry;
    private static readonly ActivitySource ActivitySource = new ActivitySource("OrderService");
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Old telemetry (will be removed)
        using var legacyOp = _legacyTelemetry.StartOperation<RequestTelemetry>("ProcessOrder");
        
        // New telemetry (keeping)
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        
        try
        {
            await _processor.ProcessAsync(order);
            
            legacyOp.Telemetry.Success = true;
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            _legacyTelemetry.TrackException(ex);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Validation Checklist

- [ ] Correlation IDs flow correctly across async boundaries
- [ ] Distributed traces connect properly across services
- [ ] Exception details captured completely
- [ ] Custom properties/tags migrated correctly
- [ ] Performance overhead acceptable
- [ ] Logs enriched with correlation context
- [ ] Metrics recording correctly
- [ ] Health checks working
- [ ] Graceful shutdown functioning

### Rollback Plan

Keep old telemetry code commented but present:

```csharp
public async Task ProcessOrderAsync(Order order)
{
    // MIGRATION: Using HVO telemetry (can rollback to Application Insights if needed)
    using var activity = ActivitySource.StartActivity("ProcessOrder");
    activity?.SetTag("orderId", order.Id);
    
    /* OLD CODE - Remove after 2 weeks if no issues
    using var operation = _telemetry.StartOperation<RequestTelemetry>("ProcessOrder");
    operation.Telemetry.Properties["orderId"] = order.Id.ToString();
    */
    
    await _processor.ProcessAsync(order);
}
```

## Common Pitfalls

### 1. Forgetting to call Telemetry.Shutdown()

**Problem**: Telemetry data lost on app shutdown

**Solution**:
```csharp
// .NET Framework
protected void Application_End()
{
    Telemetry.Shutdown();
}

// .NET 8+ - automatic via IHostApplicationLifetime
```

### 2. Not awaiting async operations

**Problem**: Correlation lost in fire-and-forget scenarios

**Solution**:
```csharp
// BAD
_ = Task.Run(() => ProcessAsync());

// GOOD
await Task.Run(() => ProcessAsync());
```

### 3. Mixing Activity.Current and custom correlation

**Problem**: Correlation IDs mismatch between systems

**Solution**: Use Activity.Current everywhere, don't mix with custom AsyncLocal

### 4. Not testing on target platform

**Problem**: .NET Framework behavioral differences discovered in production

**Solution**: Test on both .NET Framework 4.8 and .NET 8 before deployment

## Next Steps

1. Review [Platform Differences](./DIFFERENCES.md)
2. Understand [Architecture](./ARCHITECTURE.md)
3. Check [API Documentation](./api/index.html)
4. See [Sample Applications](../samples/)

## Support

- GitHub Issues: https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues
- Documentation: https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/docs
```

### ARCHITECTURE.md Outline

```markdown
# Architecture Overview

## System Design Principles

1. **Performance First** - <100ns overhead per operation
2. **Zero Breaking Changes** - Backward compatibility guaranteed
3. **Platform Adaptive** - Runtime feature detection
4. **Extensible** - Open for extension, closed for modification
5. **Battle-Tested Patterns** - Proven in production environments

## Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Code                         │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│              Public API Layer                                │
│  • ITelemetryService                                        │
│  • ActivitySource extensions                                │
│  • IOperationScope                                          │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│           Instrumentation Layer                              │
│  • DispatchProxy (auto-instrumentation)                     │
│  • TelemetryHttpMessageHandler                              │
│  • Database interceptors                                    │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│              Correlation Layer                               │
│  • CorrelationManager (AsyncLocal)                          │
│  • Activity.Current integration                             │
│  • W3C TraceContext propagation                             │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│            Processing Layer                                  │
│  • BoundedQueueWorker (Channel-based)                       │
│  • Batch processing                                         │
│  • Backpressure handling                                    │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│          Platform Adaptation Layer                           │
│  • Metrics: Meter API ↔ EventCounters                      │
│  • Configuration: IOptionsMonitor ↔ FileWatcher             │
│  • Lifecycle: IHostApplicationLifetime ↔ IRegisteredObject  │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│               Export Layer                                   │
│  • OTLP Exporters                                           │
│  • Platform bridges (AppInsights, Datadog)                  │
│  • File/Console exporters                                   │
└─────────────────────────────────────────────────────────────┘
```

[Detailed sections with code examples for each layer...]

## Threading Model

[Details on AsyncLocal, thread pool usage, synchronization...]

## Performance Optimizations

[Details on zero-allocation paths, object pooling, etc...]

## Extension Points

[Details on IMethodInstrumentationStrategy, custom exporters, etc...]

## Security Considerations

[PII handling, sensitive data redaction, etc...]
```

### ROADMAP.md Outline

```markdown
# Product Roadmap

## Version 1.0 (Q2 2026) - Foundation

**Status**: 🚧 In Development

- [x] Core package with .NET Standard 2.0 support
- [x] AsyncLocal-based correlation
- [x] Bounded queue with Channel-based worker
- [ ] Runtime-adaptive metrics (Meter/EventCounters)
- [ ] Operation scope with timing
- [ ] ILogger enrichment
- [ ] Basic extensions (IIS, WCF, Database)

## Version 1.1 (Q3 2026) - Advanced Features

- [ ] DispatchProxy automatic instrumentation
- [ ] Parameter capture with sensitivity detection
- [ ] Configuration hot reload
- [ ] Exception tracking and aggregation
- [ ] Health checks integration
- [ ] Performance optimizations (Span<T>, object pooling)

## Version 2.0 (Q4 2026) - Platform Maturity

- [ ] Source generators for compile-time instrumentation
- [ ] Advanced sampling strategies
- [ ] Multi-exporter support
- [ ] Enhanced observability platform integrations
- [ ] Comprehensive documentation and samples

## Version 3.0 (2027) - Future Vision

- [ ] AI-powered anomaly detection
- [ ] Automatic baseline learning
- [ ] Predictive performance analysis
- [ ] Enhanced visualization tools

## Feature Requests

[Community-requested features with voting...]

## Breaking Change Policy

[Version compatibility guarantees...]

## Deprecation Schedule

[Features being phased out with timeline...]
```

## Testing Requirements

### Documentation Testing

1. **README Validation**
   - [ ] All code samples compile
   - [ ] All links work
   - [ ] Quick start can be completed in <10 minutes
   - [ ] Badges show correct status

2. **DIFFERENCES.md Validation**
   - [ ] Code samples tested on .NET Framework 4.8
   - [ ] Code samples tested on .NET 8
   - [ ] Performance numbers verified with benchmarks
   - [ ] Feature matrix matches actual behavior

3. **MIGRATION.md Validation**
   - [ ] Each migration path tested with real application
   - [ ] Code transformations verified to work
   - [ ] Dual-write strategy tested
   - [ ] Rollback procedures validated

4. **ARCHITECTURE.md Validation**
   - [ ] Component diagrams match implementation
   - [ ] Code samples compile
   - [ ] Performance characteristics verified

5. **API Documentation**
   - [ ] All public APIs have XML documentation
   - [ ] XML documentation includes code examples
   - [ ] DocFX builds without errors
   - [ ] All links in documentation work

### Integration Testing

1. **Sample Application Validation**
   - [ ] README quick start works in clean environment
   - [ ] Migration guide examples work end-to-end
   - [ ] Platform-specific examples work on target platforms

2. **Link Checking**
   - [ ] All internal links resolve correctly
   - [ ] All external links are valid
   - [ ] No broken references

## Performance Requirements

### Documentation Build

- **Time**: <30 seconds for full DocFX build
- **Size**: <10MB for generated documentation
- **Search**: <200ms for documentation search queries

## Dependencies

**Blocked By**: 
- US-001: Core Package Setup
- US-002: Auto-Managed Correlation
- US-004: Bounded Queue
- US-012: Operation Scope
- All extension package stories (US-020 through US-025)

**Blocks**: None (final documentation story)

## Definition of Done

- [ ] README.md complete with quick start and badges
- [ ] DIFFERENCES.md documents all platform variations
- [ ] MIGRATION.md covers all major telemetry libraries
- [ ] ARCHITECTURE.md explains system design
- [ ] ROADMAP.md shows future direction
- [ ] All public APIs have comprehensive XML documentation
- [ ] DocFX or similar API reference site generated
- [ ] All code samples tested and working
- [ ] All links validated
- [ ] Documentation reviewed and approved
- [ ] Committed to main branch

## Notes

### Design Decisions

1. **Why separate DIFFERENCES.md from README?**
   - README focused on quick start
   - DIFFERENCES provides deep technical comparison
   - Separation keeps README concise

2. **Why comprehensive MIGRATION.md?**
   - Most users migrating from existing solutions
   - Step-by-step guides reduce migration friction
   - Examples show before/after clearly

3. **Why include ARCHITECTURE.md?**
   - Helps advanced users understand internals
   - Enables confident customization
   - Documents design decisions for maintainers

4. **Why ROADMAP.md?**
   - Sets expectations for future features
   - Shows commitment to long-term support
   - Helps users plan adoption timeline

### Implementation Tips

1. **Start with README** - Foundation for other docs
2. **Use real examples** - Extract from sample applications
3. **Test all code** - Every sample must compile and run
4. **Get feedback** - Have users review migration guide
5. **Keep updated** - Documentation is never "done"

### Documentation Tools

1. **DocFX** - Recommended for API reference
2. **Mermaid** - For architecture diagrams
3. **markdown-link-check** - Validate all links
4. **markdownlint** - Ensure consistent formatting
5. **code-fence-validator** - Verify code samples compile

### Common Pitfalls

1. **Outdated examples** - Keep synchronized with code
2. **Missing platform differences** - Test on all targets
3. **Broken links** - Run link checker regularly
4. **Unclear migration** - Get feedback from actual users
5. **Missing edge cases** - Document common troubleshooting

### Future Enhancements

- Interactive API documentation
- Video tutorials for migration
- Architecture decision records (ADR)
- Performance tuning cookbook
- Troubleshooting decision tree

## Related Documentation

- [Project Plan](../project-plan.md#29-create-comprehensive-documentation)
- [Sample Applications](../samples/)
- [API Reference](./api/index.html)
- [Contributing Guidelines](../../CONTRIBUTING.md)
