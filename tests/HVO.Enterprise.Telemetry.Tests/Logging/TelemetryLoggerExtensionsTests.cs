using System;
using HVO.Enterprise.Telemetry.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Logging
{
    [TestClass]
    public sealed class TelemetryLoggerExtensionsTests
    {
        [TestMethod]
        public void AddTelemetryLoggingEnrichment_NullServices_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                TelemetryLoggerExtensions.AddTelemetryLoggingEnrichment(null!));
        }

        [TestMethod]
        public void AddTelemetryLoggingEnrichment_RegistersTelemetryLoggerOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddTelemetryLoggingEnrichment();

            // Assert
            var provider = services.BuildServiceProvider();
            var options = provider.GetService<TelemetryLoggerOptions>();
            Assert.IsNotNull(options, "TelemetryLoggerOptions should be registered");
        }

        [TestMethod]
        public void AddTelemetryLoggingEnrichment_ConfigureCallbackApplied()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddTelemetryLoggingEnrichment(opts =>
            {
                opts.IncludeTraceFlags = true;
                opts.TraceIdFieldName = "custom_trace_id";
            });

            // Assert
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<TelemetryLoggerOptions>();
            Assert.IsTrue(options.IncludeTraceFlags);
            Assert.AreEqual("custom_trace_id", options.TraceIdFieldName);
        }

        [TestMethod]
        public void AddTelemetryLoggingEnrichment_IdempotentOnSecondCall()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddTelemetryLoggingEnrichment(opts => opts.TraceIdFieldName = "first");
            services.AddTelemetryLoggingEnrichment(opts => opts.TraceIdFieldName = "second");

            // Assert — first registration wins
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<TelemetryLoggerOptions>();
            Assert.AreEqual("first", options.TraceIdFieldName);
        }

        [TestMethod]
        public void AddTelemetryLoggingEnrichment_WithExistingFactory_WrapsFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            // Register a pre-existing ILoggerFactory (simulates what AddLogging() does)
            var innerFactory = new CapturingLoggerFactory();
            services.AddSingleton<ILoggerFactory>(innerFactory);

            // Act
            services.AddTelemetryLoggingEnrichment();

            // Assert — ILoggerFactory should still resolve (wrapped)
            var provider = services.BuildServiceProvider();
            var factory = provider.GetService<ILoggerFactory>();
            Assert.IsNotNull(factory, "ILoggerFactory should resolve");
            Assert.AreNotSame(innerFactory, factory, "Factory should be wrapped");

            var logger = factory.CreateLogger("Test");
            Assert.IsNotNull(logger);
        }

        [TestMethod]
        public void AddTelemetryLoggingEnrichment_WithoutExistingLogging_ThrowsInvalidOperationException()
        {
            // Arrange
            var services = new ServiceCollection();
            // No AddLogging() call — no existing ILoggerFactory

            // Act & Assert — should throw with guidance
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
                services.AddTelemetryLoggingEnrichment());

            Assert.IsTrue(ex.Message.Contains("AddLogging"),
                "Exception message should mention AddLogging()");
        }

        [TestMethod]
        public void AddTelemetryLoggingEnrichment_ReturnsSameServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            var result = services.AddTelemetryLoggingEnrichment();

            // Assert
            Assert.AreSame(services, result, "Should return same IServiceCollection for chaining");
        }

        [TestMethod]
        public void AddTelemetryLoggingEnrichment_DefaultOptions_WhenNoConfigureCallback()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddTelemetryLoggingEnrichment();

            // Assert
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<TelemetryLoggerOptions>();
            Assert.IsTrue(options.EnableEnrichment);
            Assert.IsTrue(options.IncludeTraceId);
            Assert.IsTrue(options.IncludeSpanId);
        }

        [TestMethod]
        public void AddTelemetryLoggingEnrichment_IdempotencyUsesMarkerNotOptions()
        {
            // Arrange — register TelemetryLoggerOptions independently (app config binding scenario)
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(new TelemetryLoggerOptions { IncludeTraceFlags = true });

            // Act — should still register enrichment despite options already present
            services.AddTelemetryLoggingEnrichment(opts => opts.TraceIdFieldName = "my_trace");

            // Assert — enrichment was applied
            var provider = services.BuildServiceProvider();
            var factory = provider.GetService<ILoggerFactory>();
            Assert.IsNotNull(factory);
        }
    }
}
