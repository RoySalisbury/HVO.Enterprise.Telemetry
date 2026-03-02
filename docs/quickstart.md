# Quick Start

This guide shows the smallest set of steps required to enable HVO Enterprise Telemetry in a modern .NET application. It installs the core package, binds configuration from `appsettings.json`, enables logging enrichment, and exposes health/statistics endpoints so you can validate the integration immediately.

## 1. Install the packages

```bash
# Core telemetry APIs
 dotnet add package HVO.Enterprise.Telemetry

# Optional but recommended extensions used in this walkthrough
 dotnet add package HVO.Enterprise.Telemetry.AppInsights
 dotnet add package HVO.Enterprise.Telemetry.OpenTelemetry
```

> You only need the extensions that match your environment. Remove the packages you do not plan to use.

## 2. Add telemetry configuration

Add a `Telemetry` section to `appsettings.json`. This shape exactly matches `TelemetryOptions` and is documented in [configuration-schema.md](configuration-schema.md).

```json
{
  "Telemetry": {
    "ServiceName": "Sample.Api",
    "ServiceVersion": "1.0.0",
    "Environment": "Development",
    "Enabled": true,
    "DefaultSamplingRate": 1.0,
    "ActivitySources": [ "Sample.Api" ],
    "Queue": {
      "Capacity": 10000,
      "BatchSize": 100
    },
    "Features": {
      "EnableHttpInstrumentation": true,
      "EnableExceptionTracking": true,
      "EnableParameterCapture": false
    }
  }
}
```

## 3. Wire up Program.cs (ASP.NET Core / .NET 8+)

```csharp
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Logging;
using HVO.Enterprise.Telemetry.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Bind options from configuration
builder.Services.AddTelemetry(builder.Configuration.GetSection("Telemetry"));

// Optional extensions shown in the issue request
builder.Services.AddTelemetryLoggingEnrichment(options =>
{
    options.IncludeTraceId = true;
    options.IncludeSpanId = true;
});

builder.Services.AddTelemetryStatistics();
builder.Services.AddTelemetryHealthCheck(new TelemetryHealthCheckOptions
{
    DegradedQueueDepthPercent = 70,
    UnhealthyQueueDepthPercent = 90
});

var app = builder.Build();
app.MapHealthChecks("/health/telemetry");
app.MapGet("/ping", () => "pong");
app.Run();
```

For older ASP.NET (.NET Framework 4.8), call `Telemetry.Initialize(options => { ... })` inside `Global.asax` and `Telemetry.Shutdown()` inside `Application_End`.

## 4. Validate the integration

1. `dotnet run` the application.
2. Hit `/ping` and observe the enriched logs (`TraceId`, `SpanId`, `CorrelationId`).
3. Hit `/health/telemetry` to verify the telemetry health check.
4. Inspect `ITelemetryStatistics` via dependency injection (e.g., expose `/debug/telemetry`).

## Next steps

- Review the configuration surface in [configuration-schema.md](configuration-schema.md).
- Explore the full API surface in [api-reference.md](api-reference.md).
- Browse the [samples/HVO.Enterprise.Samples.Net8](../samples/HVO.Enterprise.Samples.Net8/) application for end-to-end scenarios.
