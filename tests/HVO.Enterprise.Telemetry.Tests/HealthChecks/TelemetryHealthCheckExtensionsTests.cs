using System;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace HVO.Enterprise.Telemetry.Tests.HealthChecks
{
    [TestClass]
    public class TelemetryHealthCheckExtensionsTests
    {
        [TestMethod]
        public void AddTelemetryStatistics_RegistersSingletonInterface()
        {
            var services = new ServiceCollection();

            services.AddTelemetryStatistics();

            var provider = services.BuildServiceProvider();
            var stats = provider.GetService<ITelemetryStatistics>();

            Assert.IsNotNull(stats);
        }

        [TestMethod]
        public void AddTelemetryStatistics_RegistersConcreteType()
        {
            var services = new ServiceCollection();

            services.AddTelemetryStatistics();

            var provider = services.BuildServiceProvider();
            var stats = provider.GetService<TelemetryStatistics>();

            Assert.IsNotNull(stats);
        }

        [TestMethod]
        public void AddTelemetryStatistics_ReturnsSameInstance()
        {
            var services = new ServiceCollection();

            services.AddTelemetryStatistics();

            var provider = services.BuildServiceProvider();
            var iface = provider.GetService<ITelemetryStatistics>();
            var concrete = provider.GetService<TelemetryStatistics>();

            Assert.AreSame(iface, concrete);
        }

        [TestMethod]
        public void AddTelemetryStatistics_NullServices_Throws()
        {
            IServiceCollection? services = null;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => services!.AddTelemetryStatistics());
        }

        [TestMethod]
        public void AddTelemetryHealthCheck_RegistersHealthCheck()
        {
            var services = new ServiceCollection();
            services.AddTelemetryStatistics();
            services.AddTelemetryHealthCheck();

            var provider = services.BuildServiceProvider();
            var healthCheck = provider.GetService<TelemetryHealthCheck>();

            Assert.IsNotNull(healthCheck);
        }

        [TestMethod]
        public void AddTelemetryHealthCheck_WithOptions_UsesOptions()
        {
            var services = new ServiceCollection();
            services.AddTelemetryStatistics();
            var options = new TelemetryHealthCheckOptions
            {
                MaxExpectedQueueDepth = 5000
            };
            services.AddTelemetryHealthCheck(options);

            var provider = services.BuildServiceProvider();
            var healthCheck = provider.GetService<TelemetryHealthCheck>();

            Assert.IsNotNull(healthCheck);
        }

        [TestMethod]
        public void AddTelemetryHealthCheck_NullServices_Throws()
        {
            IServiceCollection? services = null;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => services!.AddTelemetryHealthCheck());
        }

        [TestMethod]
        public void AddTelemetryHealthCheck_WithoutStatistics_ThrowsOnResolve()
        {
            var services = new ServiceCollection();
            services.AddTelemetryHealthCheck();

            var provider = services.BuildServiceProvider();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => provider.GetRequiredService<TelemetryHealthCheck>());
        }

        [TestMethod]
        public void AddTelemetryHealthCheck_InvalidOptions_ThrowsOnRegistration()
        {
            var services = new ServiceCollection();
            services.AddTelemetryStatistics();
            var badOptions = new TelemetryHealthCheckOptions
            {
                DegradedErrorRateThreshold = -1.0
            };

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => services.AddTelemetryHealthCheck(badOptions));
        }
    }
}
