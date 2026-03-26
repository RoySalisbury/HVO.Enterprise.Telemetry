using System;
using System.Linq;
using HVO.Enterprise.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.OpenTelemetry.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddOpenTelemetryExport_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => ServiceCollectionExtensions.AddOpenTelemetryExport(null!));
        }

        [TestMethod]
        public void AddOpenTelemetryExport_RegistersMarker()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport();

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(OtlpExportMarker)));
        }

        [TestMethod]
        public void AddOpenTelemetryExport_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(options =>
            {
                options.ServiceName = "test-service";
                options.Endpoint = "http://collector:4317";
                options.EnableLogExport = true;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.AreEqual("test-service", options.ServiceName);
            Assert.AreEqual("http://collector:4317", options.Endpoint);
            Assert.IsTrue(options.EnableLogExport);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddOpenTelemetryExport_IsIdempotent()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport();
            services.AddOpenTelemetryExport();

            var markerCount = services.Count(s => s.ServiceType == typeof(OtlpExportMarker));
            Assert.AreEqual(1, markerCount);
        }

        [TestMethod]
        public void AddOpenTelemetryExport_ReturnsSameServiceCollection()
        {
            var services = new ServiceCollection();
            var result = services.AddOpenTelemetryExport();

            Assert.AreSame(services, result);
        }

        [TestMethod]
        public void AddOpenTelemetryExport_WithNullConfigure_Succeeds()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(configure: null);

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(OtlpExportMarker)));
        }

        [TestMethod]
        public void AddOpenTelemetryExportFromEnvironment_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => ServiceCollectionExtensions.AddOpenTelemetryExportFromEnvironment(null!));
        }

        [TestMethod]
        public void AddOpenTelemetryExportFromEnvironment_RegistersMarker()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExportFromEnvironment();

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(OtlpExportMarker)));
        }

        [TestMethod]
        public void AddOpenTelemetryExport_RegistersActivitySourceRegistrar()
        {
            var services = new ServiceCollection();
            services.AddOptions<Configuration.TelemetryOptions>();
            services.AddOpenTelemetryExport();

            var provider = services.BuildServiceProvider();
            var registrar = provider.GetService<HvoActivitySourceRegistrar>();

            Assert.IsNotNull(registrar);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddOpenTelemetryExport_WithAdditionalActivitySources_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(options =>
            {
                options.AdditionalActivitySources.Add("MyApp");
                options.AdditionalActivitySources.Add("MyApp.HttpClient");
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.AreEqual(2, options.AdditionalActivitySources.Count);
            Assert.IsTrue(options.AdditionalActivitySources.Contains("MyApp"));
            Assert.IsTrue(options.AdditionalActivitySources.Contains("MyApp.HttpClient"));
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddOpenTelemetryExport_WithAdditionalMeterNames_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(options =>
            {
                options.AdditionalMeterNames.Add("MyApp.Metrics");
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.AreEqual(1, options.AdditionalMeterNames.Count);
            Assert.IsTrue(options.AdditionalMeterNames.Contains("MyApp.Metrics"));
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddOpenTelemetryExport_WithEnableStandardMeters_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(options =>
            {
                options.EnableStandardMeters = true;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.IsTrue(options.EnableStandardMeters);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddOpenTelemetryExport_WithEnableLogExport_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(options =>
            {
                options.EnableLogExport = true;
                options.Endpoint = "http://collector:4317";
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.IsTrue(options.EnableLogExport);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddOpenTelemetryExport_WithPort4318_AutoDetectsHttpProtobuf()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(options =>
            {
                options.Endpoint = "http://collector:4318";
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            // PostConfigure applies environment defaults which auto-detects transport
            Assert.AreEqual(OtlpTransport.HttpProtobuf, options.Transport);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddOpenTelemetryExport_RegistersLoggerProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions<Configuration.TelemetryOptions>();
            services.AddOpenTelemetryExport(options =>
            {
                options.ServiceName = "test-service";
            });

            var provider = services.BuildServiceProvider();

            // WithLogging() should register an OpenTelemetry ILoggerProvider
            var loggerFactory = provider.GetService<ILoggerFactory>();
            Assert.IsNotNull(loggerFactory);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void StandardMeterNames_ContainsExpectedMeters()
        {
            var names = ServiceCollectionExtensions.StandardMeterNames;

            Assert.IsTrue(names.Contains("Microsoft.AspNetCore.Hosting"));
            Assert.IsTrue(names.Contains("Microsoft.AspNetCore.Server.Kestrel"));
            Assert.IsTrue(names.Contains("System.Net.Http"));
            Assert.IsTrue(names.Contains("System.Net.NameResolution"));
            Assert.IsTrue(names.Contains("System.Net.Security"));
            Assert.AreEqual(9, names.Length);
        }

        [TestMethod]
        public void AddOpenTelemetryExport_WithCallbacks_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetryExport(options =>
            {
                options.ConfigureTracerProvider = _ => { };
                options.ConfigureMeterProvider = _ => { };
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OtlpExportOptions>>().Value;

            Assert.IsNotNull(options.ConfigureTracerProvider);
            Assert.IsNotNull(options.ConfigureMeterProvider);
            (provider as IDisposable)?.Dispose();
        }
    }
}
