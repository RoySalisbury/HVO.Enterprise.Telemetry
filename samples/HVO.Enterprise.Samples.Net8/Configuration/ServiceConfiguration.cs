using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using HVO.Enterprise.Samples.Net8.BackgroundServices;
using HVO.Enterprise.Samples.Net8.Caching;
using HVO.Enterprise.Samples.Net8.Data;
using HVO.Enterprise.Samples.Net8.HealthChecks;
using HVO.Enterprise.Samples.Net8.Messaging;
using HVO.Enterprise.Samples.Net8.Services;
using HVO.Enterprise.Samples.Net8.Telemetry;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.AppInsights;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Data.AdoNet;
using HVO.Enterprise.Telemetry.Data.AdoNet.Extensions;
using HVO.Enterprise.Telemetry.Data.EfCore.Extensions;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Extensions;
using HVO.Enterprise.Telemetry.Data.Redis.Extensions;
using HVO.Enterprise.Telemetry.Datadog;
using HVO.Enterprise.Telemetry.HealthChecks;
using HVO.Enterprise.Telemetry.Http;
using HVO.Enterprise.Telemetry.Logging;
using HVO.Enterprise.Telemetry.OpenTelemetry;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Configuration
{
    /// <summary>
    /// Centralised service registration for the sample application.
    /// Demonstrates the recommended way to wire up all HVO.Enterprise.Telemetry
    /// features via Dependency Injection, including features that are intentionally
    /// disabled (WCF, RabbitMQ, Redis, Datadog, Serilog, App Insights).
    /// </summary>
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Registers all application services including telemetry.
        /// </summary>
        public static IServiceCollection AddSampleServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            // ────────────────────────────────────────────────────────
            // 1. CORE TELEMETRY (always enabled)
            // ────────────────────────────────────────────────────────

            // Option A: Configure from appsettings.json (recommended for production)
            services.AddTelemetry(configuration.GetSection("Telemetry"));

            // Option B: Configure fluently in code (useful for quick setup)
            // services.AddTelemetry(options =>
            // {
            //     options.ServiceName = "HVO.Samples.Net8";
            //     options.ServiceVersion = "1.0.0";
            //     options.Environment = "Development";
            //     options.Enabled = true;
            //     options.DefaultSamplingRate = 1.0;
            //     options.Queue.Capacity = 10000;
            //     options.Queue.BatchSize = 100;
            //     options.Features.EnableHttpInstrumentation = true;
            //     options.Features.EnableExceptionTracking = true;
            //     options.Features.EnableParameterCapture = true;
            //     options.Features.EnableProxyInstrumentation = true;
            //     options.Logging.EnableCorrelationEnrichment = true;
            // });

            // Option C: Builder pattern for advanced setup
            // services.AddTelemetry(builder => builder
            //     .Configure(o => { o.ServiceName = "HVO.Samples.Net8"; })
            //     .AddActivitySource("HVO.Samples.Weather")
            //     .AddHttpInstrumentation(http =>
            //     {
            //         http.RedactQueryStrings = true;
            //         http.CaptureRequestHeaders = false;
            //         http.CaptureResponseHeaders = false;
            //     }));

            // ────────────────────────────────────────────────────────
            // 2. LOGGING ENRICHMENT
            //    Automatically adds CorrelationId, TraceId, SpanId
            //    to all ILogger log entries.
            // ────────────────────────────────────────────────────────

            services.AddTelemetryLoggingEnrichment(options =>
            {
                options.EnableEnrichment = true;
                options.IncludeCorrelationId = true;
                options.IncludeTraceId = true;
                options.IncludeSpanId = true;

                // Add custom enrichers
                options.CustomEnrichers ??= new();
                options.CustomEnrichers.Add(
                    new HVO.Enterprise.Telemetry.Logging.Enrichers.EnvironmentLogEnricher());
            });

            // ────────────────────────────────────────────────────────
            // 2b. FIRST-CHANCE EXCEPTION MONITORING (opt-in)
            //     Detects exceptions the instant they are thrown, even
            //     if they are subsequently caught and suppressed. Useful
            //     for diagnosing hidden failures at runtime.
            //     Configuration is hot-reloadable via appsettings.json
            //     (section: Telemetry:FirstChanceExceptions).
            // ────────────────────────────────────────────────────────

            services.AddFirstChanceExceptionMonitoring(
                configuration.GetSection("Telemetry:FirstChanceExceptions"));

            // ────────────────────────────────────────────────────────
            // 3. TELEMETRY HEALTH CHECKS
            //    Monitors queue depth, drop rate, error rate.
            // ────────────────────────────────────────────────────────

            services.AddTelemetryStatistics();
            services.AddTelemetryHealthCheck(new HVO.Enterprise.Telemetry.HealthChecks.TelemetryHealthCheckOptions
            {
                DegradedErrorRateThreshold = 5.0,
                UnhealthyErrorRateThreshold = 20.0,
                MaxExpectedQueueDepth = 10000,
                DegradedQueueDepthPercent = 75.0,
                UnhealthyQueueDepthPercent = 95.0,
            });

            services.AddHealthChecks()
                .AddCheck<HVO.Enterprise.Telemetry.HealthChecks.TelemetryHealthCheck>(
                    "telemetry",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "ready" });

            // ────────────────────────────────────────────────────────
            // 4. PROXY INSTRUMENTATION (DispatchProxy)
            //    Wraps IWeatherService with automatic operation scopes,
            //    parameter capture, and metric recording.
            // ────────────────────────────────────────────────────────

            services.AddTelemetryProxyFactory();

            // Register WeatherService with the instrumented wrapper.
            // The proxy automatically creates operation scopes for every
            // interface method call, capturing parameters, return values,
            // timing, and exceptions.
            services.AddInstrumentedScoped<IWeatherService, WeatherService>(
                new InstrumentationOptions
                {
                    CaptureComplexTypes = true,
                    MaxCaptureDepth = 2,
                    MaxCollectionItems = 10,
                    AutoDetectPii = true,
                });

            // ────────────────────────────────────────────────────────
            // 5. HTTP CLIENT with TELEMETRY HANDLER
            //    Automatically instruments outbound HTTP calls with
            //    correlation propagation and operation scopes.
            // ────────────────────────────────────────────────────────

            services.AddHttpClient("OpenMeteo", client =>
            {
                client.BaseAddress = new Uri("https://api.open-meteo.com");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetService<ILogger<TelemetryHttpMessageHandler>>();
                return new TelemetryHttpMessageHandler(
                    new HttpInstrumentationOptions
                    {
                        RedactQueryStrings = false, // Weather API queries are not sensitive
                        CaptureRequestHeaders = false,
                        CaptureResponseHeaders = false,
                    },
                    logger);
            });

            // Default HttpClient for other uses
            services.AddHttpClient();

            // Register HttpClient as a resolvable service so that
            // WeatherService (registered via AddInstrumentedScoped) can
            // receive it through constructor injection.
            services.AddScoped<HttpClient>(sp =>
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenMeteo"));

            // ────────────────────────────────────────────────────────
            // 6. BACKGROUND SERVICES
            // ────────────────────────────────────────────────────────

            services.AddHostedService<WeatherCollectorService>();
            services.AddHostedService<TelemetryReporterService>();

            // ────────────────────────────────────────────────────────
            // 7. HEALTH CHECKS (ASP.NET Core)
            // ────────────────────────────────────────────────────────

            services.AddHealthChecks()
                .AddCheck<WeatherApiHealthCheck>(
                    "weather-api",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "ready", "external" });

            // ────────────────────────────────────────────────────────
            // 8. MULTI-LEVEL CONFIGURATION (demonstrate hierarchy)
            //    Global → Namespace → Type → Method
            // ────────────────────────────────────────────────────────

            ConfigureMultiLevelTelemetry();

            // ════════════════════════════════════════════════════════
            // EXTENSION INTEGRATIONS
            // Each extension is toggled via appsettings.json
            // (section: Extensions:<Name>:Enabled).
            // ════════════════════════════════════════════════════════

            var extensions = configuration.GetSection("Extensions");

            // ── Application Insights Extension ─────────────────────
            // Uses InMemoryChannel — no Azure connection needed.
            if (extensions.GetValue<bool>("ApplicationInsights:Enabled"))
            {
                services.AddAppInsightsTelemetry(options =>
                {
                    // InMemoryChannel is configured in Program.cs via
                    // the Microsoft.ApplicationInsights.AspNetCore SDK;
                    // the HVO bridge enriches AI items with correlation.
                    options.EnableBridge = true;
                    options.EnableActivityInitializer = true;
                    options.EnableCorrelationInitializer = true;
                });
            }

            // ── Datadog Extension ──────────────────────────────────
            // Console exporter mode — no Datadog agent required.
            if (extensions.GetValue<bool>("Datadog:Enabled"))
            {
                services.AddDatadogTelemetry(options =>
                {
                    options.ServiceName = "hvo-samples-net8";
                    options.Environment = "development";
                    options.AgentHost = extensions["Datadog:AgentHost"] ?? "localhost";
                    options.EnableMetricsExporter = true;
                    options.EnableTraceExporter = true;
                });
            }

            // ── OpenTelemetry OTLP Extension ───────────────────────
            // Routes traces, metrics, and logs to any OTLP-compatible
            // backend (Jaeger, Zipkin, Grafana Tempo, Honeycomb, etc.).
            if (extensions.GetValue<bool>("OpenTelemetry:Enabled"))
            {
                services.AddOpenTelemetryExport(options =>
                {
                    options.ServiceName = extensions["OpenTelemetry:ServiceName"] ?? "hvo-samples-net8";
                    options.ServiceVersion = extensions["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
                    options.Environment = extensions["OpenTelemetry:Environment"] ?? "development";
                    options.Endpoint = extensions["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

                    // Transport is auto-detected from port (4318 → HttpProtobuf).
                    // Only set explicitly when using a non-standard port.
                    var transport = extensions["OpenTelemetry:Transport"];
                    if (!string.IsNullOrEmpty(transport)
                        && Enum.TryParse<OtlpTransport>(transport, ignoreCase: true, out var transportValue))
                    {
                        options.Transport = transportValue;
                    }

                    options.EnableTraceExport = true;
                    options.EnableMetricsExport = true;
                    options.EnableLogExport = extensions.GetValue<bool>("OpenTelemetry:EnableLogExport");
                    options.EnablePrometheusEndpoint = extensions.GetValue<bool>("OpenTelemetry:EnablePrometheus");

                    // Register standard .NET meters (ASP.NET Core, Kestrel, System.Net.Http, etc.)
                    options.EnableStandardMeters = extensions.GetValue("OpenTelemetry:EnableStandardMeters", true);

                    // Register the sample app's own ActivitySource so its spans appear in traces
                    options.AdditionalActivitySources.Add("hvo-samples-net8");
                });
            }

            // ── Database Extension (EF Core + SQLite) ──────────────
            if (extensions.GetValue("Database:Enabled", true))
            {
                var dbConnectionString = extensions["Database:ConnectionString"]
                    ?? "Data Source=weather.db";

                services.AddEfCoreTelemetry(options =>
                {
                    options.RecordStatements = extensions.GetValue("Database:CaptureCommandText", true);
                    options.RecordParameters = false; // PII safety
                    options.RecordConnectionInfo = false;
                });

                services.AddDbContext<WeatherDbContext>((sp, options) =>
                {
                    options.UseSqlite(dbConnectionString);
                    options.AddHvoTelemetry(); // EF Core interceptor
                });

                services.AddScoped<WeatherRepository>();
            }

            // ── Database Extension (ADO.NET + SQLite) ──────────────
            if (extensions.GetValue("Database:Enabled", true))
            {
                var dbConnectionString = extensions["Database:ConnectionString"]
                    ?? "Data Source=weather.db";

                services.AddAdoNetTelemetry(options =>
                {
                    options.RecordStatements = extensions.GetValue("Database:CaptureCommandText", true);
                    options.RecordParameters = false;
                    options.RecordConnectionInfo = false;
                });

                // Register an instrumented DbConnection for ADO.NET repository
                services.AddScoped<DbConnection>(sp =>
                {
                    var connection = new SqliteConnection(dbConnectionString);
                    return connection.WithTelemetry(); // ADO.NET telemetry wrapper
                });

                services.AddScoped<WeatherAdoNetRepository>();
            }

            // ── Redis Extension (Fake In-Process Cache) ────────────
            if (extensions.GetValue("Redis:Enabled", true))
            {
                services.AddRedisTelemetry(options =>
                {
                    options.RecordCommands = true;
                    options.RecordKeys = true;
                    options.RecordDatabaseIndex = true;
                });

                // Use FakeRedisCache when no real Redis is available
                if (extensions.GetValue("Redis:UseFakeCache", true))
                {
                    services.AddSingleton<FakeRedisCache>();
                    services.AddSingleton<IDistributedCache>(sp => sp.GetRequiredService<FakeRedisCache>());
                }

                services.AddScoped<WeatherCacheService>();
            }

            // ── RabbitMQ Extension (Fake Message Bus) ──────────────
            if (extensions.GetValue("RabbitMQ:Enabled", true))
            {
                services.AddRabbitMqTelemetry(options =>
                {
                    options.PropagateTraceContext = true;
                    options.RecordExchange = true;
                    options.RecordRoutingKey = true;
                    options.RecordBodySize = true;
                });

                // Use FakeMessageBus when no real RabbitMQ is available
                if (extensions.GetValue("RabbitMQ:UseFakeMessageBus", true))
                {
                    services.AddSingleton<FakeMessageBus>();
                    services.AddScoped<WeatherObservationPublisher>();

                    // Multi-stage message processing pipeline:
                    //   Stage 1: AlertProcessorSubscriber
                    //     Consumes weather.observations → evaluates alerts, computes
                    //     heat index/wind chill, performs CPU work (Pi digits) →
                    //     publishes WeatherAnalysisEvent to weather.analysis
                    //
                    //   Stage 2: WeatherAnalyticsProcessor
                    //     Consumes weather.analysis → summarises, hash iterations,
                    //     random delay → publishes WeatherNotificationEvent to
                    //     weather.notifications
                    //
                    //   Stage 3: NotificationDispatchSubscriber
                    //     Consumes weather.notifications → logs final notification
                    //     with full pipeline timing and the original correlation ID.
                    services.AddHostedService<AlertProcessorSubscriber>();
                    services.AddHostedService<WeatherAnalyticsProcessor>();
                    services.AddHostedService<NotificationDispatchSubscriber>();
                }
            }

            // ── Serilog Extension ──────────────────────────────────
            // Serilog is wired up in Program.cs via builder.Host.UseSerilog().
            // The enricher (Enrich.WithTelemetry()) is applied there.
            // See Program.cs for the Serilog configuration.

            // ── IIS Extension ──────────────────────────────────────
            // Not applicable to .NET 8. See US-027 (.NET Framework 4.8 sample)
            // for IIS/WCF integration examples.

            // ── WCF Extension ──────────────────────────────────────
            // Not applicable to .NET 8. See US-027 (.NET Framework 4.8 sample).

            // ── Console Telemetry Sink ─────────────────────────────
            if (configuration.GetValue("Telemetry:ConsoleSink:Enabled", true))
            {
                services.AddHostedService<ConsoleTelemetrySink>();
            }

            return services;
        }

        /// <summary>
        /// Demonstrates the multi-level configuration hierarchy:
        /// Global → Namespace → Type → Method.
        /// Each level can override sampling, parameter capture, and other settings.
        /// </summary>
        private static void ConfigureMultiLevelTelemetry()
        {
            var configurator = new TelemetryConfigurator();

            // Global defaults — applies to everything
            configurator.Global()
                .SamplingRate(1.0)           // Sample 100% in dev
                .CaptureParameters(ParameterCaptureMode.NamesOnly)
                .RecordExceptions(true)
                .TimeoutThreshold(5000)      // 5 seconds
                .AddTag("app", "HVO.Samples.Net8")
                .Apply();

            // Namespace-level — reduce sampling for noisy namespaces
            configurator.Namespace("HVO.Enterprise.Samples.Net8.BackgroundServices")
                .SamplingRate(0.5)           // 50% sampling for background work
                .CaptureParameters(ParameterCaptureMode.None)
                .Apply();

            // Type-level — detailed capture for the core weather service
            configurator.ForType<WeatherService>()
                .SamplingRate(1.0)
                .CaptureParameters(ParameterCaptureMode.NamesAndValues)
                .RecordExceptions(true)
                .TimeoutThreshold(10000)     // 10 seconds (external API)
                .Apply();

            // Method-level — full capture for a specific critical method
            // (Using MethodInfo lookup — in real code, get via reflection)
            // configurator.ForMethod(typeof(WeatherService).GetMethod("GetCurrentWeatherAsync")!)
            //     .CaptureParameters(ParameterCaptureMode.Full)
            //     .TimeoutThreshold(15000)
            //     .Apply();
        }
    }
}
