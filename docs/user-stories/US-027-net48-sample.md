# US-027: .NET Framework 4.8 Sample Application

**GitHub Issue**: [#29](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/29)  
**Status**: ❌ Not Started  
**Category**: Testing  
**Effort**: 13 story points  
**Sprint**: 10

## Description

As a **developer migrating legacy applications**,  
I want to **see a comprehensive .NET Framework 4.8 sample demonstrating telemetry integration**,  
So that **I can understand how to integrate HVO.Enterprise.Telemetry into ASP.NET MVC, Web API, WCF services, and Hangfire jobs**.

## Acceptance Criteria

1. **Project Structure**
   - [ ] Sample solution includes ASP.NET MVC 5 web application
   - [ ] Web API 2 controllers with telemetry instrumentation
   - [ ] WCF service with message inspector integration
   - [ ] Hangfire background job processing with correlation
   - [ ] Console application demonstrating static initialization

2. **ASP.NET MVC Integration**
   - [ ] Global.asax initialization
   - [ ] HTTP module for request/response correlation
   - [ ] Action filter for controller instrumentation
   - [ ] View instrumentation with correlation IDs
   - [ ] Error handling with telemetry

3. **Web API Integration**
   - [ ] DelegatingHandler for automatic instrumentation
   - [ ] Attribute-based operation scopes
   - [ ] W3C TraceContext propagation
   - [ ] Exception filter with telemetry
   - [ ] Response correlation headers

4. **WCF Integration**
   - [ ] Service behavior for automatic instrumentation
   - [ ] Message inspector for SOAP header propagation
   - [ ] Operation context correlation
   - [ ] Fault contract telemetry
   - [ ] Dual HTTP/TCP bindings demonstrated

5. **Hangfire Integration**
   - [ ] Job filter for correlation propagation
   - [ ] Background job telemetry
   - [ ] Recurring job patterns
   - [ ] Queue-specific configuration
   - [ ] Dashboard integration examples

6. **Configuration**
   - [ ] Web.config/App.config setup
   - [ ] Multiple sink configuration (Console, File, Application Insights)
   - [ ] Environment-specific settings
   - [ ] Hot reload demonstration

## Technical Requirements

### Solution Structure

```
HVO.Enterprise.Samples.Net48/
├── HVO.Enterprise.Samples.Net48.Web/           # ASP.NET MVC + Web API
│   ├── App_Start/
│   │   ├── TelemetryConfig.cs
│   │   ├── WebApiConfig.cs
│   │   └── FilterConfig.cs
│   ├── Controllers/
│   │   ├── HomeController.cs                   # MVC controller
│   │   ├── ApiController.cs                    # Web API controller
│   │   └── WeatherController.cs                # Demo API
│   ├── Filters/
│   │   ├── TelemetryActionFilter.cs
│   │   └── TelemetryExceptionFilter.cs
│   ├── Handlers/
│   │   └── TelemetryDelegatingHandler.cs
│   ├── Modules/
│   │   └── TelemetryHttpModule.cs
│   ├── Jobs/
│   │   ├── SampleBackgroundJob.cs
│   │   └── RecurringReportJob.cs
│   ├── Views/
│   │   └── Shared/_Layout.cshtml               # Correlation ID in layout
│   ├── Global.asax
│   └── Web.config
├── HVO.Enterprise.Samples.Net48.Wcf/           # WCF Service
│   ├── Services/
│   │   ├── ICustomerService.cs
│   │   └── CustomerService.cs
│   ├── Behaviors/
│   │   ├── TelemetryServiceBehavior.cs
│   │   └── TelemetryEndpointBehavior.cs
│   ├── Inspectors/
│   │   └── TelemetryMessageInspector.cs
│   └── Web.config
├── HVO.Enterprise.Samples.Net48.Console/       # Console application
│   ├── Program.cs
│   └── App.config
└── HVO.Enterprise.Samples.Net48.sln
```

### 1. Global.asax - Application Initialization

```csharp
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Http;
using HVO.Enterprise.Telemetry;
using Hangfire;
using Hangfire.SqlServer;

namespace HVO.Enterprise.Samples.Net48.Web
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Initialize telemetry system
            Telemetry.Initialize(config =>
            {
                config.ServiceName = "HVO.Samples.Net48";
                config.ServiceVersion = "1.0.0";
                config.EnableCorrelation = true;
                config.EnableMetrics = true;
                config.EnableTracing = true;
                
                // Configure background queue
                config.QueueCapacity = 10000;
                config.BatchSize = 100;
                config.FlushInterval = TimeSpan.FromSeconds(5);
                
                // Add exporters
                config.AddConsoleExporter();
                config.AddFileExporter("logs/telemetry.log");
                config.AddApplicationInsightsExporter(
                    instrumentationKey: System.Configuration.ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"]);
            });

            // Standard MVC setup
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Configure Hangfire
            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage("HangfireConnection", new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                })
                .UseTelemetry(); // Add telemetry to Hangfire

            // Start Hangfire server
            var options = new BackgroundJobServerOptions
            {
                WorkerCount = 5
            };
            var server = new BackgroundJobServer(options);

            // Schedule recurring jobs
            RecurringJob.AddOrUpdate<RecurringReportJob>(
                "daily-report",
                job => job.ExecuteAsync(),
                Cron.Daily);
        }

        protected void Application_End()
        {
            // Graceful shutdown
            Telemetry.Shutdown(timeout: TimeSpan.FromSeconds(30));
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            if (exception != null)
            {
                // Track unhandled exceptions
                Telemetry.TrackException(exception, new
                {
                    ErrorSource = "Application_Error",
                    CorrelationId = CorrelationManager.CurrentCorrelationId,
                    Url = Request?.Url?.ToString()
                });
            }
        }
    }
}
```

### 2. Telemetry HTTP Module

```csharp
using System;
using System.Web;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net48.Web.Modules
{
    /// <summary>
    /// HTTP module that automatically tracks all requests with telemetry.
    /// </summary>
    public class TelemetryHttpModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
            context.Error += OnError;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var request = context.Request;

            // Extract or generate correlation ID
            var correlationId = request.Headers["X-Correlation-ID"]
                ?? request.Headers["traceparent"]
                ?? CorrelationManager.GenerateNewCorrelationId();

            CorrelationManager.CurrentCorrelationId = correlationId;

            // Start activity for the request
            var activity = Telemetry.StartActivity(
                "HTTP " + request.HttpMethod,
                ActivityKind.Server);

            if (activity != null)
            {
                activity.SetTag("http.method", request.HttpMethod);
                activity.SetTag("http.url", request.Url.ToString());
                activity.SetTag("http.target", request.Path);
                activity.SetTag("http.host", request.Url.Host);
                activity.SetTag("http.scheme", request.Url.Scheme);
                activity.SetTag("correlation.id", correlationId);

                // Extract W3C TraceContext if present
                var traceparent = request.Headers["traceparent"];
                if (!string.IsNullOrEmpty(traceparent))
                {
                    activity.SetParentId(traceparent);
                }

                // Store activity in request context
                context.Items["__TelemetryActivity"] = activity;
            }

            // Record request metric
            Telemetry.RecordMetric("http_requests_total", 1, new[]
            {
                new KeyValuePair<string, object>("method", request.HttpMethod),
                new KeyValuePair<string, object>("path", request.Path)
            });
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var response = context.Response;

            // Add correlation ID to response headers
            var correlationId = CorrelationManager.CurrentCorrelationId;
            if (!string.IsNullOrEmpty(correlationId))
            {
                response.Headers["X-Correlation-ID"] = correlationId;
            }

            // Stop activity
            var activity = context.Items["__TelemetryActivity"] as IDisposable;
            if (activity != null)
            {
                // Record response status
                Telemetry.RecordMetric("http_requests_duration_ms", 
                    activity.Duration.TotalMilliseconds,
                    new[]
                    {
                        new KeyValuePair<string, object>("method", context.Request.HttpMethod),
                        new KeyValuePair<string, object>("status", response.StatusCode)
                    });

                activity.Dispose();
            }
        }

        private void OnError(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var exception = application.Server.GetLastError();

            if (exception != null)
            {
                Telemetry.TrackException(exception, new
                {
                    Source = "TelemetryHttpModule",
                    CorrelationId = CorrelationManager.CurrentCorrelationId,
                    Url = application.Request?.Url?.ToString()
                });
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
```

Register in Web.config:

```xml
<system.webServer>
  <modules>
    <add name="TelemetryHttpModule" 
         type="HVO.Enterprise.Samples.Net48.Web.Modules.TelemetryHttpModule, HVO.Enterprise.Samples.Net48.Web" />
  </modules>
</system.webServer>
```

### 3. MVC Action Filter

```csharp
using System;
using System.Diagnostics;
using System.Web.Mvc;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net48.Web.Filters
{
    /// <summary>
    /// Action filter that creates an operation scope for each MVC action.
    /// </summary>
    public class TelemetryActionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            var actionName = filterContext.ActionDescriptor.ActionName;
            var operationName = $"{controllerName}.{actionName}";

            // Create operation scope
            var scope = Telemetry.CreateOperationScope(operationName);
            
            // Add action parameters as properties
            foreach (var param in filterContext.ActionParameters)
            {
                scope.AddProperty($"param.{param.Key}", param.Value);
            }

            // Store scope in items for cleanup
            filterContext.HttpContext.Items["__TelemetryOperationScope"] = scope;

            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var scope = filterContext.HttpContext.Items["__TelemetryOperationScope"] as IDisposable;

            if (scope != null)
            {
                // Record result
                if (filterContext.Exception != null)
                {
                    scope.SetStatus(ActivityStatusCode.Error, filterContext.Exception.Message);
                }
                else if (filterContext.Result is ViewResult viewResult)
                {
                    scope.AddProperty("view", viewResult.ViewName);
                }

                scope.Dispose();
            }

            base.OnActionExecuted(filterContext);
        }
    }
}
```

### 4. Web API Delegating Handler

```csharp
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net48.Web.Handlers
{
    /// <summary>
    /// Delegating handler for automatic Web API telemetry.
    /// </summary>
    public class TelemetryDelegatingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var method = request.Method.Method;
            var path = request.RequestUri?.AbsolutePath ?? "unknown";
            var operationName = $"{method} {path}";

            using (var scope = Telemetry.CreateOperationScope(operationName))
            {
                scope.AddProperty("http.method", method);
                scope.AddProperty("http.url", request.RequestUri?.ToString());

                try
                {
                    var response = await base.SendAsync(request, cancellationToken);

                    scope.AddProperty("http.status_code", (int)response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        scope.SetStatus(ActivityStatusCode.Error, 
                            $"HTTP {(int)response.StatusCode}");
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    scope.SetStatus(ActivityStatusCode.Error, ex.Message);
                    Telemetry.TrackException(ex);
                    throw;
                }
            }
        }
    }
}
```

Register in WebApiConfig:

```csharp
config.MessageHandlers.Add(new TelemetryDelegatingHandler());
```

### 5. WCF Service Integration

```csharp
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net48.Wcf.Inspectors
{
    /// <summary>
    /// Message inspector that propagates correlation and creates operation scopes.
    /// </summary>
    public class TelemetryMessageInspector : IDispatchMessageInspector, IClientMessageInspector
    {
        // Server-side: incoming request
        public object AfterReceiveRequest(
            ref Message request,
            IClientChannel channel,
            InstanceContext instanceContext)
        {
            // Extract correlation ID from SOAP header
            var correlationId = ExtractCorrelationId(request);
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = CorrelationManager.GenerateNewCorrelationId();
            }

            CorrelationManager.CurrentCorrelationId = correlationId;

            // Start operation scope
            var action = request.Headers.Action ?? "unknown";
            var scope = Telemetry.CreateOperationScope($"WCF {action}");
            scope.AddProperty("wcf.action", action);
            scope.AddProperty("correlation.id", correlationId);

            return scope;
        }

        // Server-side: outgoing response
        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            // Add correlation ID to response
            var correlationId = CorrelationManager.CurrentCorrelationId;
            if (!string.IsNullOrEmpty(correlationId))
            {
                AddCorrelationHeader(ref reply, correlationId);
            }

            // Dispose operation scope
            (correlationState as IDisposable)?.Dispose();
        }

        // Client-side: outgoing request
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            // Add correlation ID to outgoing request
            var correlationId = CorrelationManager.CurrentCorrelationId 
                ?? CorrelationManager.GenerateNewCorrelationId();
            
            AddCorrelationHeader(ref request, correlationId);

            return null;
        }

        // Client-side: incoming response
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            // Extract correlation ID from response
            var correlationId = ExtractCorrelationId(reply);
            if (!string.IsNullOrEmpty(correlationId))
            {
                CorrelationManager.CurrentCorrelationId = correlationId;
            }
        }

        private string ExtractCorrelationId(Message message)
        {
            var headerIndex = message.Headers.FindHeader("X-Correlation-ID", "http://hvo.enterprise/telemetry");
            if (headerIndex >= 0)
            {
                return message.Headers.GetHeader<string>(headerIndex);
            }
            return null;
        }

        private void AddCorrelationHeader(ref Message message, string correlationId)
        {
            var header = MessageHeader.CreateHeader(
                "X-Correlation-ID",
                "http://hvo.enterprise/telemetry",
                correlationId);
            
            message.Headers.Add(header);
        }
    }
}
```

Service behavior:

```csharp
using System;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace HVO.Enterprise.Samples.Net48.Wcf.Behaviors
{
    public class TelemetryServiceBehavior : IServiceBehavior
    {
        public void ApplyDispatchBehavior(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher dispatcher in serviceHostBase.ChannelDispatchers)
            {
                foreach (var endpoint in dispatcher.Endpoints)
                {
                    endpoint.DispatchRuntime.MessageInspectors.Add(
                        new TelemetryMessageInspector());
                }
            }
        }

        public void AddBindingParameters(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints,
            BindingParameterCollection bindingParameters)
        {
        }

        public void Validate(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase)
        {
        }
    }
}
```

### 6. Hangfire Integration

```csharp
using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.BackgroundJobs;

namespace HVO.Enterprise.Samples.Net48.Web.Jobs
{
    /// <summary>
    /// Sample background job with telemetry.
    /// </summary>
    public class SampleBackgroundJob
    {
        public async Task ExecuteAsync(string data, string correlationId)
        {
            // Restore correlation context
            using (BackgroundJobCorrelation.CreateScope(correlationId))
            using (var scope = Telemetry.CreateOperationScope("SampleBackgroundJob.Execute"))
            {
                scope.AddProperty("data", data);

                try
                {
                    // Simulate work
                    await Task.Delay(100);
                    
                    // Do actual work
                    ProcessData(data);

                    scope.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    scope.SetStatus(ActivityStatusCode.Error, ex.Message);
                    Telemetry.TrackException(ex);
                    throw;
                }
            }
        }

        private void ProcessData(string data)
        {
            // Process data...
        }
    }

    /// <summary>
    /// Hangfire filter for automatic correlation propagation.
    /// </summary>
    public class TelemetryJobFilter : IClientFilter, IServerFilter
    {
        // Client side: capture correlation when job is enqueued
        public void OnCreating(CreatingContext context)
        {
            var correlationId = CorrelationManager.CurrentCorrelationId 
                ?? CorrelationManager.GenerateNewCorrelationId();
            
            context.SetJobParameter("CorrelationId", correlationId);
        }

        public void OnCreated(CreatedContext context)
        {
        }

        // Server side: restore correlation when job executes
        public void OnPerforming(PerformingContext context)
        {
            var correlationId = context.GetJobParameter<string>("CorrelationId");
            if (!string.IsNullOrEmpty(correlationId))
            {
                CorrelationManager.CurrentCorrelationId = correlationId;
            }

            var jobName = $"{context.BackgroundJob.Job.Type.Name}.{context.BackgroundJob.Job.Method.Name}";
            var scope = Telemetry.CreateOperationScope($"HangfireJob {jobName}");
            
            context.Items["__TelemetryScope"] = scope;
        }

        public void OnPerformed(PerformedContext context)
        {
            var scope = context.Items["__TelemetryScope"] as IDisposable;
            scope?.Dispose();
        }
    }
}
```

Register Hangfire filter:

```csharp
GlobalJobFilters.Filters.Add(new TelemetryJobFilter());
```

### 7. Console Application

```csharp
using System;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry;

namespace HVO.Enterprise.Samples.Net48.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize telemetry
            Telemetry.Initialize(config =>
            {
                config.ServiceName = "HVO.Samples.Console";
                config.EnableCorrelation = true;
                config.AddConsoleExporter();
            });

            try
            {
                await RunSampleOperations();
            }
            finally
            {
                // Ensure graceful shutdown
                Telemetry.Shutdown(TimeSpan.FromSeconds(5));
            }
        }

        static async Task RunSampleOperations()
        {
            // Example 1: Simple operation scope
            using (var scope = Telemetry.CreateOperationScope("ProcessOrder"))
            {
                scope.AddProperty("orderId", "12345");
                await ProcessOrderAsync("12345");
            }

            // Example 2: Manual correlation propagation
            var correlationId = CorrelationManager.GenerateNewCorrelationId();
            using (CorrelationManager.CreateScope(correlationId))
            {
                await CallExternalServiceAsync();
            }

            // Example 3: Exception tracking
            try
            {
                throw new InvalidOperationException("Sample error");
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex, new { Context = "Sample" });
            }

            // Example 4: Custom metrics
            Telemetry.RecordMetric("orders_processed", 1);
            Telemetry.RecordMetric("order_value", 99.99);
        }

        static Task ProcessOrderAsync(string orderId)
        {
            // Simulate processing
            return Task.Delay(100);
        }

        static Task CallExternalServiceAsync()
        {
            // Simulate external call
            return Task.Delay(50);
        }
    }
}
```

### Web.config Configuration

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <!-- Application Insights -->
    <add key="APPINSIGHTS_INSTRUMENTATIONKEY" value="your-instrumentation-key" />
    
    <!-- Telemetry Configuration -->
    <add key="Telemetry:ServiceName" value="HVO.Samples.Net48.Web" />
    <add key="Telemetry:ServiceVersion" value="1.0.0" />
    <add key="Telemetry:EnableCorrelation" value="true" />
    <add key="Telemetry:EnableMetrics" value="true" />
    <add key="Telemetry:EnableTracing" value="true" />
    <add key="Telemetry:QueueCapacity" value="10000" />
  </appSettings>

  <connectionStrings>
    <add name="HangfireConnection" 
         connectionString="Server=.;Database=Hangfire;Integrated Security=True;" 
         providerName="System.Data.SqlClient" />
  </connectionStrings>

  <system.web>
    <compilation debug="true" targetFramework="4.8.1" />
    <httpRuntime targetFramework="4.8.1" />
  </system.web>

  <system.webServer>
    <modules>
      <add name="TelemetryHttpModule" 
           type="HVO.Enterprise.Samples.Net48.Web.Modules.TelemetryHttpModule" />
    </modules>
  </system.webServer>
</configuration>
```

## Testing Requirements

### Manual Testing Scenarios

1. **MVC Controller**
   - Navigate to home page, verify correlation ID in response
   - Check telemetry output for request timing
   - Verify operation scope created for action

2. **Web API**
   - Call API endpoint with curl/Postman
   - Send custom X-Correlation-ID header, verify propagation
   - Check W3C traceparent header support

3. **WCF Service**
   - Call WCF service from client
   - Verify SOAP header correlation propagation
   - Test both HTTP and TCP bindings

4. **Hangfire Jobs**
   - Enqueue background job from controller
   - Verify correlation ID propagates to job
   - Check Hangfire dashboard for job telemetry

5. **Error Scenarios**
   - Trigger handled exception, verify tracking
   - Trigger unhandled exception, verify Application_Error tracking
   - Verify error correlation with request

### Integration Tests

```csharp
[Fact]
public async Task WebApi_ShouldPropagateCorrelationId()
{
    // Arrange
    var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
    var correlationId = Guid.NewGuid().ToString();
    client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

    // Act
    var response = await client.GetAsync("/api/weather");

    // Assert
    response.Headers.GetValues("X-Correlation-ID").Should().Contain(correlationId);
}
```

## Performance Requirements

- **HTTP Module Overhead**: <5ms per request
- **Action Filter Overhead**: <2ms per action
- **WCF Inspector Overhead**: <3ms per call
- **Hangfire Filter Overhead**: <1ms per job
- **Memory Impact**: <50MB additional memory for telemetry system

## Dependencies

**Blocked By**: 
- US-001 through US-018 (core telemetry features)
- US-020 (IIS extension)
- US-021 (WCF extension)

**Blocks**: 
- US-029 (documentation references this sample)

## Definition of Done

- [ ] All projects created and building
- [ ] ASP.NET MVC integration complete with examples
- [ ] Web API integration complete with examples
- [ ] WCF service integration complete with examples
- [ ] Hangfire integration complete with examples
- [ ] Console application demonstrates static initialization
- [ ] All configuration examples documented
- [ ] README with setup instructions
- [ ] Manual testing scenarios verified
- [ ] Screenshots/logs captured for documentation
- [ ] Code reviewed and approved
- [ ] Deployed to sample environment

## Notes

### Design Decisions

1. **Why all-in-one solution?**
   - Demonstrates real-world application structure
   - Shows how different technologies integrate
   - Single deployment for easy testing

2. **Why Hangfire for background jobs?**
   - Most common .NET Framework background job library
   - Good integration points for telemetry
   - Demonstrates correlation propagation

3. **Why both HTTP module and action filter?**
   - HTTP module for all requests (static files, etc.)
   - Action filter for controller-specific telemetry
   - Demonstrates layered instrumentation

### Implementation Tips

- Start with simple MVC app, add features incrementally
- Test each integration point independently
- Use IIS Express for local development
- Configure Application Insights for cloud telemetry
- Add extensive comments for learning purposes

### Common Pitfalls

- **Web.config transforms**: Test Debug and Release configs
- **IIS app pool recycling**: Test graceful shutdown
- **Hangfire database**: Ensure SQL Server connection works
- **WCF configuration**: Complex XML configuration is error-prone
- **Static initialization**: Must happen in Application_Start

## Related Documentation

- [Project Plan](../project-plan.md#27-create-net-framework-48-sample-application)
- [IIS Extension](./US-020-iis-extension.md)
- [WCF Extension](./US-021-wcf-extension.md)
- [Hangfire Documentation](https://docs.hangfire.io/)
- [ASP.NET MVC Documentation](https://learn.microsoft.com/en-us/aspnet/mvc/)
