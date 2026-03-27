using System;
using System.Diagnostics;
using System.Linq;
using Serilog.Events;

namespace HVO.Enterprise.Telemetry.Serilog.Tests
{
    [TestClass]
    public class ActivityEnricherTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            // Ensure no lingering Activity
            Activity.Current?.Dispose();
            Activity.Current = null;
        }

        // ===== W3C Activity Tests =====

        [TestMethod]
        public void Enrich_WithW3CActivity_AddsTraceId()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var enricher = new ActivityEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            var traceId = SerilogTestHelpers.GetScalarValue(logEvent, "TraceId");
            Assert.IsNotNull(traceId);
            Assert.AreEqual(activity.TraceId.ToString(), traceId);
        }

        [TestMethod]
        public void Enrich_WithW3CActivity_AddsSpanId()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var enricher = new ActivityEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            var spanId = SerilogTestHelpers.GetScalarValue(logEvent, "SpanId");
            Assert.IsNotNull(spanId);
            Assert.AreEqual(activity.SpanId.ToString(), spanId);
        }

        [TestMethod]
        public void Enrich_WithW3CChildActivity_AddsParentId()
        {
            // Arrange
            using var parent = new Activity("parent")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            using var child = new Activity("child")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var enricher = new ActivityEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            var parentId = SerilogTestHelpers.GetScalarValue(logEvent, "ParentId");
            Assert.IsNotNull(parentId);
            Assert.AreEqual(parent.SpanId.ToString(), parentId);
        }

        [TestMethod]
        public void Enrich_WithW3CRootActivity_OmitsParentId()
        {
            // Arrange — root activity has no parent
            using var activity = new Activity("root")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var enricher = new ActivityEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert — ParentId should NOT be present for root Activity (all zeroes)
            Assert.IsTrue(logEvent.Properties.ContainsKey("TraceId"));
            Assert.IsTrue(logEvent.Properties.ContainsKey("SpanId"));
            Assert.IsFalse(logEvent.Properties.ContainsKey("ParentId"),
                "Root activity should not have ParentId property");
        }

        // ===== No Activity Tests =====

        [TestMethod]
        public void Enrich_WithNoActivity_AddsNoProperties()
        {
            // Arrange
            Activity.Current = null;
            var enricher = new ActivityEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            Assert.AreEqual(0, logEvent.Properties.Count,
                "No properties should be added when Activity.Current is null");
        }

        [TestMethod]
        public void Enrich_WithNoActivity_DoesNotThrow()
        {
            // Arrange
            Activity.Current = null;
            var enricher = new ActivityEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act & Assert — should not throw
            enricher.Enrich(logEvent, factory);
        }

        // ===== Hierarchical Activity Tests =====

        [TestMethod]
        public void Enrich_WithHierarchicalActivity_AddsRootIdAsTraceId()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.Hierarchical)
                .Start();

            var enricher = new ActivityEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            Assert.IsTrue(logEvent.Properties.ContainsKey("TraceId"));
            var traceId = SerilogTestHelpers.GetScalarValue(logEvent, "TraceId");
            Assert.AreEqual(activity.RootId ?? activity.Id, traceId);
        }

        [TestMethod]
        public void Enrich_WithHierarchicalActivity_AddsIdAsSpanId()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.Hierarchical)
                .Start();

            var enricher = new ActivityEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            Assert.IsTrue(logEvent.Properties.ContainsKey("SpanId"));
            var spanId = SerilogTestHelpers.GetScalarValue(logEvent, "SpanId");
            Assert.AreEqual(activity.Id, spanId);
        }

        // ===== Custom Property Names =====

        [TestMethod]
        public void Enrich_WithCustomPropertyNames_UsesCustomNames()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var enricher = new ActivityEnricher(
                traceIdPropertyName: "trace_id",
                spanIdPropertyName: "span_id",
                parentIdPropertyName: "parent_span_id");
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            Assert.IsTrue(logEvent.Properties.ContainsKey("trace_id"));
            Assert.IsTrue(logEvent.Properties.ContainsKey("span_id"));
            Assert.IsFalse(logEvent.Properties.ContainsKey("TraceId"),
                "Default property name should not be used when custom name is specified");
            Assert.IsFalse(logEvent.Properties.ContainsKey("SpanId"),
                "Default property name should not be used when custom name is specified");
        }

        // ===== AddPropertyIfAbsent Behavior =====

        [TestMethod]
        public void Enrich_DoesNotOverwriteExistingProperties()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var enricher = new ActivityEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Pre-set a TraceId property
            logEvent.AddPropertyIfAbsent(new LogEventProperty("TraceId", new ScalarValue("user-provided")));

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert — user-provided value should NOT be overwritten
            var traceId = SerilogTestHelpers.GetScalarValue(logEvent, "TraceId");
            Assert.AreEqual("user-provided", traceId);
        }

        // ===== Constructor Validation =====

        [TestMethod]
        public void Constructor_NullTraceIdPropertyName_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ActivityEnricher(traceIdPropertyName: null!));
        }

        [TestMethod]
        public void Constructor_EmptySpanIdPropertyName_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ActivityEnricher(spanIdPropertyName: ""));
        }

        [TestMethod]
        public void Constructor_EmptyParentIdPropertyName_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ActivityEnricher(parentIdPropertyName: ""));
        }

        // ===== Null Parameter Validation =====

        [TestMethod]
        public void Enrich_NullLogEvent_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var enricher = new ActivityEnricher();
                enricher.Enrich(null!, SerilogTestHelpers.CreatePropertyFactory());
            });
        }

        [TestMethod]
        public void Enrich_NullPropertyFactory_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var enricher = new ActivityEnricher();
                enricher.Enrich(SerilogTestHelpers.CreateLogEvent(), null!);
            });
        }

        // ===== Performance Test =====

        [TestMethod]
        public void Enrich_Performance_CompletesQuickly()
        {
            // Arrange
            using var activity = new Activity("perf-test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            var enricher = new ActivityEnricher();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act — 10K enrichments
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10_000; i++)
            {
                var logEvent = SerilogTestHelpers.CreateLogEvent();
                enricher.Enrich(logEvent, factory);
            }
            stopwatch.Stop();

            // Assert — should complete in <500ms (avg <50μs per enrichment; relaxed for CI runners)
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500,
                $"Enrichment too slow: {stopwatch.ElapsedMilliseconds}ms for 10K operations");
        }
    }
}
