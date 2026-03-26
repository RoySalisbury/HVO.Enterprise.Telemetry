using System;
using HVO.Enterprise.Telemetry.AppInsights;
using Microsoft.Extensions.DependencyInjection;

namespace HVO.Enterprise.Telemetry.AppInsights.Tests
{
    [TestClass]
    public class TelemetryBuilderExtensionsTests
    {
        [TestMethod]
        public void WithAppInsights_NullBuilder_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryBuilderExtensions.WithAppInsights(
                    null!, (Action<AppInsightsOptions>?)null));
        }

        [TestMethod]
        public void WithAppInsights_ConnectionString_NullBuilder_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryBuilderExtensions.WithAppInsights(
                    null!, "connection-string"));
        }

        [TestMethod]
        public void WithAppInsights_WithOptions_RegistersBridge()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithAppInsights(options =>
                {
                    options.ConnectionString = "InstrumentationKey=test-00000000-0000-0000-0000-000000000000";
                    options.ForceOtlpMode = false;
                });
            });

            var provider = services.BuildServiceProvider();
            var bridge = provider.GetService<ApplicationInsightsBridge>();

            Assert.IsNotNull(bridge);
            bridge?.Dispose();
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void WithAppInsights_WithConnectionString_RegistersBridge()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithAppInsights("InstrumentationKey=test-00000000-0000-0000-0000-000000000000");
            });

            var provider = services.BuildServiceProvider();
            var bridge = provider.GetService<ApplicationInsightsBridge>();

            Assert.IsNotNull(bridge);
            bridge?.Dispose();
            (provider as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void WithAppInsights_ReturnsSameBuilder()
        {
            var services = new ServiceCollection();
            TelemetryBuilder? capturedBuilder = null;

            services.AddTelemetry(builder =>
            {
                capturedBuilder = builder;
                var result = builder.WithAppInsights(options =>
                {
                    options.ConnectionString = "InstrumentationKey=test";
                });

                Assert.AreSame(builder, result);
            });
        }
    }
}
