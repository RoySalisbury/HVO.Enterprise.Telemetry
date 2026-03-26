using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Logging
{
    [TestClass]
    public sealed class TelemetryEnrichedLoggerTests
    {
        private CapturingLogger _innerLogger = null!;
        private TelemetryLoggerOptions _options = null!;

        [TestInitialize]
        public void Setup()
        {
            _innerLogger = new CapturingLogger();
            _options = new TelemetryLoggerOptions();
            // Clean up any ambient state
            Activity.Current = null;
            CorrelationContext.Clear();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Activity.Current = null;
            CorrelationContext.Clear();
        }

        // --- Constructor validation ---

        [TestMethod]
        public void Constructor_NullInnerLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new TelemetryEnrichedLogger(null!, new TelemetryLoggerOptions()));
        }

        [TestMethod]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new TelemetryEnrichedLogger(_innerLogger, null!));
        }

        // --- IsEnabled delegation ---

        [TestMethod]
        public void IsEnabled_DelegatesToInnerLogger()
        {
            // Arrange
            _innerLogger.IsEnabledFunc = level => level >= LogLevel.Warning;
            var logger = CreateEnrichedLogger();

            // Act & Assert
            Assert.IsFalse(logger.IsEnabled(LogLevel.Debug));
            Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
            Assert.IsTrue(logger.IsEnabled(LogLevel.Warning));
            Assert.IsTrue(logger.IsEnabled(LogLevel.Error));
        }

        // --- BeginScope delegation ---

        [TestMethod]
        public void BeginScope_DelegatesToInnerLogger()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            var scopeState = new Dictionary<string, string> { ["key"] = "value" };

            // Act
            using (var scope = logger.BeginScope(scopeState))
            {
                // Assert
                Assert.IsNotNull(scope);
            }

            Assert.AreEqual(1, _innerLogger.Scopes.Count);
            Assert.AreSame(scopeState, _innerLogger.Scopes[0]);
        }

        // --- Log with enrichment disabled ---

        [TestMethod]
        public void Log_EnrichmentDisabled_DelegatesDirectly()
        {
            // Arrange
            _options.EnableEnrichment = false;
            var logger = CreateEnrichedLogger();

            // Act
            logger.LogInformation("Test message");

            // Assert — should have logged but no scope created
            Assert.AreEqual(1, _innerLogger.LogEntries.Count);
            Assert.AreEqual(0, _innerLogger.Scopes.Count);
        }

        // --- Log when log level not enabled ---

        [TestMethod]
        public void Log_LevelNotEnabled_DoesNotLog()
        {
            // Arrange
            _innerLogger.IsEnabledFunc = level => level >= LogLevel.Error;
            var logger = CreateEnrichedLogger();

            // Act
            logger.LogInformation("Should be skipped");

            // Assert
            Assert.AreEqual(0, _innerLogger.LogEntries.Count);
            Assert.AreEqual(0, _innerLogger.Scopes.Count);
        }

        // --- Enrichment with Activity context ---

        [TestMethod]
        public void Log_WithActivity_EnrichesTraceIdAndSpanId()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity, "Activity should be created");

            // Act
            logger.LogInformation("Activity enriched");

            // Assert
            Assert.AreEqual(1, _innerLogger.LogEntries.Count);
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope, "Enrichment scope should be created");
            Assert.IsTrue(scope.ContainsKey("TraceId"), "Should contain TraceId");
            Assert.IsTrue(scope.ContainsKey("SpanId"), "Should contain SpanId");
            Assert.AreEqual(activity.TraceId.ToString(), scope["TraceId"]);
            Assert.AreEqual(activity.SpanId.ToString(), scope["SpanId"]);
        }

        [TestMethod]
        public void Log_WithActivity_IncludesParentSpanId_WhenParentExists()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var parentActivity = source.StartActivity("Parent");
            Assert.IsNotNull(parentActivity);

            using var childActivity = source.StartActivity("Child");
            Assert.IsNotNull(childActivity);

            // Act
            logger.LogInformation("Child operation");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.IsTrue(scope.ContainsKey("ParentSpanId"), "Should contain ParentSpanId");
            Assert.AreEqual(parentActivity.SpanId.ToString(), scope["ParentSpanId"]);
        }

        [TestMethod]
        public void Log_WithActivity_TraceFlagsIncluded_WhenEnabled()
        {
            // Arrange
            _options.IncludeTraceFlags = true;
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);

            // Act
            logger.LogInformation("With trace flags");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.IsTrue(scope.ContainsKey("TraceFlags"));
        }

        [TestMethod]
        public void Log_WithActivity_TraceFlagsExcluded_WhenDisabled()
        {
            // Arrange
            _options.IncludeTraceFlags = false; // default
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);

            // Act
            logger.LogInformation("Without trace flags");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.IsFalse(scope.ContainsKey("TraceFlags"));
        }

        [TestMethod]
        public void Log_WithActivity_TraceStateIncluded_WhenSetAndEnabled()
        {
            // Arrange
            _options.IncludeTraceState = true;
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);
            activity.TraceStateString = "vendorkey=vendorvalue";

            // Act
            logger.LogInformation("With trace state");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.IsTrue(scope.ContainsKey("TraceState"));
            Assert.AreEqual("vendorkey=vendorvalue", scope["TraceState"]);
        }

        // --- Enrichment with CorrelationContext ---

        [TestMethod]
        public void Log_WithCorrelationContext_EnrichesCorrelationId()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            CorrelationContext.SetRawValue("test-correlation-123");

            // Act
            logger.LogInformation("Correlated message");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope, "Enrichment scope should be created");
            Assert.IsTrue(scope.ContainsKey("CorrelationId"), "Should contain CorrelationId");
            Assert.AreEqual("test-correlation-123", scope["CorrelationId"]);
        }

        [TestMethod]
        public void Log_WithoutCorrelationContext_OmitsCorrelationId()
        {
            // Arrange — no CorrelationContext set, no Activity
            var logger = CreateEnrichedLogger();

            // Act
            logger.LogInformation("No context");

            // Assert — no scope created (empty enrichment data)
            Assert.AreEqual(1, _innerLogger.LogEntries.Count);
            // When no enrichment data is present, no scope should be created
            Assert.AreEqual(0, _innerLogger.Scopes.Count);
        }

        [TestMethod]
        public void Log_CorrelationIdDisabled_OmitsCorrelationId()
        {
            // Arrange
            _options.IncludeCorrelationId = false;
            var logger = CreateEnrichedLogger();
            CorrelationContext.SetRawValue("should-be-omitted");

            // Act
            logger.LogInformation("No correlation enrichment");

            // Assert — no scope (nothing else enabled without Activity)
            Assert.AreEqual(1, _innerLogger.LogEntries.Count);
            Assert.AreEqual(0, _innerLogger.Scopes.Count);
        }

        // --- Custom field names ---

        [TestMethod]
        public void Log_CustomFieldNames_UsesConfiguredNames()
        {
            // Arrange
            _options.TraceIdFieldName = "dd.trace_id";
            _options.SpanIdFieldName = "dd.span_id";
            _options.CorrelationIdFieldName = "x-correlation-id";
            var logger = CreateEnrichedLogger();

            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);
            CorrelationContext.SetRawValue("my-corr-id");

            // Act
            logger.LogInformation("Custom field names");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.IsTrue(scope.ContainsKey("dd.trace_id"), "Should use custom TraceId name");
            Assert.IsTrue(scope.ContainsKey("dd.span_id"), "Should use custom SpanId name");
            Assert.IsTrue(scope.ContainsKey("x-correlation-id"), "Should use custom CorrelationId name");
            Assert.IsFalse(scope.ContainsKey("TraceId"), "Should not use default TraceId name");
        }

        // --- Custom enrichers ---

        [TestMethod]
        public void Log_WithCustomEnrichers_AppliesEnrichment()
        {
            // Arrange
            _options.CustomEnrichers = new List<ILogEnricher>
            {
                new FakeEnricher(props =>
                {
                    props["CustomKey1"] = "value1";
                    props["CustomKey2"] = 42;
                })
            };
            var logger = CreateEnrichedLogger();

            // Act
            logger.LogInformation("Custom enrichment");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope, "Scope should be created from custom enricher");
            Assert.AreEqual("value1", scope["CustomKey1"]);
            Assert.AreEqual(42, scope["CustomKey2"]);
        }

        [TestMethod]
        public void Log_CustomEnricherThrows_SuppressesExceptionAndContinues()
        {
            // Arrange
            var enricherAfterThrow = new FakeEnricher(props =>
                props["AfterThrow"] = "survived");

            _options.CustomEnrichers = new List<ILogEnricher>
            {
                new ThrowingEnricher(),
                enricherAfterThrow
            };
            var logger = CreateEnrichedLogger();

            // Act — should not throw
            logger.LogInformation("Enricher throws");

            // Assert — the second enricher should still run
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.IsTrue(scope.ContainsKey("AfterThrow"), "Enricher after throwing one should still run");
            Assert.AreEqual("survived", scope["AfterThrow"]);
        }

        [TestMethod]
        public void Log_MultipleCustomEnrichers_AllApplied()
        {
            // Arrange
            _options.CustomEnrichers = new List<ILogEnricher>
            {
                new FakeEnricher(props => props["Key1"] = "A"),
                new FakeEnricher(props => props["Key2"] = "B"),
                new FakeEnricher(props => props["Key3"] = "C")
            };
            var logger = CreateEnrichedLogger();

            // Act
            logger.LogInformation("Three enrichers");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.AreEqual("A", scope["Key1"]);
            Assert.AreEqual("B", scope["Key2"]);
            Assert.AreEqual("C", scope["Key3"]);
        }

        // --- No context available (zero allocation path) ---

        [TestMethod]
        public void Log_NoActivityNoCorrelationNoEnrichers_NoScopeCreated()
        {
            // Arrange — no Activity, no CorrelationContext, no enrichers
            var logger = CreateEnrichedLogger();

            // Act
            logger.LogInformation("No enrichment context");

            // Assert — message logged but no scope
            Assert.AreEqual(1, _innerLogger.LogEntries.Count);
            Assert.AreEqual(0, _innerLogger.Scopes.Count);
        }

        // --- All fields disabled ---

        [TestMethod]
        public void Log_AllFieldsDisabled_NoScopeCreated()
        {
            // Arrange
            _options.IncludeTraceId = false;
            _options.IncludeSpanId = false;
            _options.IncludeParentSpanId = false;
            _options.IncludeTraceFlags = false;
            _options.IncludeTraceState = false;
            _options.IncludeCorrelationId = false;
            var logger = CreateEnrichedLogger();

            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            CorrelationContext.SetRawValue("ignored");

            // Act
            logger.LogInformation("All disabled");

            // Assert — no scope even with context present
            Assert.AreEqual(1, _innerLogger.LogEntries.Count);
            Assert.AreEqual(0, _innerLogger.Scopes.Count);
        }

        // --- Combined Activity + CorrelationId + custom enrichers ---

        [TestMethod]
        public void Log_CombinedEnrichment_AllSourcesPresent()
        {
            // Arrange
            _options.IncludeTraceFlags = true;
            _options.CustomEnrichers = new List<ILogEnricher>
            {
                new FakeEnricher(props => props["AppName"] = "TestApp")
            };
            var logger = CreateEnrichedLogger();

            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);
            CorrelationContext.SetRawValue("combined-test-id");

            // Act
            logger.LogInformation("Combined enrichment");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.IsTrue(scope.ContainsKey("TraceId"));
            Assert.IsTrue(scope.ContainsKey("SpanId"));
            Assert.IsTrue(scope.ContainsKey("TraceFlags"));
            Assert.IsTrue(scope.ContainsKey("CorrelationId"));
            Assert.AreEqual("combined-test-id", scope["CorrelationId"]);
            Assert.AreEqual("TestApp", scope["AppName"]);
        }

        // --- Log entry passes through correctly ---

        [TestMethod]
        public void Log_MessageAndExceptionPassedThrough()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            var exception = new InvalidOperationException("Boom");
            CorrelationContext.SetRawValue("log-entry-test");

            // Act
            logger.Log(LogLevel.Error, new EventId(42, "TestEvent"), "Error message", exception,
                (state, ex) => $"{state} - {ex?.Message}");

            // Assert
            Assert.AreEqual(1, _innerLogger.LogEntries.Count);
            var entry = _innerLogger.LogEntries[0];
            Assert.AreEqual(LogLevel.Error, entry.Level);
            Assert.AreEqual(42, entry.EventId.Id);
            Assert.AreEqual("Error message - Boom", entry.Message);
            Assert.AreSame(exception, entry.Exception);
        }

        // --- Only TraceId selected ---

        [TestMethod]
        public void Log_OnlyTraceIdEnabled_EnrichesOnlyTraceId()
        {
            // Arrange
            _options.IncludeSpanId = false;
            _options.IncludeParentSpanId = false;
            _options.IncludeCorrelationId = false;
            var logger = CreateEnrichedLogger();

            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);

            // Act
            logger.LogInformation("Only TraceId");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.AreEqual(1, scope.Count);
            Assert.IsTrue(scope.ContainsKey("TraceId"));
        }

        // --- Enrichment Value Type Validation ---
        // These tests ensure enrichment values are stored as proper string data,
        // not as boxed struct types (ActivityTraceId, ActivitySpanId, etc.) that
        // would render as type names in some logging providers.

        [TestMethod]
        public void Log_TraceId_IsString_NotActivityTraceIdStruct()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);

            // Act
            logger.LogInformation("TraceId type check");

            // Assert — value must be a string, not a boxed ActivityTraceId
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            var traceIdValue = scope["TraceId"];
            Assert.IsInstanceOfType(traceIdValue, typeof(string),
                "TraceId should be a string, not a boxed ActivityTraceId struct");
        }

        [TestMethod]
        public void Log_SpanId_IsString_NotActivitySpanIdStruct()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);

            // Act
            logger.LogInformation("SpanId type check");

            // Assert — value must be a string, not a boxed ActivitySpanId
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            var spanIdValue = scope["SpanId"];
            Assert.IsInstanceOfType(spanIdValue, typeof(string),
                "SpanId should be a string, not a boxed ActivitySpanId struct");
        }

        [TestMethod]
        public void Log_ParentSpanId_IsString_NotActivitySpanIdStruct()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var parentActivity = source.StartActivity("Parent");
            Assert.IsNotNull(parentActivity);
            using var childActivity = source.StartActivity("Child");
            Assert.IsNotNull(childActivity);

            // Act
            logger.LogInformation("ParentSpanId type check");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            var parentSpanIdValue = scope["ParentSpanId"];
            Assert.IsInstanceOfType(parentSpanIdValue, typeof(string),
                "ParentSpanId should be a string, not a boxed ActivitySpanId struct");
        }

        [TestMethod]
        public void Log_TraceFlags_IsString_NotEnumValue()
        {
            // Arrange
            _options.IncludeTraceFlags = true;
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);

            // Act
            logger.LogInformation("TraceFlags type check");

            // Assert — should be a string like "Recorded" or "None", not a boxed enum
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            var traceFlagsValue = scope["TraceFlags"];
            Assert.IsInstanceOfType(traceFlagsValue, typeof(string),
                "TraceFlags should be a string, not a boxed ActivityTraceFlags enum");
        }

        [TestMethod]
        public void Log_TraceId_MatchesHexFormat()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);

            // Act
            logger.LogInformation("TraceId format check");

            // Assert — W3C TraceId is 32 lowercase hex characters
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            var traceId = (string)scope["TraceId"]!;
            Assert.IsTrue(Regex.IsMatch(traceId, @"^[0-9a-f]{32}$"),
                $"TraceId should be 32 hex chars, got: '{traceId}'");
        }

        [TestMethod]
        public void Log_SpanId_MatchesHexFormat()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = source.StartActivity("TestOp");
            Assert.IsNotNull(activity);

            // Act
            logger.LogInformation("SpanId format check");

            // Assert — W3C SpanId is 16 lowercase hex characters
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            var spanId = (string)scope["SpanId"]!;
            Assert.IsTrue(Regex.IsMatch(spanId, @"^[0-9a-f]{16}$"),
                $"SpanId should be 16 hex chars, got: '{spanId}'");
        }

        [TestMethod]
        public void Log_AllEnrichedValues_AreStrings_NeverContainTypeNames()
        {
            // Arrange — enable all fields for maximum coverage
            _options.IncludeTraceFlags = true;
            _options.IncludeTraceState = true;
            var logger = CreateEnrichedLogger();

            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var parentActivity = source.StartActivity("Parent");
            Assert.IsNotNull(parentActivity);
            using var childActivity = source.StartActivity("Child");
            Assert.IsNotNull(childActivity);
            childActivity.TraceStateString = "vendor=value";
            CorrelationContext.SetRawValue("corr-12345");

            // Act
            logger.LogInformation("Full enrichment type check");

            // Assert — every value must be a string
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);

            foreach (var kvp in scope)
            {
                Assert.IsNotNull(kvp.Value, $"Value for '{kvp.Key}' should not be null");
                Assert.IsInstanceOfType(kvp.Value, typeof(string),
                    $"Value for '{kvp.Key}' should be string, but was {kvp.Value?.GetType().FullName}");

                var strValue = (string)kvp.Value;
                Assert.IsFalse(strValue.StartsWith("System."),
                    $"Value for '{kvp.Key}' looks like a type name: '{strValue}'");
                Assert.IsFalse(strValue.Contains("ActivityTraceId"),
                    $"Value for '{kvp.Key}' contains struct type name: '{strValue}'");
                Assert.IsFalse(strValue.Contains("ActivitySpanId"),
                    $"Value for '{kvp.Key}' contains struct type name: '{strValue}'");
            }
        }

        [TestMethod]
        public void Log_EnrichmentScope_ToString_NeverContainsTypeNames()
        {
            // Arrange — enable all fields
            _options.IncludeTraceFlags = true;
            var logger = CreateEnrichedLogger();

            using var source = new ActivitySource("test");
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var parentActivity = source.StartActivity("Parent");
            Assert.IsNotNull(parentActivity);
            using var childActivity = source.StartActivity("Child");
            Assert.IsNotNull(childActivity);
            CorrelationContext.SetRawValue("corr-abc");

            // Act
            logger.LogInformation("Scope ToString check");

            // Assert — the scope's ToString should contain data, not type names
            var rawScope = _innerLogger.Scopes.LastOrDefault();
            Assert.IsNotNull(rawScope);
            var toStringResult = rawScope!.ToString()!;

            Assert.IsFalse(toStringResult.Contains("System.Diagnostics"),
                $"ToString should not contain System.Diagnostics: '{toStringResult}'");
            Assert.IsFalse(toStringResult.Contains("ActivityTraceId"),
                $"ToString should not contain ActivityTraceId: '{toStringResult}'");
            Assert.IsFalse(toStringResult.Contains("ActivitySpanId"),
                $"ToString should not contain ActivitySpanId: '{toStringResult}'");
            Assert.IsFalse(toStringResult.Contains("ActivityTraceFlags"),
                $"ToString should not contain ActivityTraceFlags: '{toStringResult}'");
            Assert.IsFalse(toStringResult.Contains("Dictionary"),
                $"ToString should not contain Dictionary type name: '{toStringResult}'");

            // Positive checks: should contain actual data
            Assert.IsTrue(toStringResult.Contains("TraceId:"),
                $"ToString should contain TraceId key: '{toStringResult}'");
            Assert.IsTrue(toStringResult.Contains("SpanId:"),
                $"ToString should contain SpanId key: '{toStringResult}'");
        }

        [TestMethod]
        public void Log_CustomEnricherWithNonStringValue_PreservesTypeInScope()
        {
            // Arrange — custom enricher that stores an int (not a string).
            // This is allowed but the test documents the behavior:
            // ToString() will render the int fine, but structured providers
            // will see an int, not a string. Enrichers SHOULD store strings
            // for telemetry fields to avoid provider-specific formatting issues.
            _options.CustomEnrichers = new List<ILogEnricher>
            {
                new FakeEnricher(props =>
                {
                    props["HttpStatus"] = 200;
                    props["IsHealthy"] = true;
                    props["UserId"] = "user-42"; // Best practice: store as string
                })
            };
            var logger = CreateEnrichedLogger();

            // Act
            logger.LogInformation("Custom enricher types");

            // Assert — verify original types are preserved for structured providers
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.AreEqual(200, scope["HttpStatus"]);
            Assert.AreEqual(true, scope["IsHealthy"]);
            Assert.AreEqual("user-42", scope["UserId"]);

            // Verify ToString doesn't produce type names
            var rawScope = _innerLogger.Scopes.LastOrDefault();
            Assert.IsNotNull(rawScope);
            var toStr = rawScope!.ToString()!;
            Assert.IsTrue(toStr.Contains("HttpStatus:200"), $"Expected HttpStatus:200 in '{toStr}'");
            Assert.IsTrue(toStr.Contains("IsHealthy:True"), $"Expected IsHealthy:True in '{toStr}'");
            Assert.IsTrue(toStr.Contains("UserId:user-42"), $"Expected UserId:user-42 in '{toStr}'");
            Assert.IsFalse(toStr.Contains("Int32"), $"Should not contain Int32 type name: '{toStr}'");
            Assert.IsFalse(toStr.Contains("Boolean"), $"Should not contain Boolean type name: '{toStr}'");
        }

        [TestMethod]
        public void Log_CorrelationId_IsString_NotGuidOrOtherType()
        {
            // Arrange
            var logger = CreateEnrichedLogger();
            CorrelationContext.SetRawValue("550e8400-e29b-41d4-a716-446655440000");

            // Act
            logger.LogInformation("CorrelationId type check");

            // Assert
            var scope = _innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            var value = scope["CorrelationId"];
            Assert.IsInstanceOfType(value, typeof(string),
                "CorrelationId should be a string, not a Guid or other type");
            Assert.AreEqual("550e8400-e29b-41d4-a716-446655440000", value);
        }

        private TelemetryEnrichedLogger CreateEnrichedLogger()
        {
            return new TelemetryEnrichedLogger(_innerLogger, _options);
        }
    }
}
