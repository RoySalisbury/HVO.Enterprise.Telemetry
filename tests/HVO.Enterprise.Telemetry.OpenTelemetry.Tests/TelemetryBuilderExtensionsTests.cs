using System;
using System.Linq;
using HVO.Enterprise.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.OpenTelemetry.Tests
{
    [TestClass]
    public class TelemetryBuilderExtensionsTests
    {
        [TestMethod]
        public void WithOpenTelemetry_NullBuilder_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryBuilderExtensions.WithOpenTelemetry(
                    null!, (Action<OtlpExportOptions>?)null));
        }

        [TestMethod]
        public void WithOpenTelemetry_RegistersOtlpExport()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithOpenTelemetry(options =>
                {
                    options.ServiceName = "test-service";
                });
            });

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(OtlpExportMarker)));
        }

        [TestMethod]
        public void WithOpenTelemetry_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithOpenTelemetry(options =>
                {
                    options.ServiceName = "builder-test";
                    options.Endpoint = "http://custom:4317";
                });
            });

            var provider = services.BuildServiceProvider();
            var opts = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.AreEqual("builder-test", opts.ServiceName);
            Assert.AreEqual("http://custom:4317", opts.Endpoint);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void WithOpenTelemetry_ReturnsSameBuilder()
        {
            var services = new ServiceCollection();

            services.AddTelemetry(builder =>
            {
                var result = builder.WithOpenTelemetry(options =>
                {
                    options.ServiceName = "test-service";
                });

                Assert.AreSame(builder, result);
            });
        }

        [TestMethod]
        public void WithOpenTelemetry_WithoutOptions_RegistersExport()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithOpenTelemetry();
            });

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(OtlpExportMarker)));
        }

        [TestMethod]
        public void WithPrometheusEndpoint_NullBuilder_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryBuilderExtensions.WithPrometheusEndpoint(null!));
        }

        [TestMethod]
        public void WithPrometheusEndpoint_SetsOptions()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithPrometheusEndpoint("/custom-metrics");
            });

            var provider = services.BuildServiceProvider();
            var opts = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.IsTrue(opts.EnablePrometheusEndpoint);
            Assert.AreEqual("/custom-metrics", opts.PrometheusEndpointPath);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void WithPrometheusEndpoint_DefaultPath_UsesMetrics()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithPrometheusEndpoint();
            });

            var provider = services.BuildServiceProvider();
            var opts = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.IsTrue(opts.EnablePrometheusEndpoint);
            Assert.AreEqual("/metrics", opts.PrometheusEndpointPath);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void WithPrometheusEndpoint_ReturnsSameBuilder()
        {
            var services = new ServiceCollection();

            services.AddTelemetry(builder =>
            {
                var result = builder.WithPrometheusEndpoint();
                Assert.AreSame(builder, result);
            });
        }

        [TestMethod]
        public void WithOtlpLogExport_NullBuilder_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryBuilderExtensions.WithOtlpLogExport(null!));
        }

        [TestMethod]
        public void WithOtlpLogExport_EnablesLogExport()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithOtlpLogExport();
            });

            var provider = services.BuildServiceProvider();
            var opts = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.IsTrue(opts.EnableLogExport);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void WithOtlpLogExport_ReturnsSameBuilder()
        {
            var services = new ServiceCollection();

            services.AddTelemetry(builder =>
            {
                var result = builder.WithOtlpLogExport();
                Assert.AreSame(builder, result);
            });
        }
    }
}
