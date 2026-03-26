using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HVO.Enterprise.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Extension methods for registering OpenTelemetry OTLP export with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Well-known .NET runtime and ASP.NET Core meter names registered when
        /// <see cref="OtlpExportOptions.EnableStandardMeters"/> is <see langword="true"/>.
        /// </summary>
        internal static readonly string[] StandardMeterNames = new[]
        {
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "Microsoft.AspNetCore.Http.Connections",
            "Microsoft.AspNetCore.Routing",
            "Microsoft.AspNetCore.Diagnostics",
            "Microsoft.AspNetCore.RateLimiting",
            "System.Net.Http",
            "System.Net.NameResolution",
            "System.Net.Security",
        };
        /// <summary>
        /// Adds OpenTelemetry OTLP export for traces, metrics, and optionally logs.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure <see cref="OtlpExportOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// <para>This method is idempotent — calling it multiple times will not add duplicate registrations.</para>
        /// <para>Registers TracerProvider and MeterProvider with all HVO ActivitySource and Meter names,
        /// exporting via OTLP to the configured collector endpoint. Environment variable defaults
        /// (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, <c>OTEL_SERVICE_NAME</c>, etc.) are applied
        /// automatically after explicit configuration.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddTelemetry();
        /// services.AddOpenTelemetryExport(options =>
        /// {
        ///     options.ServiceName = "my-service";
        ///     options.Endpoint = "http://otel-collector:4317";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddOpenTelemetryExport(
            this IServiceCollection services,
            Action<OtlpExportOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Idempotency guard
            if (services.Any(s => s.ServiceType == typeof(OtlpExportMarker)))
            {
                return services;
            }

            services.AddSingleton<OtlpExportMarker>();

            // Configure options
            var optionsBuilder = services.AddOptions<OtlpExportOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Apply environment variable defaults after all Configure delegates
            services.PostConfigure<OtlpExportOptions>(o => o.ApplyEnvironmentDefaults());

            // Register activity source registrar
            services.TryAddSingleton<HvoActivitySourceRegistrar>();

            // Register OTel SDK infrastructure (including logging support for OTLP log export)
            services.AddOpenTelemetry()
                .WithLogging();

            // Configure TracerProvider with HVO activity sources, additional sources, and OTLP exporter
            services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
            {
                var options = sp.GetRequiredService<IOptions<OtlpExportOptions>>().Value;
                var registrar = sp.GetRequiredService<HvoActivitySourceRegistrar>();

                foreach (var source in registrar.GetSourceNames())
                {
                    builder.AddSource(source);
                }

                foreach (var source in options.AdditionalActivitySources)
                {
                    builder.AddSource(source);
                }

                builder.ConfigureResource(CreateResourceAction(options));

                if (options.EnableTraceExport)
                {
                    builder.AddOtlpExporter(exporterOptions =>
                    {
                        MapExporterOptions(exporterOptions, options);
                    });
                }

                options.ConfigureTracerProvider?.Invoke(builder);
            });

            // Configure MeterProvider with HVO meter, additional meters, standard meters, and OTLP exporter
            services.ConfigureOpenTelemetryMeterProvider((sp, builder) =>
            {
                var options = sp.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

                builder.AddMeter("HVO.Enterprise.Telemetry");

                foreach (var meter in options.AdditionalMeterNames)
                {
                    builder.AddMeter(meter);
                }

                if (options.EnableStandardMeters)
                {
                    foreach (var meter in StandardMeterNames)
                    {
                        builder.AddMeter(meter);
                    }
                }

                builder.ConfigureResource(CreateResourceAction(options));

                if (options.EnableMetricsExport)
                {
                    builder.AddOtlpExporter((exporterOptions, metricReaderOptions) =>
                    {
                        MapExporterOptions(exporterOptions, options);
                        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds =
                            (int)options.MetricsExportInterval.TotalMilliseconds;
                        metricReaderOptions.TemporalityPreference = options.TemporalityPreference == MetricsTemporality.Delta
                            ? MetricReaderTemporalityPreference.Delta
                            : MetricReaderTemporalityPreference.Cumulative;
                    });
                }

                options.ConfigureMeterProvider?.Invoke(builder);
            });

            // Configure LoggerProvider with OTLP log export when enabled
            services.ConfigureOpenTelemetryLoggerProvider((sp, builder) =>
            {
                var options = sp.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

                builder.ConfigureResource(CreateResourceAction(options));

                if (options.EnableLogExport)
                {
                    builder.AddOtlpExporter(exporterOptions =>
                    {
                        MapExporterOptions(exporterOptions, options);
                    });
                }
            });

            return services;
        }

        /// <summary>
        /// Adds OpenTelemetry OTLP export with environment-variable-only configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <remarks>
        /// Reads all configuration from OpenTelemetry environment variables
        /// (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, <c>OTEL_SERVICE_NAME</c>,
        /// <c>OTEL_RESOURCE_ATTRIBUTES</c>). Environment variable defaults are applied
        /// automatically via <c>PostConfigure</c>.
        /// </remarks>
        public static IServiceCollection AddOpenTelemetryExportFromEnvironment(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddOpenTelemetryExport();
        }

        private static Action<ResourceBuilder> CreateResourceAction(OtlpExportOptions options)
        {
            return resource =>
            {
                if (!string.IsNullOrEmpty(options.ServiceName))
                {
                    resource.AddService(
                        serviceName: options.ServiceName!,
                        serviceVersion: options.ServiceVersion);
                }

                var extraAttributes = new List<KeyValuePair<string, object>>();

                if (!string.IsNullOrEmpty(options.Environment))
                {
                    extraAttributes.Add(
                        new KeyValuePair<string, object>("deployment.environment", options.Environment!));
                }

                foreach (var attr in options.ResourceAttributes)
                {
                    extraAttributes.Add(new KeyValuePair<string, object>(attr.Key, attr.Value));
                }

                if (extraAttributes.Count > 0)
                {
                    resource.AddAttributes(extraAttributes);
                }
            };
        }

        private static void MapExporterOptions(
            OtlpExporterOptions exporterOptions,
            OtlpExportOptions options)
        {
            exporterOptions.Endpoint = new Uri(options.Endpoint);
            exporterOptions.Protocol = options.Transport == OtlpTransport.Grpc
                ? OtlpExportProtocol.Grpc
                : OtlpExportProtocol.HttpProtobuf;

            if (options.Headers.Count > 0)
            {
                exporterOptions.Headers = string.Join(",",
                    options.Headers.Select(h => $"{h.Key}={h.Value}"));
            }
        }
    }

    /// <summary>
    /// Marker type for idempotency guard. Prevents duplicate OpenTelemetry export registrations.
    /// </summary>
    internal sealed class OtlpExportMarker { }
}
