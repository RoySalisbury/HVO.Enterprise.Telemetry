using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HVO.Enterprise.Telemetry.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Logging
{
    [TestClass]
    public sealed class LogEnrichmentScopeTests
    {
        // --- Constructor ---

        [TestMethod]
        public void Constructor_NullDictionary_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new LogEnrichmentScope(null!));
        }

        [TestMethod]
        public void Constructor_EmptyDictionary_CreatesEmptyScope()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>());

            Assert.AreEqual(0, scope.Count);
        }

        [TestMethod]
        public void Constructor_WithData_CopiesAllEntries()
        {
            var data = new Dictionary<string, object?>
            {
                ["TraceId"] = "abc123",
                ["SpanId"] = "def456",
                ["CorrelationId"] = "ghi789"
            };

            var scope = new LogEnrichmentScope(data);

            Assert.AreEqual(3, scope.Count);
        }

        // --- ToString ---

        [TestMethod]
        public void ToString_EmptyScope_ReturnsEmptyString()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>());

            Assert.AreEqual(string.Empty, scope.ToString());
        }

        [TestMethod]
        public void ToString_SingleEntry_ReturnsKeyColonValue()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = "abc123"
            });

            Assert.AreEqual("CorrelationId:abc123", scope.ToString());
        }

        [TestMethod]
        public void ToString_MultipleEntries_ReturnsCommaSeparatedPairs()
        {
            // Use a single entry to be deterministic about order, then verify format
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["Key1"] = "Value1",
                ["Key2"] = "Value2"
            });

            var result = scope.ToString();

            // Contains both pairs
            Assert.IsTrue(result.Contains("Key1:Value1"), $"Expected Key1:Value1 in '{result}'");
            Assert.IsTrue(result.Contains("Key2:Value2"), $"Expected Key2:Value2 in '{result}'");
            // Separated by ", "
            Assert.IsTrue(result.Contains(", "), $"Expected comma separator in '{result}'");
        }

        [TestMethod]
        public void ToString_NullValue_RendersAsNull()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["Key"] = null
            });

            Assert.AreEqual("Key:null", scope.ToString());
        }

        [TestMethod]
        public void ToString_IsCached_ReturnsSameInstance()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["A"] = "1"
            });

            var first = scope.ToString();
            var second = scope.ToString();

            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void ToString_DoesNotContainDictionaryTypeName()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["TraceId"] = "abc",
                ["SpanId"] = "def",
                ["CorrelationId"] = "ghi"
            });

            var result = scope.ToString();

            Assert.IsFalse(result.Contains("Dictionary"), $"Should not contain 'Dictionary': '{result}'");
            Assert.IsFalse(result.Contains("System.Collections"), $"Should not contain type info: '{result}'");
        }

        // --- Indexer ---

        [TestMethod]
        public void Indexer_ValidIndex_ReturnsCorrectEntry()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["OnlyKey"] = "OnlyValue"
            });

            var entry = scope[0];

            Assert.AreEqual("OnlyKey", entry.Key);
            Assert.AreEqual("OnlyValue", entry.Value);
        }

        [TestMethod]
        public void Indexer_OutOfRange_ThrowsIndexOutOfRangeException()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["Key"] = "Value"
            });

            Assert.ThrowsExactly<IndexOutOfRangeException>(() => scope[1]);
        }

        // --- IEnumerable ---

        [TestMethod]
        public void GetEnumerator_EnumeratesAllEntries()
        {
            var data = new Dictionary<string, object?>
            {
                ["A"] = "1",
                ["B"] = "2",
                ["C"] = "3"
            };

            var scope = new LogEnrichmentScope(data);
            var enumerated = scope.ToList();

            Assert.AreEqual(3, enumerated.Count);
            Assert.IsTrue(enumerated.Any(e => e.Key == "A" && (string?)e.Value == "1"));
            Assert.IsTrue(enumerated.Any(e => e.Key == "B" && (string?)e.Value == "2"));
            Assert.IsTrue(enumerated.Any(e => e.Key == "C" && (string?)e.Value == "3"));
        }

        [TestMethod]
        public void Scope_IsImmutable_ModifyingOriginalDictDoesNotAffectScope()
        {
            var data = new Dictionary<string, object?>
            {
                ["Key"] = "Original"
            };

            var scope = new LogEnrichmentScope(data);

            // Mutate the original dictionary
            data["Key"] = "Modified";
            data["New"] = "Extra";

            // Scope should be unchanged
            Assert.AreEqual(1, scope.Count);
            Assert.AreEqual("Original", scope[0].Value);
        }

        // --- Integration: structured logging providers can enumerate ---

        [TestMethod]
        public void Scope_AsIEnumerableKeyValuePair_CanBeEnumerated()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["TraceId"] = "trace123",
                ["SpanId"] = "span456"
            });

            IEnumerable<KeyValuePair<string, object?>> enumerable = scope;
            var pairs = new List<KeyValuePair<string, object?>>(enumerable);

            Assert.AreEqual(2, pairs.Count);
            Assert.IsTrue(pairs.Any(p => p.Key == "TraceId"));
            Assert.IsTrue(pairs.Any(p => p.Key == "SpanId"));
        }

        // --- Value Type Formatting: Ensures data is rendered, not type names ---

        [TestMethod]
        public void ToString_IntValue_RendersNumber_NotTypeName()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["Count"] = 42
            });

            var result = scope.ToString();

            Assert.AreEqual("Count:42", result);
            Assert.IsFalse(result.Contains("Int32"), $"Should not contain type name: '{result}'");
            Assert.IsFalse(result.Contains("System."), $"Should not contain System namespace: '{result}'");
        }

        [TestMethod]
        public void ToString_GuidValue_RendersGuidString_NotTypeName()
        {
            var guid = Guid.NewGuid();
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["RequestId"] = guid
            });

            var result = scope.ToString();

            Assert.AreEqual($"RequestId:{guid}", result);
            Assert.IsFalse(result.Contains("Guid"), $"Should not contain type name: '{result}'");
        }

        [TestMethod]
        public void ToString_DateTimeValue_RendersDateString_NotTypeName()
        {
            var dt = new DateTime(2026, 2, 11, 10, 30, 0, DateTimeKind.Utc);
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["Timestamp"] = dt
            });

            var result = scope.ToString();

            Assert.IsFalse(result.Contains("DateTime"), $"Should not contain type name: '{result}'");
            Assert.IsTrue(result.StartsWith("Timestamp:"), $"Should start with key: '{result}'");
            // The DateTime.ToString() result is locale-dependent, but must not be a type name.
            Assert.IsTrue(result.Contains("2026"), $"Should contain the year: '{result}'");
        }

        [TestMethod]
        public void ToString_EnumValue_RendersEnumMemberName_NotTypeName()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["TraceFlags"] = ActivityTraceFlags.Recorded
            });

            var result = scope.ToString();

            Assert.AreEqual("TraceFlags:Recorded", result);
            Assert.IsFalse(result.Contains("ActivityTraceFlags"),
                $"Should not contain enum type name: '{result}'");
        }

        [TestMethod]
        public void ToString_BoolValue_RendersTrueOrFalse_NotTypeName()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["IsRecorded"] = true,
                ["IsFailed"] = false
            });

            var result = scope.ToString();

            Assert.IsTrue(result.Contains("IsRecorded:True"), $"Expected True: '{result}'");
            Assert.IsTrue(result.Contains("IsFailed:False"), $"Expected False: '{result}'");
            Assert.IsFalse(result.Contains("Boolean"), $"Should not contain type name: '{result}'");
        }

        [TestMethod]
        public void ToString_DoubleValue_RendersNumber_NotTypeName()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["Duration"] = 123.456
            });

            var result = scope.ToString();

            Assert.IsTrue(result.StartsWith("Duration:"), $"Should start with key: '{result}'");
            Assert.IsTrue(result.Contains("123.456") || result.Contains("123,456"),
                $"Should contain actual number: '{result}'");
            Assert.IsFalse(result.Contains("Double"), $"Should not contain type name: '{result}'");
        }

        [TestMethod]
        public void ToString_ObjectWithoutToString_RendersFullTypeName_DetectedByTest()
        {
            // This test documents and guards against the "Type name leak" problem.
            // If a value's ToString() returns its type name, the enrichment output
            // will contain garbage like "System.Object" instead of useful data.
            // Custom enrichers should always store string values to avoid this.
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["BadValue"] = new object()
            });

            var result = scope.ToString();

            // object.ToString() returns "System.Object" — this test documents the behavior
            // and serves as a reminder that enrichers should pre-format values as strings.
            Assert.IsTrue(result.Contains("System.Object"),
                $"Plain object should expose the type name problem: '{result}'");
        }

        [TestMethod]
        public void ToString_CustomObjectWithToStringOverride_RendersOverrideValue()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["User"] = new TestFormattableObject("alice@example.com")
            });

            var result = scope.ToString();

            Assert.AreEqual("User:alice@example.com", result);
            Assert.IsFalse(result.Contains("TestFormattableObject"),
                $"Should not contain type name: '{result}'");
        }

        [TestMethod]
        public void ToString_MixedValueTypes_AllRenderAsExpectedData()
        {
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["TraceId"] = "abc123def456",
                ["SpanId"] = "789012345678",
                ["Count"] = 5,
                ["IsActive"] = true,
                ["Label"] = (string?)null
            });

            var result = scope.ToString();

            Assert.IsTrue(result.Contains("TraceId:abc123def456"), $"String value wrong: '{result}'");
            Assert.IsTrue(result.Contains("SpanId:789012345678"), $"String value wrong: '{result}'");
            Assert.IsTrue(result.Contains("Count:5"), $"Int value wrong: '{result}'");
            Assert.IsTrue(result.Contains("IsActive:True"), $"Bool value wrong: '{result}'");
            Assert.IsTrue(result.Contains("Label:null"), $"Null value wrong: '{result}'");
            // Must not contain any System.* type leak
            Assert.IsFalse(result.Contains("System."), $"Should not contain type info: '{result}'");
        }

        [TestMethod]
        public void Enumeration_ValuesPreserveOriginalType_NotConvertedToString()
        {
            // Structured logging providers enumerate the KVPs and rely on the
            // original value type for proper serialization. Ensure that the scope
            // preserves the original object reference, not a ToString() copy.
            var guidValue = Guid.NewGuid();
            var scope = new LogEnrichmentScope(new Dictionary<string, object?>
            {
                ["StringVal"] = "hello",
                ["IntVal"] = 42,
                ["GuidVal"] = guidValue
            });

            var entries = scope.ToList();

            var strEntry = entries.First(e => e.Key == "StringVal");
            Assert.IsInstanceOfType(strEntry.Value, typeof(string));
            Assert.AreEqual("hello", strEntry.Value);

            var intEntry = entries.First(e => e.Key == "IntVal");
            Assert.IsInstanceOfType(intEntry.Value, typeof(int));
            Assert.AreEqual(42, intEntry.Value);

            var guidEntry = entries.First(e => e.Key == "GuidVal");
            Assert.IsInstanceOfType(guidEntry.Value, typeof(Guid));
            Assert.AreEqual(guidValue, guidEntry.Value);
        }

        /// <summary>
        /// Helper class that overrides ToString() to return meaningful data.
        /// </summary>
        private sealed class TestFormattableObject
        {
            private readonly string _displayValue;

            public TestFormattableObject(string displayValue)
            {
                _displayValue = displayValue;
            }

            public override string ToString() => _displayValue;
        }
    }
}
