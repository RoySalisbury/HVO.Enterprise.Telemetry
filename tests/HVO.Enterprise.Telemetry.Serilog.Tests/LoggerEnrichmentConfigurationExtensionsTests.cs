using System;
using System.Diagnostics;
using global::Serilog;
using global::Serilog.Events;
using HVO.Enterprise.Telemetry.Correlation;

namespace HVO.Enterprise.Telemetry.Serilog.Tests
{
    [TestClass]
    public class LoggerEnrichmentConfigurationExtensionsTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            CorrelationContext.Clear();
            Activity.Current?.Dispose();
            Activity.Current = null;
        }

        // ===== WithActivity Extension Method Tests =====

        [TestMethod]
        public void WithActivity_ReturnsLoggerConfiguration()
        {
            // Act
            var config = new LoggerConfiguration()
                .Enrich.WithActivity();

            // Assert
            Assert.IsNotNull(config);
        }

        [TestMethod]
        public void WithActivity_CreatesWorkingLogger()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            LogEvent? capturedEvent = null;
            var logger = new LoggerConfiguration()
                .Enrich.WithActivity()
                .WriteTo.Sink(new DelegatingLogEventSink(e => capturedEvent = e))
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert
            Assert.IsNotNull(capturedEvent);
            Assert.IsTrue(capturedEvent.Properties.ContainsKey("TraceId"));
            Assert.IsTrue(capturedEvent.Properties.ContainsKey("SpanId"));
        }

        [TestMethod]
        public void WithActivity_CustomPropertyNames_Respected()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            LogEvent? capturedEvent = null;
            var logger = new LoggerConfiguration()
                .Enrich.WithActivity(
                    traceIdPropertyName: "trace_id",
                    spanIdPropertyName: "span_id",
                    parentIdPropertyName: "parent_span_id")
                .WriteTo.Sink(new DelegatingLogEventSink(e => capturedEvent = e))
                .CreateLogger();

            // Act
            logger.Information("Test");

            // Assert
            Assert.IsNotNull(capturedEvent);
            Assert.IsTrue(capturedEvent.Properties.ContainsKey("trace_id"));
            Assert.IsTrue(capturedEvent.Properties.ContainsKey("span_id"));
            Assert.IsFalse(capturedEvent.Properties.ContainsKey("TraceId"));
        }

        [TestMethod]
        public void WithActivity_NullConfiguration_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => LoggerEnrichmentConfigurationExtensions.WithActivity(null!));
        }

        // ===== WithCorrelation Extension Method Tests =====

        [TestMethod]
        public void WithCorrelation_ReturnsLoggerConfiguration()
        {
            var config = new LoggerConfiguration()
                .Enrich.WithCorrelation();
            Assert.IsNotNull(config);
        }

        [TestMethod]
        public void WithCorrelation_CreatesWorkingLogger()
        {
            // Arrange
            using var scope = CorrelationContext.BeginScope("test-corr-123");

            LogEvent? capturedEvent = null;
            var logger = new LoggerConfiguration()
                .Enrich.WithCorrelation()
                .WriteTo.Sink(new DelegatingLogEventSink(e => capturedEvent = e))
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert
            Assert.IsNotNull(capturedEvent);
            Assert.IsTrue(capturedEvent.Properties.ContainsKey("CorrelationId"));
            var value = ((ScalarValue)capturedEvent.Properties["CorrelationId"]).Value?.ToString();
            Assert.AreEqual("test-corr-123", value);
        }

        [TestMethod]
        public void WithCorrelation_CustomPropertyName_Respected()
        {
            // Arrange
            using var scope = CorrelationContext.BeginScope("abc");

            LogEvent? capturedEvent = null;
            var logger = new LoggerConfiguration()
                .Enrich.WithCorrelation(propertyName: "request_id")
                .WriteTo.Sink(new DelegatingLogEventSink(e => capturedEvent = e))
                .CreateLogger();

            // Act
            logger.Information("Test");

            // Assert
            Assert.IsNotNull(capturedEvent);
            Assert.IsTrue(capturedEvent.Properties.ContainsKey("request_id"));
            Assert.IsFalse(capturedEvent.Properties.ContainsKey("CorrelationId"));
        }

        [TestMethod]
        public void WithCorrelation_NullConfiguration_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => LoggerEnrichmentConfigurationExtensions.WithCorrelation(null!));
        }

        // ===== WithTelemetry Convenience Method Tests =====

        [TestMethod]
        public void WithTelemetry_ReturnsLoggerConfiguration()
        {
            var config = new LoggerConfiguration()
                .Enrich.WithTelemetry();
            Assert.IsNotNull(config);
        }

        [TestMethod]
        public void WithTelemetry_AddsBothEnrichers()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            using var scope = CorrelationContext.BeginScope("telemetry-test");

            LogEvent? capturedEvent = null;
            var logger = new LoggerConfiguration()
                .Enrich.WithTelemetry()
                .WriteTo.Sink(new DelegatingLogEventSink(e => capturedEvent = e))
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert — should have BOTH Activity and Correlation properties
            Assert.IsNotNull(capturedEvent);
            Assert.IsTrue(capturedEvent.Properties.ContainsKey("TraceId"),
                "WithTelemetry should add TraceId");
            Assert.IsTrue(capturedEvent.Properties.ContainsKey("SpanId"),
                "WithTelemetry should add SpanId");
            Assert.IsTrue(capturedEvent.Properties.ContainsKey("CorrelationId"),
                "WithTelemetry should add CorrelationId");

            var correlationId = ((ScalarValue)capturedEvent.Properties["CorrelationId"]).Value?.ToString();
            Assert.AreEqual("telemetry-test", correlationId);
        }

        [TestMethod]
        public void WithTelemetry_NullConfiguration_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => LoggerEnrichmentConfigurationExtensions.WithTelemetry(null!));
        }

        // ===== Integration Tests =====

        [TestMethod]
        public void Integration_FullPipeline_CorrelatesLogsAndTraces()
        {
            // Arrange
            var capturedEvents = new System.Collections.Generic.List<LogEvent>();

            var logger = new LoggerConfiguration()
                .Enrich.WithTelemetry()
                .WriteTo.Sink(new DelegatingLogEventSink(e => capturedEvents.Add(e)))
                .CreateLogger();

            // Act — create an Activity and correlation scope, then log
            using (new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start())
            {
                using (CorrelationContext.BeginScope("integration-test-123"))
                {
                    logger.Information("First log within scope");
                    logger.Warning("Second log within scope");
                }
            }

            // Log outside scope
            CorrelationContext.Clear();
            Activity.Current = null;
            logger.Information("Log outside scope");

            // Assert
            Assert.AreEqual(3, capturedEvents.Count);

            // First two logs should have all properties
            var first = capturedEvents[0];
            Assert.IsTrue(first.Properties.ContainsKey("TraceId"));
            Assert.IsTrue(first.Properties.ContainsKey("SpanId"));
            Assert.IsTrue(first.Properties.ContainsKey("CorrelationId"));
            Assert.AreEqual("integration-test-123",
                ((ScalarValue)first.Properties["CorrelationId"]).Value?.ToString());

            // Second log should have same TraceId (same Activity)
            var second = capturedEvents[1];
            Assert.AreEqual(
                ((ScalarValue)first.Properties["TraceId"]).Value?.ToString(),
                ((ScalarValue)second.Properties["TraceId"]).Value?.ToString(),
                "Logs within same Activity should share TraceId");

            // Third log outside scope — no Activity properties
            var third = capturedEvents[2];
            Assert.IsFalse(third.Properties.ContainsKey("TraceId"),
                "Log outside Activity should not have TraceId");
        }

        [TestMethod]
        public void Integration_MultipleLogLevels_AllEnriched()
        {
            // Arrange
            using var activity = new Activity("multi-level")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            using var scope = CorrelationContext.BeginScope("level-test");

            var capturedEvents = new System.Collections.Generic.List<LogEvent>();

            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithTelemetry()
                .WriteTo.Sink(new DelegatingLogEventSink(e => capturedEvents.Add(e)))
                .CreateLogger();

            // Act
            logger.Verbose("Verbose");
            logger.Debug("Debug");
            logger.Information("Info");
            logger.Warning("Warn");
            logger.Error("Error");
            logger.Fatal("Fatal");

            // Assert — all levels should be enriched
            Assert.AreEqual(6, capturedEvents.Count);
            foreach (var evt in capturedEvents)
            {
                Assert.IsTrue(evt.Properties.ContainsKey("TraceId"),
                    $"Log at level {evt.Level} should have TraceId");
                Assert.IsTrue(evt.Properties.ContainsKey("CorrelationId"),
                    $"Log at level {evt.Level} should have CorrelationId");
            }
        }
    }
}
