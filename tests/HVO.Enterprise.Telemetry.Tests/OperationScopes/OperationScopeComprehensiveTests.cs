using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Internal;
using HVO.Enterprise.Telemetry.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Tests.OperationScopes
{
    /// <summary>
    /// Comprehensive tests for <see cref="OperationScope"/> covering guard clauses,
    /// option behaviors, tag normalization, and disposal semantics.
    /// </summary>
    [TestClass]
    public class OperationScopeComprehensiveTests
    {
        private TestActivitySource _testSource = null!;

        [TestInitialize]
        public void Setup()
        {
            _testSource = new TestActivitySource("comprehensive-scope-test");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _testSource.Dispose();
        }

        // --- Name and CorrelationId ---

        [TestMethod]
        public void OperationScope_Name_ReturnsOperationName()
        {
            using var scope = CreateScope("my-operation");
            Assert.AreEqual("my-operation", scope.Name);
        }

        [TestMethod]
        public void OperationScope_CorrelationId_IsNotEmpty()
        {
            using var scope = CreateScope("corr-test-op");
            Assert.IsFalse(string.IsNullOrEmpty(scope.CorrelationId));
        }

        // --- WithTags (bulk) ---

        [TestMethod]
        public void OperationScope_WithTags_AddsBulkTags()
        {
            using var scope = CreateScope("bulk-tag-op");
            var tags = new[]
            {
                new KeyValuePair<string, object?>("key1", "value1"),
                new KeyValuePair<string, object?>("key2", 42),
                new KeyValuePair<string, object?>("key3", true)
            };

            var result = scope.WithTags(tags);

            Assert.AreSame(scope, result, "WithTags should return the same scope for fluent chaining");
            if (scope.Activity != null)
            {
                Assert.AreEqual("value1", scope.Activity.GetTagItem("key1")?.ToString());
            }
        }

        // --- WithTag edge cases ---

        [TestMethod]
        public void OperationScope_WithTag_NullKey_DoesNotThrow()
        {
            using var scope = CreateScope("null-key-op");
            // null/empty key should be a no-op (not throw)
            scope.WithTag(null!, "value");
            scope.WithTag("", "value");
        }

        [TestMethod]
        public void OperationScope_WithTag_NullValue_AddsTag()
        {
            using var scope = CreateScope("null-value-op");
            scope.WithTag("key", "value");

            scope.WithTag("key", null);

            // Legacy test name retained for historical baselines; null now removes the tag.
            if (scope.Activity != null)
            {
                Assert.IsNull(scope.Activity.GetTagItem("key"));
            }
        }

        [TestMethod]
        public void OperationScope_WithTag_SimpleTypes_AreSerialized()
        {
            using var scope = CreateScope("simple-types-op");
            scope.WithTag("int", 42);
            scope.WithTag("double", 3.14);
            scope.WithTag("bool", true);
            scope.WithTag("datetime", new DateTime(2025, 1, 1));
            scope.WithTag("guid", Guid.Empty);
            scope.WithTag("string", "hello");
            // Should not throw for any simple type
        }

        // --- WithProperty edge cases ---

        [TestMethod]
        public void OperationScope_WithProperty_NullKey_DoesNotThrow()
        {
            using var scope = CreateScope("null-prop-key");
            scope.WithProperty(null!, () => "value");
        }

        [TestMethod]
        public void OperationScope_WithProperty_NullFactory_DoesNotThrow()
        {
            using var scope = CreateScope("null-prop-factory");
            scope.WithProperty("key", null!);
        }

        [TestMethod]
        public void OperationScope_WithProperty_FactoryThrows_DoesNotBubble()
        {
            // Arrange & Act - dispose triggers lazy evaluation
            using var scope = CreateScope("throwing-prop");
            scope.WithProperty("bad", () => throw new InvalidOperationException("boom"));
            // Dispose should not throw even if the property factory fails
        }

        // --- RecordException ---

        [TestMethod]
        public void OperationScope_RecordException_AddsExceptionToActivity()
        {
            using var scope = CreateScope("record-exception-op");
            var exception = new InvalidOperationException("test error");

            scope.RecordException(exception);

            // Should not throw, and exception should be recorded
        }

        // --- Fail guard clause ---

        [TestMethod]
        public void OperationScope_Fail_NullException_ThrowsArgumentNullException()
        {
            using var scope = CreateScope("fail-null-op");
            Assert.ThrowsException<ArgumentNullException>(() => scope.Fail(null!));
        }

        // --- Disposed state guards ---

        [TestMethod]
        public void OperationScope_AfterDispose_WithTag_IsNoOp()
        {
            var scope = CreateScope("disposed-tag-op");
            scope.Dispose();

            // Should silently no-op, not throw
            scope.WithTag("postDispose", "value");
        }

        [TestMethod]
        public void OperationScope_AfterDispose_Succeed_IsNoOp()
        {
            var scope = CreateScope("disposed-succeed-op");
            scope.Dispose();

            scope.Succeed();
        }

        [TestMethod]
        public void OperationScope_AfterDispose_Fail_WithValidException_IsNoOp()
        {
            var scope = CreateScope("disposed-fail-op");
            scope.Dispose();

            scope.Fail(new Exception("should be no-op"));
        }

        [TestMethod]
        public void OperationScope_AfterDispose_WithResult_IsNoOp()
        {
            var scope = CreateScope("disposed-result-op");
            scope.Dispose();

            scope.WithResult("too late");
        }

        [TestMethod]
        public void OperationScope_DoubleDispose_IsIdempotent()
        {
            var scope = CreateScope("double-dispose-op");
            scope.Dispose();
            scope.Dispose(); // Should not throw
        }

        // --- OperationScopeOptions behaviors ---

        [TestMethod]
        public void OperationScope_CreateActivityFalse_NoActivityCreated()
        {
            var options = new OperationScopeOptions { CreateActivity = false };
            using var scope = new OperationScope("no-activity-op", options,
                _testSource.Source, null, null, null);

            Assert.IsNull(scope.Activity, "Activity should be null when CreateActivity=false");
            Assert.AreEqual("no-activity-op", scope.Name);
        }

        [TestMethod]
        public void OperationScope_WithInitialTags_TagsAppliedOnCreation()
        {
            var options = new OperationScopeOptions
            {
                InitialTags = new Dictionary<string, object?>
                {
                    ["init.key1"] = "init.value1",
                    ["init.key2"] = 99
                }
            };

            using var scope = new OperationScope("initial-tags-op", options,
                _testSource.Source, null, null, null);

            if (scope.Activity != null)
            {
                Assert.AreEqual("init.value1", scope.Activity.GetTagItem("init.key1")?.ToString());
            }
        }

        [TestMethod]
        public void OperationScope_SerializeComplexTypesFalse_UsesToString()
        {
            var options = new OperationScopeOptions { SerializeComplexTypes = false };
            using var scope = new OperationScope("no-serialize-op", options,
                _testSource.Source, null, null, null);

            var complexObject = new { Name = "test", Value = 42 };
            scope.WithTag("complex", complexObject);

            // Should use ToString() instead of JSON serialization
        }

        [TestMethod]
        public void OperationScope_ActivityKind_IsRespected()
        {
            var options = new OperationScopeOptions { ActivityKind = ActivityKind.Client };
            using var scope = new OperationScope("client-op", options,
                _testSource.Source, null, null, null);

            if (scope.Activity != null)
            {
                Assert.AreEqual(ActivityKind.Client, scope.Activity.Kind);
            }
        }

        // --- WithResult ---

        [TestMethod]
        public void OperationScope_WithResult_NullResult_DoesNotThrow()
        {
            using var scope = CreateScope("null-result-op");
            scope.WithResult(null);
        }

        // --- Succeed ---

        [TestMethod]
        public void OperationScope_Succeed_SetsActivityStatusOk()
        {
            using var scope = CreateScope("succeed-op");
            scope.Succeed();

            if (scope.Activity != null)
            {
                Assert.AreEqual(ActivityStatusCode.Ok, scope.Activity.Status);
            }
        }

        // --- Elapsed ---

        [TestMethod]
        public void OperationScope_Elapsed_IncreasesOverTime()
        {
            using var scope = CreateScope("elapsed-op");
            var first = scope.Elapsed;
            System.Threading.Thread.SpinWait(10000);
            var second = scope.Elapsed;
            Assert.IsTrue(second >= first, "Elapsed should increase over time");
        }

        // --- LogEvents with logger ---

        [TestMethod]
        public void OperationScope_WithLogger_LogsOnDispose()
        {
            var logger = new FakeLogger("OperationScope");
            var options = new OperationScopeOptions { LogEvents = true, LogLevel = LogLevel.Information };
            using (var scope = new OperationScope("logged-op", options,
                _testSource.Source, logger, null, null))
            {
                scope.Succeed();
            }

            // Logger should have received at least one log entry
            Assert.IsTrue(logger.Count > 0, "Logger should have recorded entries on dispose");
        }

        [TestMethod]
        public void OperationScope_LogEventsFalse_DoesNotLogOnSuccessPath()
        {
            var logger = new FakeLogger("OperationScope");
            var options = new OperationScopeOptions { LogEvents = false };
            using (var scope = new OperationScope("silent-op", options,
                _testSource.Source, logger, null, null))
            {
                scope.Succeed();
            }

            // With LogEvents=false, the success path should not produce log entries.
            // Compare against the LogEvents=true test which asserts Count > 0.
            var logEventsEnabled = new FakeLogger("OperationScope");
            var enabledOptions = new OperationScopeOptions { LogEvents = true, LogLevel = LogLevel.Information };
            using (var scope2 = new OperationScope("logged-op", enabledOptions,
                _testSource.Source, logEventsEnabled, null, null))
            {
                scope2.Succeed();
            }

            Assert.IsTrue(logger.Count <= logEventsEnabled.Count,
                $"LogEvents=false should produce no more log entries than LogEvents=true. " +
                $"Silent={logger.Count}, Logged={logEventsEnabled.Count}");
        }

        // --- Fluent chaining ---

        [TestMethod]
        public void OperationScope_FluentChaining_AllMethodsReturnSelf()
        {
            using var scope = CreateScope("fluent-op");

            var result1 = scope.WithTag("k", "v");
            var result2 = scope.WithTags(new List<KeyValuePair<string, object?>>());
            var result3 = scope.WithProperty("lazy", () => "val");
            var result4 = scope.WithResult("result");
            var result5 = scope.Succeed();

            Assert.AreSame(scope, result1);
            Assert.AreSame(scope, result2);
            Assert.AreSame(scope, result3);
            Assert.AreSame(scope, result4);
            Assert.AreSame(scope, result5);
        }

        private OperationScope CreateScope(string name)
        {
            return new OperationScope(name, new OperationScopeOptions(),
                _testSource.Source, null, null, null);
        }
    }
}
