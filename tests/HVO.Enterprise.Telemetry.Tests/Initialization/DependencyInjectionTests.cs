using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Initialization
{
    [TestClass]
    public class DependencyInjectionTests
    {
        /// <summary>
        /// Mock implementation of IHostApplicationLifetime for testing.
        /// </summary>
        private sealed class MockHostApplicationLifetime : IHostApplicationLifetime
        {
            private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
            private readonly CancellationTokenSource _stoppedCts = new CancellationTokenSource();
            private readonly CancellationTokenSource _startedCts = new CancellationTokenSource();

            public CancellationToken ApplicationStarted => _startedCts.Token;
            public CancellationToken ApplicationStopping => _stoppingCts.Token;
            public CancellationToken ApplicationStopped => _stoppedCts.Token;

            public void StopApplication()
            {
                if (!_stoppingCts.IsCancellationRequested) _stoppingCts.Cancel();
                if (!_stoppedCts.IsCancellationRequested) _stoppedCts.Cancel();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clear static state that may be set by hosted service bridge
            Telemetry.ClearInstance();
        }

        private static ServiceCollection CreateBaseServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHostApplicationLifetime>(new MockHostApplicationLifetime());
            return services;
        }

        [TestMethod]
        public void AddTelemetry_RegistersITelemetryService()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options => options.ServiceName = "Test");

            using var provider = services.BuildServiceProvider();
            var service = provider.GetService<ITelemetryService>();

            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void AddTelemetry_RegistersTelemetryServiceConcrete()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options => options.ServiceName = "Test");

            using var provider = services.BuildServiceProvider();
            var service = provider.GetService<TelemetryService>();

            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void AddTelemetry_RegistersStatistics()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options => options.ServiceName = "Test");

            using var provider = services.BuildServiceProvider();
            var stats = provider.GetService<ITelemetryStatistics>();

            Assert.IsNotNull(stats);
        }

        [TestMethod]
        public void AddTelemetry_RegistersCorrelationIdProvider()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options => options.ServiceName = "Test");

            using var provider = services.BuildServiceProvider();
            var correlationProvider = provider.GetService<ICorrelationIdProvider>();

            Assert.IsNotNull(correlationProvider);
        }

        [TestMethod]
        public void AddTelemetry_RegistersOperationScopeFactory()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options => options.ServiceName = "Test");

            using var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetService<IOperationScopeFactory>();

            Assert.IsNotNull(scopeFactory);
        }

        [TestMethod]
        public void AddTelemetry_RegistersHostedService()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options => options.ServiceName = "Test");

            using var provider = services.BuildServiceProvider();
            var hostedServices = provider.GetServices<IHostedService>();

            Assert.IsTrue(hostedServices.Any(s => s is TelemetryHostedService));
        }

        [TestMethod]
        public void AddTelemetry_ConfiguresOptions()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options =>
            {
                options.ServiceName = "MyService";
                options.ServiceVersion = "2.0.0";
                options.Environment = "Staging";
                options.DefaultSamplingRate = 0.5;
            });

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            Assert.AreEqual("MyService", options.Value.ServiceName);
            Assert.AreEqual("2.0.0", options.Value.ServiceVersion);
            Assert.AreEqual("Staging", options.Value.Environment);
            Assert.AreEqual(0.5, options.Value.DefaultSamplingRate);
        }

        [TestMethod]
        public void AddTelemetry_ThrowsOnNullServices()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryServiceCollectionExtensions.AddTelemetry(null!));
        }

        [TestMethod]
        public void AddTelemetry_IsIdempotent()
        {
            var services = CreateBaseServices();

            // Call twice
            services.AddTelemetry(options => options.ServiceName = "Test");
            services.AddTelemetry(options => options.ServiceName = "Duplicate");

            using var provider = services.BuildServiceProvider();

            // Should only have one TelemetryService registration
            var telemetryServices = services.Where(s => s.ServiceType == typeof(TelemetryService));
            Assert.AreEqual(1, telemetryServices.Count());
        }

        [TestMethod]
        public void AddTelemetry_InterfaceAndConcreteAreSameInstance()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options => options.ServiceName = "Test");

            using var provider = services.BuildServiceProvider();
            var concrete = provider.GetRequiredService<TelemetryService>();
            var iface = provider.GetRequiredService<ITelemetryService>();

            Assert.AreSame(concrete, iface);
        }

        [TestMethod]
        public void AddTelemetry_ReturnsServiceCollectionForChaining()
        {
            var services = CreateBaseServices();
            var result = services.AddTelemetry(options => options.ServiceName = "Test");

            Assert.AreSame(services, result);
        }

        [TestMethod]
        public void AddTelemetry_WithNullConfigure_UsesDefaults()
        {
            var services = CreateBaseServices();
            services.AddTelemetry((Action<TelemetryOptions>?)null);

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            Assert.AreEqual("Unknown", options.Value.ServiceName);
            Assert.IsTrue(options.Value.Enabled);
        }

        [TestMethod]
        public async Task HostedService_StartsAndStopsService()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options => options.ServiceName = "HostedTest");

            using var provider = services.BuildServiceProvider();
            var hostedService = provider.GetServices<IHostedService>()
                .OfType<TelemetryHostedService>()
                .First();

            // Start
            await hostedService.StartAsync(CancellationToken.None);
            Assert.IsTrue(Telemetry.IsInitialized, "Static Telemetry should be initialized after hosted service start");

            // Stop
            await hostedService.StopAsync(CancellationToken.None);
            Assert.IsFalse(Telemetry.IsInitialized, "Static Telemetry should be cleared after hosted service stop");
        }

        [TestMethod]
        public async Task HostedService_BridgesStaticApi()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(options => options.ServiceName = "BridgeTest");

            using var provider = services.BuildServiceProvider();
            var hostedService = provider.GetServices<IHostedService>()
                .OfType<TelemetryHostedService>()
                .First();

            await hostedService.StartAsync(CancellationToken.None);

            // Static API should now work
            using var scope = Telemetry.StartOperation("TestOp");
            Assert.IsNotNull(scope);

            Telemetry.TrackEvent("TestEvent");
            Telemetry.RecordMetric("TestMetric", 1.0);

            await hostedService.StopAsync(CancellationToken.None);
        }
    }
}
