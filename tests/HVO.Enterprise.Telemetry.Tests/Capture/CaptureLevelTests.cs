using System;
using System.Collections.Generic;
using System.Reflection;
using HVO.Enterprise.Telemetry.Capture;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Capture
{
    [TestClass]
    public class CaptureLevelTests
    {
        private ParameterCapture _capture = null!;

        [TestInitialize]
        public void Setup()
        {
            _capture = new ParameterCapture();
        }

        // ─── None level ─────────────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_NoneLevel_ReturnsNull()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.None };

            var result = _capture.CaptureParameter("value", 123, typeof(int), options);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void CaptureParameters_NoneLevel_ReturnsEmptyDict()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.None };
            var method = typeof(ISampleService).GetMethod("DoWork")!;
            var parameters = method.GetParameters();
            var values = new object?[] { 42, "hello" };

            var result = _capture.CaptureParameters(parameters, values, options);

            Assert.AreEqual(0, result.Count);
        }

        // ─── Minimal level ──────────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_MinimalLevel_CapturesPrimitives()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Minimal };

            Assert.AreEqual(123, _capture.CaptureParameter("value", 123, typeof(int), options));
            Assert.AreEqual("hello", _capture.CaptureParameter("name", "hello", typeof(string), options));
            Assert.AreEqual(true, _capture.CaptureParameter("flag", true, typeof(bool), options));
            Assert.AreEqual(3.14m, _capture.CaptureParameter("price", 3.14m, typeof(decimal), options));
        }

        [TestMethod]
        public void CaptureParameter_MinimalLevel_SkipsCollections()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Minimal };
            var list = new List<int> { 1, 2, 3 };

            var result = _capture.CaptureParameter("items", list, typeof(List<int>), options);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void CaptureParameter_MinimalLevel_SkipsComplexTypes()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Minimal };
            var obj = new SampleDto { Name = "test", Age = 25 };

            var result = _capture.CaptureParameter("dto", obj, typeof(SampleDto), options);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void CaptureParameter_MinimalLevel_CapturesEnums()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Minimal };

            var result = _capture.CaptureParameter("level", CaptureLevel.Verbose, typeof(CaptureLevel), options);

            Assert.AreEqual("Verbose", result);
        }

        [TestMethod]
        public void CaptureParameter_MinimalLevel_CapturesDateTime()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Minimal };
            var dt = new DateTime(2026, 2, 9);

            var result = _capture.CaptureParameter("date", dt, typeof(DateTime), options);

            Assert.AreEqual(dt, result);
        }

        [TestMethod]
        public void CaptureParameter_MinimalLevel_CapturesGuid()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Minimal };
            var guid = Guid.NewGuid();

            var result = _capture.CaptureParameter("id", guid, typeof(Guid), options);

            Assert.AreEqual(guid, result);
        }

        // ─── Standard level ─────────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_StandardLevel_CapturesPrimitives()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };

            Assert.AreEqual(42, _capture.CaptureParameter("x", 42, typeof(int), options));
            Assert.AreEqual("text", _capture.CaptureParameter("s", "text", typeof(string), options));
        }

        [TestMethod]
        public void CaptureParameter_StandardLevel_CapturesCollections()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };
            var list = new List<int> { 1, 2, 3 };

            var result = _capture.CaptureParameter("items", list, typeof(List<int>), options);

            Assert.IsInstanceOfType(result, typeof(List<object?>));
            var captured = (List<object?>)result;
            Assert.AreEqual(3, captured.Count);
            Assert.AreEqual(1, captured[0]);
            Assert.AreEqual(2, captured[1]);
            Assert.AreEqual(3, captured[2]);
        }

        [TestMethod]
        public void CaptureParameter_StandardLevel_ComplexObject_UsesToString()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };
            var obj = new ToStringDto { Value = "test-42" };

            var result = _capture.CaptureParameter("dto", obj, typeof(ToStringDto), options);

            Assert.AreEqual("ToStringDto:test-42", result);
        }

        // ─── Verbose level ──────────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_VerboseLevel_CapturesComplexObject()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                CapturePropertyNames = true,
                UseCustomToString = false
            };
            var obj = new SampleDto { Name = "Alice", Age = 30 };

            var result = _capture.CaptureParameter("dto", obj, typeof(SampleDto), options);

            Assert.IsInstanceOfType(result, typeof(Dictionary<string, object?>));
            var dict = (Dictionary<string, object?>)result;
            Assert.AreEqual("Alice", dict["Name"]);
            Assert.AreEqual(30, dict["Age"]);
        }

        [TestMethod]
        public void CaptureParameter_VerboseLevel_CapturesNestedObject()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                MaxDepth = 3,
                CapturePropertyNames = true,
                UseCustomToString = false
            };
            var obj = new NestedDto
            {
                Id = 1,
                Child = new SampleDto { Name = "Child", Age = 5 }
            };

            var result = _capture.CaptureParameter("dto", obj, typeof(NestedDto), options);

            Assert.IsInstanceOfType(result, typeof(Dictionary<string, object?>));
            var dict = (Dictionary<string, object?>)result;
            Assert.AreEqual(1, dict["Id"]);

            Assert.IsInstanceOfType(dict["Child"], typeof(Dictionary<string, object?>));
            var childDict = (Dictionary<string, object?>)dict["Child"]!;
            Assert.AreEqual("Child", childDict["Name"]);
            Assert.AreEqual(5, childDict["Age"]);
        }

        [TestMethod]
        public void CaptureParameter_VerboseLevel_CustomToString_Used()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                UseCustomToString = true
            };
            var obj = new ToStringDto { Value = "custom-42" };

            var result = _capture.CaptureParameter("dto", obj, typeof(ToStringDto), options);

            // UseCustomToString is true and ToStringDto overrides ToString.
            Assert.AreEqual("ToStringDto:custom-42", result);
        }

        [TestMethod]
        public void CaptureParameter_VerboseLevel_NoCustomToString_TraversesProperties()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                UseCustomToString = false,
                CapturePropertyNames = true
            };
            var obj = new ToStringDto { Value = "prop-value" };

            var result = _capture.CaptureParameter("dto", obj, typeof(ToStringDto), options);

            Assert.IsInstanceOfType(result, typeof(Dictionary<string, object?>));
            var dict = (Dictionary<string, object?>)result;
            Assert.AreEqual("prop-value", dict["Value"]);
        }

        // ─── Depth limits ───────────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_MaxDepthReached_ReturnsMessage()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                MaxDepth = 1,
                UseCustomToString = false,
                CapturePropertyNames = true
            };
            var obj = new NestedDto
            {
                Id = 1,
                Child = new SampleDto { Name = "Deep" }
            };

            var result = _capture.CaptureParameter("dto", obj, typeof(NestedDto), options);

            Assert.IsInstanceOfType(result, typeof(Dictionary<string, object?>));
            var dict = (Dictionary<string, object?>)result;
            Assert.AreEqual(1, dict["Id"]);
            Assert.IsTrue(dict["Child"]!.ToString()!.Contains("Max depth"), $"Got: {dict["Child"]}");
        }

        [TestMethod]
        public void CaptureParameter_MaxDepthZero_ImmediateLimit()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                MaxDepth = 0
            };
            var obj = new SampleDto { Name = "test" };

            var result = _capture.CaptureParameter("dto", obj, typeof(SampleDto), options);

            Assert.IsTrue(result!.ToString()!.Contains("Max depth 0 reached"));
        }

        // ─── Collection limits ──────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_CollectionLimitExceeded_Truncates()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Standard,
                MaxCollectionItems = 3
            };
            var list = new List<int> { 1, 2, 3, 4, 5 };

            var result = _capture.CaptureParameter("items", list, typeof(List<int>), options);

            Assert.IsInstanceOfType(result, typeof(List<object?>));
            var captured = (List<object?>)result;
            Assert.AreEqual(4, captured.Count); // 3 items + truncation marker
            Assert.AreEqual(1, captured[0]);
            Assert.AreEqual(2, captured[1]);
            Assert.AreEqual(3, captured[2]);
            Assert.IsTrue(captured[3]!.ToString()!.Contains("truncated after 3 items"));
        }

        [TestMethod]
        public void CaptureParameter_CollectionSmallEnough_NoTruncation()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Standard,
                MaxCollectionItems = 10
            };
            var list = new List<int> { 10, 20 };

            var result = _capture.CaptureParameter("items", list, typeof(List<int>), options);

            Assert.IsInstanceOfType(result, typeof(List<object?>));
            var captured = (List<object?>)result;
            Assert.AreEqual(2, captured.Count);
        }

        [TestMethod]
        public void CaptureParameter_DictionaryCollection_Captured()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };
            var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };

            var result = _capture.CaptureParameter("map", dict, typeof(Dictionary<string, int>), options);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(List<object?>));
        }

        [TestMethod]
        public void CaptureParameter_ArrayCollection_Captured()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };
            var arr = new int[] { 1, 2, 3 };

            var result = _capture.CaptureParameter("arr", arr, typeof(int[]), options);

            Assert.IsInstanceOfType(result, typeof(List<object?>));
            var captured = (List<object?>)result;
            Assert.AreEqual(3, captured.Count);
        }

        // ─── String truncation ──────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_LongString_Truncated()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Minimal,
                MaxStringLength = 10
            };
            var longStr = new string('x', 100);

            var result = _capture.CaptureParameter("text", longStr, typeof(string), options);

            Assert.IsInstanceOfType(result, typeof(string));
            var str = (string)result;
            Assert.IsTrue(str.Length < 100);
            Assert.IsTrue(str.Contains("100 chars"));
        }

        [TestMethod]
        public void CaptureParameter_ShortString_NotTruncated()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Minimal,
                MaxStringLength = 1000
            };

            var result = _capture.CaptureParameter("text", "short", typeof(string), options);

            Assert.AreEqual("short", result);
        }

        // ─── Null handling ──────────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_NullValue_ReturnsNull()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Verbose };

            var result = _capture.CaptureParameter("value", null, typeof(string), options);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void CaptureParameters_NullValue_CapturedAsNull()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };
            var method = typeof(ISampleService).GetMethod("DoWork")!;
            var parameters = method.GetParameters();
            var values = new object?[] { null, null };

            var result = _capture.CaptureParameters(parameters, values, options);

            Assert.IsTrue(result.ContainsKey("id"));
            Assert.IsNull(result["id"]);
        }

        // ─── Custom serializer ──────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_CustomSerializer_Used()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                CustomSerializers = new Dictionary<Type, Func<object, object?>>
                {
                    [typeof(SampleDto)] = obj =>
                    {
                        var dto = (SampleDto)obj;
                        return $"Custom:{dto.Name}";
                    }
                }
            };
            var obj = new SampleDto { Name = "Alice" };

            var result = _capture.CaptureParameter("dto", obj, typeof(SampleDto), options);

            Assert.AreEqual("Custom:Alice", result);
        }

        [TestMethod]
        public void CaptureParameter_CustomSerializer_FallsBackOnError()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                UseCustomToString = false,
                CapturePropertyNames = true,
                CustomSerializers = new Dictionary<Type, Func<object, object?>>
                {
                    [typeof(SampleDto)] = _ => throw new InvalidOperationException("boom")
                }
            };
            var obj = new SampleDto { Name = "Bob" };

            // Should fall through to default property capture.
            var result = _capture.CaptureParameter("dto", obj, typeof(SampleDto), options);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Dictionary<string, object?>));
        }

        // ─── Argument validation ────────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_NullName_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _capture.CaptureParameter(null!, "x", typeof(string), ParameterCaptureOptions.Default));
        }

        [TestMethod]
        public void CaptureParameter_NullType_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _capture.CaptureParameter("x", "v", null!, ParameterCaptureOptions.Default));
        }

        [TestMethod]
        public void CaptureParameter_NullOptions_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _capture.CaptureParameter("x", "v", typeof(string), null!));
        }

        [TestMethod]
        public void CaptureParameters_NullParameters_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _capture.CaptureParameters(null!, new object?[] { }, ParameterCaptureOptions.Default));
        }

        [TestMethod]
        public void CaptureParameters_NullValues_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _capture.CaptureParameters(new ParameterInfo[0], null!, ParameterCaptureOptions.Default));
        }

        [TestMethod]
        public void CaptureParameters_NullOptions_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _capture.CaptureParameters(new ParameterInfo[0], new object?[] { }, null!));
        }

        // ─── CaptureLevel enum values ───────────────────────────────────

        [TestMethod]
        public void CaptureLevel_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)CaptureLevel.None);
            Assert.AreEqual(1, (int)CaptureLevel.Minimal);
            Assert.AreEqual(2, (int)CaptureLevel.Standard);
            Assert.AreEqual(3, (int)CaptureLevel.Verbose);
        }

        // ─── Multiple parameters ────────────────────────────────────────

        [TestMethod]
        public void CaptureParameters_MultipleParams_AllCaptured()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };
            var method = typeof(ISampleService).GetMethod("DoWork")!;
            var parameters = method.GetParameters();
            var values = new object?[] { 42, "hello" };

            var result = _capture.CaptureParameters(parameters, values, options);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(42, result["id"]);
            Assert.AreEqual("hello", result["data"]);
        }

        // ─── Helper types ───────────────────────────────────────────────

        public interface ISampleService
        {
            void DoWork(int id, string data);
        }

        public class SampleDto
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        public class NestedDto
        {
            public int Id { get; set; }
            public SampleDto? Child { get; set; }
        }

        public class ToStringDto
        {
            public string? Value { get; set; }
            public override string ToString() => $"ToStringDto:{Value}";
        }
    }
}
