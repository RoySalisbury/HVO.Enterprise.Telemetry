using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Correlation;
using Serilog.Events;

namespace HVO.Enterprise.Telemetry.Serilog.Tests
{
    [TestClass]
    public class CorrelationEnricherTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            // Clear correlation context and Activity
            CorrelationContext.Clear();
            Activity.Current?.Dispose();
            Activity.Current = null;
        }

        // ===== Explicit Correlation Context Tests =====

        [TestMethod]
        public void Enrich_WithExplicitCorrelation_AddsCorrelationId()
        {
            // Arrange
            using var scope = CorrelationContext.BeginScope("test-correlation-123");
            var enricher = new CorrelationEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            var correlationId = SerilogTestHelpers.GetScalarValue(logEvent, "CorrelationId");
            Assert.AreEqual("test-correlation-123", correlationId);
        }

        [TestMethod]
        public void Enrich_WithExplicitCorrelation_IgnoresFallbackSetting()
        {
            // Arrange — explicit correlation overrides fallback behavior
            using var scope = CorrelationContext.BeginScope("explicit-id");
            var enricher = new CorrelationEnricher(fallbackToActivity: false);
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert — explicit correlation should always be included
            var correlationId = SerilogTestHelpers.GetScalarValue(logEvent, "CorrelationId");
            Assert.AreEqual("explicit-id", correlationId);
        }

        // ===== Activity Fallback Tests =====

        [TestMethod]
        public void Enrich_WithNoCorrelationButActivity_FallsBackToTraceId()
        {
            // Arrange
            CorrelationContext.Clear();
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var enricher = new CorrelationEnricher(fallbackToActivity: true);
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert — should use Activity TraceId via CorrelationContext.Current fallback
            var correlationId = SerilogTestHelpers.GetScalarValue(logEvent, "CorrelationId");
            Assert.IsNotNull(correlationId);
            Assert.AreEqual(activity.TraceId.ToString(), correlationId);
        }

        [TestMethod]
        public void Enrich_WithFallbackDisabledAndNoActivity_AddsNothing()
        {
            // Arrange
            CorrelationContext.Clear();
            Activity.Current = null;
            var enricher = new CorrelationEnricher(fallbackToActivity: false);
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            Assert.IsFalse(logEvent.Properties.ContainsKey("CorrelationId"),
                "Should not add CorrelationId when fallback disabled and no Activity");
        }

        [TestMethod]
        public void Enrich_WithFallbackDisabledButActivity_DoesNotAddCorrelation()
        {
            // Arrange — fallback disabled: even with Activity present,
            // only explicit AsyncLocal values should be used
            CorrelationContext.Clear();
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var enricher = new CorrelationEnricher(fallbackToActivity: false);
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert — fallbackToActivity:false means only explicit correlation IDs are used,
            // Activity.TraceId is not used even though Activity is present
            var correlationId = SerilogTestHelpers.GetScalarValue(logEvent, "CorrelationId");
            Assert.IsNull(correlationId,
                "Should not add CorrelationId when fallback disabled, even with Activity present");
        }

        [TestMethod]
        public void Enrich_WithFallbackEnabled_AlwaysAddsCorrelation()
        {
            // Arrange — fallback enabled, even without explicit correlation or Activity,
            // CorrelationContext.Current auto-generates a GUID
            CorrelationContext.Clear();
            Activity.Current = null;
            var enricher = new CorrelationEnricher(fallbackToActivity: true);
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert — CorrelationContext.Current always returns a value (auto-generates)
            var correlationId = SerilogTestHelpers.GetScalarValue(logEvent, "CorrelationId");
            Assert.IsNotNull(correlationId, "CorrelationId should always be present when fallback is enabled");
            Assert.AreEqual(32, correlationId.Length, "Auto-generated GUID should be 32 hex chars (format N)");
        }

        // ===== Custom Property Name Tests =====

        [TestMethod]
        public void Enrich_WithCustomPropertyName_UsesCustomName()
        {
            // Arrange
            using var scope = CorrelationContext.BeginScope("test-123");
            var enricher = new CorrelationEnricher(propertyName: "request_id");
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            Assert.IsTrue(logEvent.Properties.ContainsKey("request_id"));
            Assert.IsFalse(logEvent.Properties.ContainsKey("CorrelationId"));
            Assert.AreEqual("test-123", SerilogTestHelpers.GetScalarValue(logEvent, "request_id"));
        }

        // ===== AddPropertyIfAbsent Behavior =====

        [TestMethod]
        public void Enrich_DoesNotOverwriteExistingProperty()
        {
            // Arrange
            using var scope = CorrelationContext.BeginScope("from-context");
            var enricher = new CorrelationEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Pre-set a CorrelationId
            logEvent.AddPropertyIfAbsent(new LogEventProperty("CorrelationId", new ScalarValue("user-provided")));

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            var correlationId = SerilogTestHelpers.GetScalarValue(logEvent, "CorrelationId");
            Assert.AreEqual("user-provided", correlationId);
        }

        // ===== Properties =====

        [TestMethod]
        public void FallbackToActivity_DefaultIsTrue()
        {
            var enricher = new CorrelationEnricher();
            Assert.IsTrue(enricher.FallbackToActivity);
        }

        [TestMethod]
        public void FallbackToActivity_CanBeSetToFalse()
        {
            var enricher = new CorrelationEnricher(fallbackToActivity: false);
            Assert.IsFalse(enricher.FallbackToActivity);
        }

        // ===== Scope Nesting Tests =====

        [TestMethod]
        public void Enrich_WithNestedScopes_UsesInnermostCorrelation()
        {
            // Arrange
            using var outer = CorrelationContext.BeginScope("outer-id");
            using var inner = CorrelationContext.BeginScope("inner-id");

            var enricher = new CorrelationEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            var correlationId = SerilogTestHelpers.GetScalarValue(logEvent, "CorrelationId");
            Assert.AreEqual("inner-id", correlationId);
        }

        [TestMethod]
        public void Enrich_AfterScopeDispose_RestoresPreviousCorrelation()
        {
            // Arrange
            using var outer = CorrelationContext.BeginScope("outer-id");

            using (CorrelationContext.BeginScope("inner-id"))
            {
                // inner scope active — just verify it's working
            }
            // inner scope disposed — back to outer

            var enricher = new CorrelationEnricher();
            var logEvent = SerilogTestHelpers.CreateLogEvent();
            var factory = SerilogTestHelpers.CreatePropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            var correlationId = SerilogTestHelpers.GetScalarValue(logEvent, "CorrelationId");
            Assert.AreEqual("outer-id", correlationId);
        }

        // ===== Constructor Validation =====

        [TestMethod]
        public void Constructor_NullPropertyName_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new CorrelationEnricher(propertyName: null!));
        }

        [TestMethod]
        public void Constructor_EmptyPropertyName_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new CorrelationEnricher(propertyName: ""));
        }

        // ===== Null Parameter Validation =====

        [TestMethod]
        public void Enrich_NullLogEvent_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var enricher = new CorrelationEnricher();
                enricher.Enrich(null!, SerilogTestHelpers.CreatePropertyFactory());
            });
        }

        [TestMethod]
        public void Enrich_NullPropertyFactory_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var enricher = new CorrelationEnricher();
                enricher.Enrich(SerilogTestHelpers.CreateLogEvent(), null!);
            });
        }

        // ===== Performance Test =====

        [TestMethod]
        public void Enrich_Performance_CompletesQuickly()
        {
            // Arrange
            using var scope = CorrelationContext.BeginScope("perf-test-id");
            var enricher = new CorrelationEnricher();
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
