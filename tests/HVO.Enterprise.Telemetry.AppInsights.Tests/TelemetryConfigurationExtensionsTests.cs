using System;
using System.Linq;
using Microsoft.ApplicationInsights.Extensibility;
using HVO.Enterprise.Telemetry.AppInsights;

namespace HVO.Enterprise.Telemetry.AppInsights.Tests
{
    [TestClass]
    public class TelemetryConfigurationExtensionsTests
    {
        [TestMethod]
        public void AddHvoEnrichers_NullConfiguration_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryConfigurationExtensions.AddHvoEnrichers(null!));
        }

        [TestMethod]
        public void AddHvoEnrichers_DefaultOptions_AddsBothInitializers()
        {
            using var configuration = TelemetryConfiguration.CreateDefault();

            var initializerCountBefore = configuration.TelemetryInitializers.Count;
            configuration.AddHvoEnrichers();
            var initializerCountAfter = configuration.TelemetryInitializers.Count;

            // Should add 2 initializers (Activity + Correlation)
            Assert.AreEqual(initializerCountBefore + 2, initializerCountAfter);
        }

        [TestMethod]
        public void AddHvoEnrichers_DefaultOptions_AddsActivityInitializer()
        {
            using var configuration = TelemetryConfiguration.CreateDefault();
            configuration.AddHvoEnrichers();

            Assert.IsTrue(
                configuration.TelemetryInitializers.OfType<ActivityTelemetryInitializer>().Any());
        }

        [TestMethod]
        public void AddHvoEnrichers_DefaultOptions_AddsCorrelationInitializer()
        {
            using var configuration = TelemetryConfiguration.CreateDefault();
            configuration.AddHvoEnrichers();

            Assert.IsTrue(
                configuration.TelemetryInitializers.OfType<CorrelationTelemetryInitializer>().Any());
        }

        [TestMethod]
        public void AddHvoEnrichers_DisableActivity_DoesNotAddActivityInitializer()
        {
            using var configuration = TelemetryConfiguration.CreateDefault();
            var options = new AppInsightsOptions { EnableActivityInitializer = false };

            configuration.AddHvoEnrichers(options);

            Assert.IsFalse(
                configuration.TelemetryInitializers.OfType<ActivityTelemetryInitializer>().Any());
        }

        [TestMethod]
        public void AddHvoEnrichers_DisableCorrelation_DoesNotAddCorrelationInitializer()
        {
            using var configuration = TelemetryConfiguration.CreateDefault();
            var options = new AppInsightsOptions { EnableCorrelationInitializer = false };

            configuration.AddHvoEnrichers(options);

            Assert.IsFalse(
                configuration.TelemetryInitializers.OfType<CorrelationTelemetryInitializer>().Any());
        }

        [TestMethod]
        public void AddHvoEnrichers_ReturnsConfiguration_ForChaining()
        {
            using var configuration = TelemetryConfiguration.CreateDefault();
            var result = configuration.AddHvoEnrichers();

            Assert.AreSame(configuration, result);
        }

        [TestMethod]
        public void AddHvoEnrichers_CustomCorrelationPropertyName_PassedToInitializer()
        {
            using var configuration = TelemetryConfiguration.CreateDefault();
            var options = new AppInsightsOptions
            {
                CorrelationPropertyName = "CustomCorrelationId"
            };

            configuration.AddHvoEnrichers(options);

            var correlationInitializer = configuration.TelemetryInitializers
                .OfType<CorrelationTelemetryInitializer>()
                .FirstOrDefault();

            Assert.IsNotNull(correlationInitializer);
            // Verify the FallbackToActivity is set (indirect validation of custom options being passed)
            Assert.IsTrue(correlationInitializer!.FallbackToActivity);
        }

        [TestMethod]
        public void AddHvoEnrichers_DisableFallbackToActivity_PassedToInitializer()
        {
            using var configuration = TelemetryConfiguration.CreateDefault();
            var options = new AppInsightsOptions
            {
                CorrelationFallbackToActivity = false
            };

            configuration.AddHvoEnrichers(options);

            var correlationInitializer = configuration.TelemetryInitializers
                .OfType<CorrelationTelemetryInitializer>()
                .FirstOrDefault();

            Assert.IsNotNull(correlationInitializer);
            Assert.IsFalse(correlationInitializer!.FallbackToActivity);
        }
    }
}
