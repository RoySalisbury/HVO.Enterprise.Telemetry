# US-028: .NET 8 Sample Application

**GitHub Issue**: [#30](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/30)  
**Status**: ✅ Complete  
**Category**: Testing  
**Effort**: 13 story points  
**Sprint**: 10

## Description

As a **developer building modern .NET applications**,  
I want to **see a comprehensive .NET 8 sample demonstrating telemetry integration**,  
So that **I can understand how to integrate HVO.Enterprise.Telemetry into ASP.NET Core, gRPC, background services, and health checks**.

## Acceptance Criteria

1. **Project Structure**
   - [x] Sample solution includes ASP.NET Core Web API application
   - [ ] gRPC service with telemetry instrumentation (deferred — requires proto tooling and separate service)
   - [x] IHostedService background workers with correlation
   - [x] Health check endpoints with telemetry statistics
   - [x] Minimal API endpoints demonstrating modern patterns

2. **ASP.NET Core Integration**
   - [x] Program.cs with dependency injection setup
   - [x] Middleware for automatic request instrumentation
   - [x] Controller and Minimal API examples
   - [ ] OpenTelemetry exporter integration (deferred — OTel packages not yet part of the library)
   - [x] Built-in logging integration

3. **gRPC Integration** (deferred to future story)
   - [ ] gRPC interceptor for automatic instrumentation
   - [ ] Metadata propagation for correlation
   - [ ] Streaming support (unary, server, client, bidirectional)
   - [ ] Error handling with telemetry
   - [ ] W3C TraceContext in gRPC metadata

4. **Background Services**
   - [x] IHostedService with periodic execution
   - [x] BackgroundService with cancellation support
   - [x] Correlation propagation to background work
   - [ ] Queue-based processing patterns (deferred)
   - [x] Graceful shutdown handling

5. **Health Checks**
   - [x] Liveness endpoint with telemetry status
   - [x] Readiness endpoint with dependency checks
   - [x] Custom health check for telemetry statistics
   - [x] Health check UI integration
   - [ ] Prometheus metrics endpoint (deferred — requires OTel)

6. **Modern .NET Features**
   - [ ] Native AOT compatibility notes
   - [x] Minimal API patterns
   - [x] Top-level statements
   - [ ] Source generators (if applicable)
   - [x] Record types for DTOs

## Technical Requirements

### Solution Structure

```
HVO.Enterprise.Samples.Net8/
├── HVO.Enterprise.Samples.Net8.WebApi/          # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── WeatherController.cs
│   │   └── OrdersController.cs
│   ├── Endpoints/
│   │   └── MinimalEndpoints.cs
│   ├── Middleware/
│   │   └── CorrelationMiddleware.cs
│   ├── Services/
│   │   ├── OrderService.cs
│   │   └── EmailService.cs
│   ├── BackgroundServices/
│   │   ├── MetricsCollectorService.cs
│   │   └── QueueProcessorService.cs
│   ├── HealthChecks/
│   │   └── TelemetryHealthCheck.cs
│   ├── Program.cs
│   └── appsettings.json
├── HVO.Enterprise.Samples.Net8.Grpc/            # gRPC Service
│   ├── Services/
│   │   └── CustomerService.cs
│   ├── Interceptors/
│   │   └── TelemetryInterceptor.cs
│   ├── Protos/
│   │   └── customer.proto
│   ├── Program.cs
│   └── appsettings.json
├── HVO.Enterprise.Samples.Net8.Worker/          # Worker Service
│   ├── Workers/
│   │   ├── DataProcessorWorker.cs
│   │   └── ReportGeneratorWorker.cs
│   ├── Program.cs
│   └── appsettings.json
└── HVO.Enterprise.Samples.Net8.sln
```

### 1. Program.cs - Modern Startup with Dependency Injection

```csharp
using System.Diagnostics;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Samples.Net8.WebApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure telemetry with DI
builder.Services.AddTelemetry(options =>
{
    options.ServiceName = "HVO.Samples.Net8.WebApi";
    options.ServiceVersion = "1.0.0";
    options.EnableCorrelation = true;
    options.EnableMetrics = true;
    options.EnableTracing = true;
    options.QueueCapacity = 10000;
});

// Add OpenTelemetry integration
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddHvoEnterpriseInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri("http://localhost:4317");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddHvoEnterpriseInstrumentation()
            .AddPrometheusExporter();
    });

// Add controllers and API explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks()
    .AddTelemetryHealthCheck()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<ExternalApiHealthCheck>("external-api");

// Add application services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add background services
builder.Services.AddHostedService<MetricsCollectorService>();
builder.Services.AddHostedService<QueueProcessorService>();

// Add HTTP client with telemetry
builder.Services.AddHttpClient("ExternalApi")
    .AddTelemetryHandler();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add correlation middleware (runs early in pipeline)
app.UseCorrelation();

app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map minimal API endpoints
app.MapMinimalEndpoints();

// Map health checks
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse
});

// Map Prometheus metrics
app.MapPrometheusScrapingEndpoint();

// Run application
await app.RunAsync();
```

### 2. Correlation Middleware

```csharp
using System.Diagnostics;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net8.WebApi.Middleware;

/// <summary>
/// Middleware that manages correlation IDs for all requests.
/// </summary>
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract or generate correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Activity.Current?.TraceId.ToString()
            ?? CorrelationManager.GenerateNewCorrelationId();

        CorrelationManager.CurrentCorrelationId = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd("X-Correlation-ID", correlationId);
            return Task.CompletedTask;
        });

        // Add to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? "none"
        }))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension methods for correlation middleware registration.
/// </summary>
public static class CorrelationMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationMiddleware>();
    }
}
```

### 3. Controller with Telemetry

```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net8.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        ITelemetryService telemetry,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _telemetry = telemetry;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        using var scope = _telemetry.CreateOperationScope("GetOrders");
        
        try
        {
            var orders = await _orderService.GetOrdersAsync();
            scope.AddProperty("orderCount", orders.Count());
            
            return Ok(orders);
        }
        catch (Exception ex)
        {
            scope.SetStatus(ActivityStatusCode.Error, ex.Message);
            _telemetry.TrackException(ex);
            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(Guid id)
    {
        using var scope = _telemetry.CreateOperationScope("GetOrder");
        scope.AddProperty("orderId", id);

        var order = await _orderService.GetOrderByIdAsync(id);
        
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", id);
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        using var scope = _telemetry.CreateOperationScope("CreateOrder");
        scope.AddProperty("customerName", request.CustomerName);
        scope.AddProperty("itemCount", request.Items.Count);

        try
        {
            var order = await _orderService.CreateOrderAsync(request);
            
            _telemetry.RecordMetric("orders_created_total", 1, new[]
            {
                new KeyValuePair<string, object>("customer", request.CustomerName)
            });

            _logger.LogInformation(
                "Order {OrderId} created for customer {CustomerName}",
                order.Id,
                request.CustomerName);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            scope.SetStatus(ActivityStatusCode.Error, ex.Message);
            _telemetry.TrackException(ex, new { CustomerName = request.CustomerName });
            throw;
        }
    }
}

public record Order(Guid Id, string CustomerName, decimal Total, DateTimeOffset CreatedAt);
public record CreateOrderRequest(string CustomerName, List<OrderItem> Items);
public record OrderItem(string ProductId, int Quantity, decimal Price);
```

### 4. Minimal API Endpoints

```csharp
using System.Diagnostics;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net8.WebApi.Endpoints;

public static class MinimalEndpoints
{
    public static void MapMinimalEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/minimal");

        // Simple GET endpoint
        group.MapGet("/hello", (ITelemetryService telemetry) =>
        {
            using var scope = telemetry.CreateOperationScope("MinimalHello");
            return Results.Ok(new { Message = "Hello from minimal API!" });
        })
        .WithName("Hello")
        .WithOpenApi();

        // Endpoint with route parameters
        group.MapGet("/greet/{name}", (string name, ITelemetryService telemetry) =>
        {
            using var scope = telemetry.CreateOperationScope("MinimalGreet");
            scope.AddProperty("name", name);
            
            return Results.Ok(new { Message = $"Hello, {name}!" });
        })
        .WithName("Greet")
        .WithOpenApi();

        // Async endpoint with services
        group.MapPost("/process", async (
            ProcessRequest request,
            IOrderService orderService,
            ITelemetryService telemetry,
            ILogger<Program> logger) =>
        {
            using var scope = telemetry.CreateOperationScope("MinimalProcess");
            scope.AddProperty("requestId", request.Id);

            try
            {
                await orderService.ProcessAsync(request.Id);
                logger.LogInformation("Processed request {RequestId}", request.Id);
                
                return Results.Ok(new { Success = true, RequestId = request.Id });
            }
            catch (Exception ex)
            {
                scope.SetStatus(ActivityStatusCode.Error, ex.Message);
                telemetry.TrackException(ex);
                return Results.Problem(ex.Message);
            }
        })
        .WithName("Process")
        .WithOpenApi();
    }
}

public record ProcessRequest(string Id, string Data);
```

### 5. gRPC Service with Interceptor

```protobuf
// customer.proto
syntax = "proto3";

option csharp_namespace = "HVO.Enterprise.Samples.Net8.Grpc";

service CustomerService {
  rpc GetCustomer (GetCustomerRequest) returns (Customer);
  rpc ListCustomers (ListCustomersRequest) returns (stream Customer);
  rpc CreateCustomer (CreateCustomerRequest) returns (Customer);
}

message GetCustomerRequest {
  string customer_id = 1;
}

message ListCustomersRequest {
  int32 page_size = 1;
  string page_token = 2;
}

message CreateCustomerRequest {
  string name = 1;
  string email = 2;
}

message Customer {
  string id = 1;
  string name = 2;
  string email = 3;
}
```

```csharp
using Grpc.Core;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net8.Grpc.Services;

public class CustomerService : Grpc.CustomerService.CustomerServiceBase
{
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ITelemetryService telemetry, ILogger<CustomerService> logger)
    {
        _telemetry = telemetry;
        _logger = logger;
    }

    public override async Task<Customer> GetCustomer(
        GetCustomerRequest request,
        ServerCallContext context)
    {
        using var scope = _telemetry.CreateOperationScope("GetCustomer");
        scope.AddProperty("customerId", request.CustomerId);

        // Extract correlation from metadata
        var correlationId = context.RequestHeaders.GetValue("x-correlation-id")
            ?? CorrelationManager.CurrentCorrelationId;
        
        if (correlationId != null)
        {
            CorrelationManager.CurrentCorrelationId = correlationId;
        }

        _logger.LogInformation("Getting customer {CustomerId}", request.CustomerId);

        // Simulate database lookup
        await Task.Delay(50);

        return new Customer
        {
            Id = request.CustomerId,
            Name = "John Doe",
            Email = "john@example.com"
        };
    }

    public override async Task ListCustomers(
        ListCustomersRequest request,
        IServerStreamWriter<Customer> responseStream,
        ServerCallContext context)
    {
        using var scope = _telemetry.CreateOperationScope("ListCustomers");
        scope.AddProperty("pageSize", request.PageSize);

        var customers = GenerateCustomers(request.PageSize);
        var count = 0;

        foreach (var customer in customers)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("ListCustomers cancelled after {Count} customers", count);
                break;
            }

            await responseStream.WriteAsync(customer);
            count++;
            await Task.Delay(10); // Simulate streaming delay
        }

        scope.AddProperty("customersStreamed", count);
        _telemetry.RecordMetric("grpc_customers_streamed", count);
    }

    public override async Task<Customer> CreateCustomer(
        CreateCustomerRequest request,
        ServerCallContext context)
    {
        using var scope = _telemetry.CreateOperationScope("CreateCustomer");
        scope.AddProperty("customerName", request.Name);

        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required"));
        }

        // Simulate creation
        await Task.Delay(100);

        var customer = new Customer
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Email = request.Email
        };

        _logger.LogInformation("Created customer {CustomerId}", customer.Id);
        _telemetry.RecordMetric("grpc_customers_created_total", 1);

        return customer;
    }

    private IEnumerable<Customer> GenerateCustomers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new Customer
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Customer {i}",
                Email = $"customer{i}@example.com"
            };
        }
    }
}
```

gRPC Interceptor:

```csharp
using Grpc.Core;
using Grpc.Core.Interceptors;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net8.Grpc.Interceptors;

public class TelemetryInterceptor : Interceptor
{
    private readonly ITelemetryService _telemetry;

    public TelemetryInterceptor(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var method = context.Method;
        using var scope = _telemetry.CreateOperationScope($"gRPC {method}");

        // Extract correlation
        var correlationId = context.RequestHeaders.GetValue("x-correlation-id");
        if (!string.IsNullOrEmpty(correlationId))
        {
            CorrelationManager.CurrentCorrelationId = correlationId;
        }

        try
        {
            var response = await continuation(request, context);
            scope.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
            return response;
        }
        catch (RpcException ex)
        {
            scope.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            scope.AddProperty("grpc.status_code", ex.StatusCode);
            throw;
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var method = context.Method;
        using var scope = _telemetry.CreateOperationScope($"gRPC {method}");

        var messageCount = 0;
        scope.AddProperty("streaming.type", "client");

        // Wrap request stream to count messages
        var wrappedStream = new CountingStreamReader<TRequest>(requestStream, count =>
        {
            messageCount = count;
        });

        try
        {
            var response = await continuation(wrappedStream, context);
            scope.AddProperty("messages.received", messageCount);
            return response;
        }
        catch (Exception ex)
        {
            scope.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
```

### 6. Background Worker Service

```csharp
using System.Diagnostics;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net8.WebApi.BackgroundServices;

public class MetricsCollectorService : BackgroundService
{
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<MetricsCollectorService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public MetricsCollectorService(
        ITelemetryService telemetry,
        ILogger<MetricsCollectorService> logger)
    {
        _telemetry = telemetry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics collector service starting");

        // Create a new correlation context for background work
        var correlationId = CorrelationManager.GenerateNewCorrelationId();
        CorrelationManager.CurrentCorrelationId = correlationId;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _telemetry.CreateOperationScope("CollectMetrics");
            
            try
            {
                await CollectSystemMetrics(stoppingToken);
                await CollectApplicationMetrics(stoppingToken);

                scope.SetStatus(ActivityStatusCode.Ok);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Metrics collection cancelled");
                break;
            }
            catch (Exception ex)
            {
                scope.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error collecting metrics");
                _telemetry.TrackException(ex);
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Metrics collector service stopped");
    }

    private Task CollectSystemMetrics(CancellationToken cancellationToken)
    {
        // Collect system metrics
        var process = Process.GetCurrentProcess();
        
        _telemetry.RecordMetric("process_memory_bytes", process.WorkingSet64);
        _telemetry.RecordMetric("process_cpu_seconds", process.TotalProcessorTime.TotalSeconds);
        _telemetry.RecordMetric("process_threads", process.Threads.Count);

        var gc = GC.GetGCMemoryInfo();
        _telemetry.RecordMetric("gc_heap_size_bytes", gc.HeapSizeBytes);
        _telemetry.RecordMetric("gc_total_memory_bytes", GC.GetTotalMemory(false));

        return Task.CompletedTask;
    }

    private Task CollectApplicationMetrics(CancellationToken cancellationToken)
    {
        // Collect application-specific metrics
        var stats = _telemetry.GetStatistics();
        
        _telemetry.RecordMetric("telemetry_queue_size", stats.QueueSize);
        _telemetry.RecordMetric("telemetry_items_processed", stats.ItemsProcessed);
        _telemetry.RecordMetric("telemetry_items_dropped", stats.ItemsDropped);

        return Task.CompletedTask;
    }
}
```

Queue Processor Service:

```csharp
using System.Threading.Channels;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net8.WebApi.BackgroundServices;

public class QueueProcessorService : BackgroundService
{
    private readonly Channel<WorkItem> _queue;
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<QueueProcessorService> _logger;

    public QueueProcessorService(
        ITelemetryService telemetry,
        ILogger<QueueProcessorService> logger)
    {
        _telemetry = telemetry;
        _logger = logger;
        _queue = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    public async ValueTask<bool> EnqueueAsync(WorkItem item, CancellationToken cancellationToken = default)
    {
        // Capture current correlation context
        item.CorrelationId = CorrelationManager.CurrentCorrelationId;
        
        return await _queue.Writer.WriteAsync(item, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue processor service starting");

        await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            // Restore correlation context
            using var correlationScope = CorrelationManager.CreateScope(item.CorrelationId);
            using var operationScope = _telemetry.CreateOperationScope("ProcessWorkItem");
            
            operationScope.AddProperty("itemId", item.Id);
            operationScope.AddProperty("itemType", item.Type);

            try
            {
                await ProcessItemAsync(item, stoppingToken);
                
                _telemetry.RecordMetric("work_items_processed_total", 1, new[]
                {
                    new KeyValuePair<string, object>("type", item.Type),
                    new KeyValuePair<string, object>("status", "success")
                });
            }
            catch (Exception ex)
            {
                operationScope.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error processing work item {ItemId}", item.Id);
                _telemetry.TrackException(ex, new { ItemId = item.Id, ItemType = item.Type });
                
                _telemetry.RecordMetric("work_items_processed_total", 1, new[]
                {
                    new KeyValuePair<string, object>("type", item.Type),
                    new KeyValuePair<string, object>("status", "error")
                });
            }
        }

        _logger.LogInformation("Queue processor service stopped");
    }

    private async Task ProcessItemAsync(WorkItem item, CancellationToken cancellationToken)
    {
        // Simulate processing
        await Task.Delay(100, cancellationToken);
        
        _logger.LogInformation("Processed work item {ItemId} of type {ItemType}", item.Id, item.Type);
    }
}

public class WorkItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "default";
    public string? CorrelationId { get; set; }
    public object? Data { get; set; }
}
```

### 7. Health Checks

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net8.WebApi.HealthChecks;

public class TelemetryHealthCheck : IHealthCheck
{
    private readonly ITelemetryService _telemetry;

    public TelemetryHealthCheck(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = _telemetry.GetStatistics();
            
            var data = new Dictionary<string, object>
            {
                ["queue_size"] = stats.QueueSize,
                ["queue_capacity"] = stats.QueueCapacity,
                ["items_processed"] = stats.ItemsProcessed,
                ["items_dropped"] = stats.ItemsDropped,
                ["drop_rate"] = stats.ItemsProcessed > 0 
                    ? (double)stats.ItemsDropped / stats.ItemsProcessed 
                    : 0.0
            };

            // Check if queue is nearly full
            var utilization = (double)stats.QueueSize / stats.QueueCapacity;
            
            if (utilization > 0.9)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Telemetry queue is >90% full",
                    data: data));
            }

            // Check drop rate
            var dropRate = stats.ItemsProcessed > 0 
                ? (double)stats.ItemsDropped / stats.ItemsProcessed 
                : 0.0;

            if (dropRate > 0.05) // >5% drop rate
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Telemetry drop rate >5%",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "Telemetry system is healthy",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Error checking telemetry health",
                ex));
        }
    }
}

public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddTelemetryHealthCheck(this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<TelemetryHealthCheck>(
            "telemetry",
            tags: new[] { "ready" });
    }
}
```

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "HVO.Enterprise": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Telemetry": {
    "ServiceName": "HVO.Samples.Net8.WebApi",
    "ServiceVersion": "1.0.0",
    "EnableCorrelation": true,
    "EnableMetrics": true,
    "EnableTracing": true,
    "QueueCapacity": 10000,
    "BatchSize": 100,
    "FlushIntervalSeconds": 5,
    "Exporters": {
      "Console": {
        "Enabled": true
      },
      "OpenTelemetry": {
        "Enabled": true,
        "Endpoint": "http://localhost:4317",
        "Protocol": "grpc"
      },
      "Prometheus": {
        "Enabled": true,
        "Port": 9090
      }
    }
  },
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http1AndHttp2"
    }
  }
}
```

## Testing Requirements

### Manual Testing Scenarios

1. **ASP.NET Core Web API**
   - Call REST endpoints with curl/Postman
   - Verify correlation ID in response headers
   - Check W3C traceparent propagation
   - Test Swagger UI integration

2. **gRPC Service**
   - Use grpcurl or BloomRPC to call service
   - Test unary and streaming RPCs
   - Verify metadata propagation
   - Test cancellation scenarios

3. **Background Services**
   - Verify services start with application
   - Check metrics collection in logs
   - Enqueue work items, verify processing
   - Test graceful shutdown (Ctrl+C)

4. **Health Checks**
   - Call /health/live endpoint
   - Call /health/ready endpoint with details
   - Verify Prometheus metrics at /metrics
   - Check health status with high load

5. **OpenTelemetry Integration**
   - Start Jaeger/Zipkin locally
   - Verify traces appear in UI
   - Check span relationships
   - Verify metrics in Prometheus

### Integration Tests

```csharp
[Fact]
public async Task WebApi_ShouldPropagateCorrelationWithOpenTelemetry()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    var correlationId = Guid.NewGuid().ToString();
    
    client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

    // Act
    var response = await client.GetAsync("/api/orders");

    // Assert
    response.Headers.GetValues("X-Correlation-ID").Should().Contain(correlationId);
    response.Headers.GetValues("traceparent").Should().NotBeEmpty();
}

[Fact]
public async Task HealthCheck_ShouldReturnTelemetryStatistics()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();

    // Act
    var response = await client.GetAsync("/health/ready");
    var content = await response.Content.ReadAsStringAsync();

    // Assert
    response.IsSuccessStatusCode.Should().BeTrue();
    content.Should().Contain("telemetry");
    content.Should().Contain("queue_size");
}
```

## Performance Requirements

- **Middleware Overhead**: <2ms per request
- **gRPC Interceptor Overhead**: <1ms per call
- **Background Service Impact**: <5% CPU usage
- **Health Check Response Time**: <50ms
- **Memory Impact**: <50MB additional for telemetry

## Dependencies

**Blocked By**: 
- US-001 through US-018 (core telemetry features)
- US-026 (testing patterns established)

**Blocks**: 
- US-029 (documentation references this sample)

## Definition of Done

- [x] All projects created and building
- [x] ASP.NET Core Web API with full telemetry integration
- [ ] gRPC service with interceptor implementation (deferred)
- [x] Background services with correlation propagation
- [x] Health checks with telemetry statistics
- [ ] OpenTelemetry integration working (deferred — not yet part of library)
- [ ] Prometheus metrics endpoint functional (deferred — requires OTel)
- [x] All configuration examples documented
- [x] README with setup and running instructions
- [ ] Docker Compose file for dependencies (Jaeger, Prometheus) (deferred)
- [x] Manual testing scenarios verified
- [ ] Integration tests passing (deferred — requires WebApplicationFactory setup)
- [ ] Code reviewed and approved

## Notes

### Design Decisions

1. **Why .NET 8 specifically?**
   - Latest LTS version
   - Full modern C# 12 features
   - Best OpenTelemetry support
   - Native AOT readiness

2. **Why both controllers and minimal APIs?**
   - Shows both patterns
   - Demonstrates flexibility
   - Minimal APIs are modern default
   - Controllers still widely used

3. **Why OpenTelemetry integration?**
   - Industry standard for observability
   - Cloud-native monitoring
   - Vendor-neutral approach
   - Rich ecosystem

4. **Why gRPC?**
   - Increasingly popular for microservices
   - Modern RPC framework
   - Shows metadata propagation
   - Streaming scenarios important

### Implementation Tips

- Use top-level statements in Program.cs
- Leverage dependency injection throughout
- Use modern C# features (records, pattern matching, etc.)
- Enable nullable reference types
- Use `ConfigureAwait(false)` in library code
- Follow async/await best practices
- Use `IAsyncEnumerable` for streaming

### Common Pitfalls

- **DI lifetime issues**: Ensure correct service lifetimes
- **Async void**: Never use in production code
- **Missing cancellation tokens**: Always pass CancellationToken
- **Health check blocking**: Keep health checks fast and non-blocking
- **gRPC ports**: Ensure HTTP/2 is enabled
- **Background service exceptions**: Handle and log all exceptions

### Modern .NET 8 Features Demonstrated

- **Minimal APIs**: Lightweight endpoint definitions
- **Top-level statements**: Simplified Program.cs
- **Record types**: Immutable DTOs
- **Required members**: C# 11 feature for DTOs
- **Raw string literals**: For JSON and multi-line strings
- **Source generators**: OpenTelemetry instrumentation
- **Native AOT compatibility**: Design considerations noted

## Related Documentation

- [Project Plan](../project-plan.md#28-create-net-8-sample-application)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [gRPC in .NET](https://learn.microsoft.com/en-us/aspnet/core/grpc/)
- [Background Services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

---

## Implementation Summary

**Completed**: 2026-02-10  
**Implemented by**: GitHub Copilot

### What Was Implemented

A comprehensive .NET 8 ASP.NET Core Web API that serves as a **real-time weather monitoring service**, built from the ground up to exercise the full breadth of the HVO.Enterprise.Telemetry library. The application fetches live weather data from the free Open-Meteo API (no API key required), monitors multiple global locations, evaluates weather alerts, and exposes telemetry diagnostics.

#### Telemetry Features Exercised

| Feature | Files | Description |
|---|---|---|
| **Operation Scopes** | `WeatherService`, `WeatherController` | `IOperationScopeFactory.Begin()` with `ActivityKind`, `InitialTags`, `WithTag`, `WithResult`, `Succeed`, `Fail` |
| **Correlation Context** | `CorrelationMiddleware` | `CorrelationContext.BeginScope()` with X-Correlation-ID header propagation |
| **DispatchProxy Instrumentation** | `ServiceConfiguration` | `AddInstrumentedScoped<IWeatherService, WeatherService>()` with `InstrumentationOptions` |
| **Exception Tracking** | `WeatherService`, error-demo endpoint | `ExceptionAggregator`, `TrackException()`, `scope.RecordException()`, `scope.Fail()` |
| **Telemetry Statistics** | `/api/weather/diagnostics`, `TelemetryReporterService` | `ITelemetryStatistics.GetSnapshot()` — activities, errors, metrics, queue depth, throughput |
| **Health Checks** | `/health`, `/health/ready`, `/health/live` | `AddTelemetryHealthCheck()` + custom `WeatherApiHealthCheck` for Open-Meteo |
| **HTTP Instrumentation** | `ServiceConfiguration` | `TelemetryHttpMessageHandler` on named `IHttpClientFactory` client |
| **ILogger Enrichment** | `ServiceConfiguration` | `AddTelemetryLoggingEnrichment()` with `EnvironmentLogEnricher` |
| **Multi-Level Configuration** | `ServiceConfiguration.ConfigureMultiLevelTelemetry()` | `TelemetryConfigurator` — Global → Namespace → Type hierarchy |
| **Parameter Capture** | Proxy-instrumented `IWeatherService` | `InstrumentationOptions` with PII detection, capture depth control |
| **Metric Recording** | `WeatherService`, `WeatherCollectorService` | `RecordMetric()` for temperatures, durations, counts |
| **Structured Logging** | Everywhere | Named template parameters throughout |
| **Lifecycle Management** | Auto-registered via `AddTelemetry()` | `TelemetryHostedService` + `TelemetryLifetimeManager` |
| **Proxy Factory** | `ServiceConfiguration` | `AddTelemetryProxyFactory()` singleton registration |

#### Disabled Service Integrations Documented

The following integrations are fully documented with commented-out DI + configuration code:
- Application Insights, Datadog, Serilog
- IIS module instrumentation
- WCF message inspector and operation behavior
- Database (EF Core + ADO.NET)
- Redis command instrumentation
- RabbitMQ message publish/consume

### Key Files

| File | Purpose |
|---|---|
| `samples/HVO.Enterprise.Samples.Net8/Program.cs` | App bootstrap, middleware pipeline, Swagger, minimal API |
| `samples/HVO.Enterprise.Samples.Net8/Configuration/ServiceConfiguration.cs` | Full DI wiring, disabled integrations, multi-level config |
| `samples/HVO.Enterprise.Samples.Net8/Controllers/WeatherController.cs` | 8 REST endpoints with telemetry |
| `samples/HVO.Enterprise.Samples.Net8/Services/WeatherService.cs` | Core service with comprehensive telemetry |
| `samples/HVO.Enterprise.Samples.Net8/Services/IWeatherService.cs` | Interface designed for DispatchProxy instrumentation |
| `samples/HVO.Enterprise.Samples.Net8/BackgroundServices/WeatherCollectorService.cs` | Periodic collection (5 min) with `IServiceScopeFactory` |
| `samples/HVO.Enterprise.Samples.Net8/BackgroundServices/TelemetryReporterService.cs` | Periodic stats reporter (1 min) |
| `samples/HVO.Enterprise.Samples.Net8/Middleware/CorrelationMiddleware.cs` | X-Correlation-ID propagation middleware |
| `samples/HVO.Enterprise.Samples.Net8/HealthChecks/WeatherApiHealthCheck.cs` | Open-Meteo API health check |
| `samples/HVO.Enterprise.Samples.Net8/Models/WeatherModels.cs` | Domain models and Open-Meteo DTOs |
| `samples/HVO.Enterprise.Samples.Net8/appsettings.json` | Full telemetry configuration with all options |
| `samples/HVO.Enterprise.Samples.Net8/README.md` | Setup, endpoints, architecture, configuration docs |

### Decisions Made

1. **Open-Meteo API** — Free, no API key required, good for a demo that "just works"
2. **Weather domain** — Provides natural use cases: periodic collection, alerts, multi-location, external API calls
3. **Single Web API project** — More practical than a multi-project sample; covers controllers, minimal APIs, background services, and health checks in one project  
4. **No gRPC** — Deferred because it needs proto tooling, a separate service project, and the library doesn't have gRPC interceptors yet
5. **No OpenTelemetry exporters** — The library doesn't include OTel exporter packages yet; the sample exercises what's actually built
6. **`IServiceScopeFactory` in background services** — Correct DI pattern for resolving scoped services from singleton hosted services
7. **Scoped `HttpClient` via factory** — Bridges `IHttpClientFactory` named client with constructor-injected `HttpClient` in `WeatherService`
8. **Disabled services as commented code** — Shows the exact DI and config patterns for WCF, Redis, RabbitMQ, etc.

### Quality Gates

- ✅ Build: 0 warnings, 0 errors (full solution)
- ✅ Tests: 1,301 passed (120 Common + 1,181 Telemetry) — no regressions
- ✅ App starts and responds to all endpoints
- ✅ Live weather data returned from Open-Meteo API
- ✅ Correlation ID propagation verified
- ✅ Health check returns healthy status
- ✅ Error demo endpoint tracks exceptions correctly
- ✅ Telemetry diagnostics returns live statistics
- ✅ Graceful shutdown with telemetry flush

### Deferred Items

- gRPC service + interceptor (needs proto tooling, separate project)
- OpenTelemetry exporter integration (library doesn't include OTel packages yet)
- Prometheus metrics endpoint (requires OTel)
- Docker Compose file (Jaeger, Prometheus)
- WebApplicationFactory integration tests
- Native AOT compatibility notes
