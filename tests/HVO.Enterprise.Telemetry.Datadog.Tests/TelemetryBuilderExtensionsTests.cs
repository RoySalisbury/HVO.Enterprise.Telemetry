using System;
using HVO.Enterprise.Telemetry.Datadog;
using Microsoft.Extensions.DependencyInjection;

namespace HVO.Enterprise.Telemetry.Datadog.Tests
{
    [TestClass]
    public class TelemetryBuilderExtensionsTests
    {
        [TestMethod]
        public void WithDatadog_NullBuilder_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryBuilderExtensions.WithDatadog(
                    null!, (Action<DatadogOptions>?)null));
        }

        [TestMethod]
        public void WithDatadog_WithOptions_RegistersMetricsExporter()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithDatadog(options =>
                {
                    options.ServiceName = "test-service";
                });
            });

            var provider = services.BuildServiceProvider();
            var exporter = provider.GetService<DatadogMetricsExporter>();

            Assert.IsNotNull(exporter);
            exporter?.Dispose();
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void WithDatadog_WithOptions_RegistersTraceExporter()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithDatadog(options =>
                {
                    options.ServiceName = "test-service";
                });
            });

            var provider = services.BuildServiceProvider();
            var exporter = provider.GetService<DatadogTraceExporter>();

            Assert.IsNotNull(exporter);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void WithDatadog_ReturnsSameBuilder()
        {
            var services = new ServiceCollection();
            TelemetryBuilder? capturedBuilder = null;

            services.AddTelemetry(builder =>
            {
                capturedBuilder = builder;
                var result = builder.WithDatadog(options =>
                {
                    options.ServiceName = "test-service";
                });

                Assert.AreSame(builder, result);
            });
        }

        [TestMethod]
        public void WithDatadog_WithoutOptions_RegistersExporters()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithDatadog();
            });

            var provider = services.BuildServiceProvider();
            var metrics = provider.GetService<DatadogMetricsExporter>();
            var traces = provider.GetService<DatadogTraceExporter>();

            Assert.IsNotNull(metrics);
            Assert.IsNotNull(traces);
            metrics?.Dispose();
            (provider as IDisposable)?.Dispose();
        }
    }
}
