using System;
using System.Linq;
using HVO.Enterprise.Telemetry.AppInsights;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.AppInsights.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddAppInsightsTelemetry_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => ServiceCollectionExtensions.AddAppInsightsTelemetry(
                    null!, (Action<AppInsightsOptions>?)null));
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_WithConnectionString_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => ServiceCollectionExtensions.AddAppInsightsTelemetry(
                    null!, "connection-string"));
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_WithEmptyConnectionString_ThrowsArgumentException()
        {
            var services = new ServiceCollection();
            Assert.ThrowsExactly<ArgumentException>(
                () => services.AddAppInsightsTelemetry(""));
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_WithNullConnectionString_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => services.AddAppInsightsTelemetry((string)null!));
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_RegistersBridge()
        {
            var services = new ServiceCollection();
            services.AddAppInsightsTelemetry(options =>
            {
                options.ConnectionString = "InstrumentationKey=test-00000000-0000-0000-0000-000000000000";
                options.ForceOtlpMode = false;
            });

            var provider = services.BuildServiceProvider();
            var bridge = provider.GetService<ApplicationInsightsBridge>();

            Assert.IsNotNull(bridge);
            bridge?.Dispose();
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_RegistersTelemetryClient()
        {
            var services = new ServiceCollection();
            services.AddAppInsightsTelemetry(options =>
            {
                options.ConnectionString = "InstrumentationKey=test-00000000-0000-0000-0000-000000000000";
            });

            var provider = services.BuildServiceProvider();
            var client = provider.GetService<TelemetryClient>();

            Assert.IsNotNull(client);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_RegistersTelemetryConfiguration()
        {
            var services = new ServiceCollection();
            services.AddAppInsightsTelemetry(options =>
            {
                options.ConnectionString = "InstrumentationKey=test-00000000-0000-0000-0000-000000000000";
            });

            var provider = services.BuildServiceProvider();
            var config = provider.GetService<TelemetryConfiguration>();

            Assert.IsNotNull(config);
            config?.Dispose();
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddAppInsightsTelemetry(options =>
            {
                options.ConnectionString = "InstrumentationKey=custom-key";
                options.EnableBridge = false;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<AppInsightsOptions>>().Value;

            Assert.AreEqual("InstrumentationKey=custom-key", options.ConnectionString);
            Assert.IsFalse(options.EnableBridge);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_IsIdempotent()
        {
            var services = new ServiceCollection();
            services.AddAppInsightsTelemetry(options =>
            {
                options.ConnectionString = "InstrumentationKey=test-00000000-0000-0000-0000-000000000000";
            });
            services.AddAppInsightsTelemetry(options =>
            {
                options.ConnectionString = "InstrumentationKey=test-00000000-0000-0000-0000-000000000000";
            });

            var bridgeCount = services.Count(s => s.ServiceType == typeof(ApplicationInsightsBridge));
            Assert.AreEqual(1, bridgeCount);
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_WithConnectionStringOverload_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddAppInsightsTelemetry("InstrumentationKey=simple-key");

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<AppInsightsOptions>>().Value;

            Assert.AreEqual("InstrumentationKey=simple-key", options.ConnectionString);
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_ReturnsSameServiceCollection()
        {
            var services = new ServiceCollection();
            var result = services.AddAppInsightsTelemetry(options =>
            {
                options.ConnectionString = "InstrumentationKey=test";
            });

            Assert.AreSame(services, result);
        }

        [TestMethod]
        public void AddAppInsightsTelemetry_TelemetryConfigurationHasHvoInitializers()
        {
            var services = new ServiceCollection();
            services.AddAppInsightsTelemetry(options =>
            {
                options.ConnectionString = "InstrumentationKey=test-00000000-0000-0000-0000-000000000000";
            });

            var provider = services.BuildServiceProvider();
            var config = provider.GetRequiredService<TelemetryConfiguration>();

            Assert.IsTrue(
                config.TelemetryInitializers.OfType<ActivityTelemetryInitializer>().Any(),
                "Should have ActivityTelemetryInitializer");
            Assert.IsTrue(
                config.TelemetryInitializers.OfType<CorrelationTelemetryInitializer>().Any(),
                "Should have CorrelationTelemetryInitializer");

            config.Dispose();
            (provider as IDisposable)?.Dispose();
        }
    }
}
