using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Datadog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Datadog.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddDatadogTelemetry_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => ServiceCollectionExtensions.AddDatadogTelemetry(null!));
        }

        [TestMethod]
        public void AddDatadogTelemetry_RegistersMetricsExporter()
        {
            var services = new ServiceCollection();
            services.AddDatadogTelemetry();

            var provider = services.BuildServiceProvider();
            var exporter = provider.GetService<DatadogMetricsExporter>();

            Assert.IsNotNull(exporter);
            exporter?.Dispose();
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddDatadogTelemetry_RegistersTraceExporter()
        {
            var services = new ServiceCollection();
            services.AddDatadogTelemetry();

            var provider = services.BuildServiceProvider();
            var exporter = provider.GetService<DatadogTraceExporter>();

            Assert.IsNotNull(exporter);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddDatadogTelemetry_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddDatadogTelemetry(options =>
            {
                options.ServiceName = "my-service";
                options.Environment = "staging";
                options.AgentPort = 9999;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<DatadogOptions>>().Value;

            Assert.AreEqual("my-service", options.ServiceName);
            Assert.AreEqual("staging", options.Environment);
            Assert.AreEqual(9999, options.AgentPort);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddDatadogTelemetry_IsIdempotent()
        {
            var services = new ServiceCollection();
            services.AddDatadogTelemetry();
            services.AddDatadogTelemetry();

            var metricsCount = services.Count(s => s.ServiceType == typeof(DatadogMetricsExporter));
            Assert.AreEqual(1, metricsCount);
        }

        [TestMethod]
        public void AddDatadogTelemetry_ReturnsSameServiceCollection()
        {
            var services = new ServiceCollection();
            var result = services.AddDatadogTelemetry();

            Assert.AreSame(services, result);
        }

        [TestMethod]
        public void AddDatadogTelemetryFromEnvironment_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => ServiceCollectionExtensions.AddDatadogTelemetryFromEnvironment(null!));
        }

        [TestMethod]
        public void AddDatadogTelemetryFromEnvironment_RegistersServices()
        {
            var services = new ServiceCollection();
            services.AddDatadogTelemetryFromEnvironment();

            var provider = services.BuildServiceProvider();
            var metrics = provider.GetService<DatadogMetricsExporter>();
            var traces = provider.GetService<DatadogTraceExporter>();

            Assert.IsNotNull(metrics);
            Assert.IsNotNull(traces);
            metrics?.Dispose();
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddDatadogTelemetry_WithDisabledTraceExporter_StillRegisters()
        {
            var services = new ServiceCollection();
            services.AddDatadogTelemetry(options =>
            {
                options.EnableTraceExporter = false;
            });

            var provider = services.BuildServiceProvider();
            var exporter = provider.GetService<DatadogTraceExporter>();

            // Trace exporter is always registered; disabled just uses a lightweight default
            Assert.IsNotNull(exporter);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddDatadogTelemetry_WithNullConfigure_Succeeds()
        {
            var services = new ServiceCollection();
            services.AddDatadogTelemetry(configure: null);

            var provider = services.BuildServiceProvider();
            var metrics = provider.GetService<DatadogMetricsExporter>();

            Assert.IsNotNull(metrics);
            metrics?.Dispose();
            (provider as IDisposable)?.Dispose();
        }
    }
}
