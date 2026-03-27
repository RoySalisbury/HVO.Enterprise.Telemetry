using System;
using System.Collections.Generic;
using System.Reflection;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Capture;
using HVO.Enterprise.Telemetry.Proxies;
using HVO.Enterprise.Telemetry.Tests.Proxies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Capture
{
    [TestClass]
    public class CaptureExtensionTests
    {
        private FakeOperationScopeFactory _scopeFactory = null!;

        [TestInitialize]
        public void Setup()
        {
            _scopeFactory = new FakeOperationScopeFactory();
        }

        // ─── CaptureParameters extension ────────────────────────────────

        [TestMethod]
        public void CaptureParameters_AddsTagsToScope()
        {
            var scope = _scopeFactory.Begin("TestOp");
            var method = typeof(ISampleService).GetMethod("DoWork")!;
            var parameters = method.GetParameters();
            var values = new object?[] { 42, "hello" };

            scope.CaptureParameters(parameters, values);

            var fake = _scopeFactory.LastScope!;
            Assert.AreEqual(42, fake.Tags["param.id"]);
            Assert.AreEqual("hello", fake.Tags["param.data"]);
        }

        [TestMethod]
        public void CaptureParameters_WithCustomCapture_UsesIt()
        {
            var scope = _scopeFactory.Begin("TestOp");
            var method = typeof(ISampleService).GetMethod("DoWork")!;
            var parameters = method.GetParameters();
            var values = new object?[] { 42, "hello" };
            var capture = new ParameterCapture(registerDefaults: false);

            scope.CaptureParameters(parameters, values, capture, ParameterCaptureOptions.Default);

            var fake = _scopeFactory.LastScope!;
            Assert.IsTrue(fake.Tags.ContainsKey("param.id"));
        }

        [TestMethod]
        public void CaptureParameters_NoneLevel_AddsNoTags()
        {
            var scope = _scopeFactory.Begin("TestOp");
            var method = typeof(ISampleService).GetMethod("DoWork")!;
            var parameters = method.GetParameters();
            var values = new object?[] { 42, "hello" };
            var options = new ParameterCaptureOptions { Level = CaptureLevel.None };

            scope.CaptureParameters(parameters, values, null, options);

            var fake = _scopeFactory.LastScope!;
            Assert.AreEqual(0, fake.Tags.Count);
        }

        // ─── CaptureReturnValue extension ───────────────────────────────

        [TestMethod]
        public void CaptureReturnValue_AddsResultTag()
        {
            var scope = _scopeFactory.Begin("TestOp");

            scope.CaptureReturnValue("result-value", typeof(string));

            var fake = _scopeFactory.LastScope!;
            Assert.AreEqual("result-value", fake.Tags["result"]);
        }

        [TestMethod]
        public void CaptureReturnValue_NullValue_NoTag()
        {
            var scope = _scopeFactory.Begin("TestOp");

            scope.CaptureReturnValue(null, typeof(string));

            var fake = _scopeFactory.LastScope!;
            Assert.IsFalse(fake.Tags.ContainsKey("result"));
        }

        [TestMethod]
        public void CaptureReturnValue_ComplexType_CapturedPerLevel()
        {
            var scope = _scopeFactory.Begin("TestOp");
            var dto = new ReturnDto { Id = 42 };

            scope.CaptureReturnValue(dto, typeof(ReturnDto), null,
                new ParameterCaptureOptions
                {
                    Level = CaptureLevel.Verbose,
                    UseCustomToString = false,
                    CapturePropertyNames = true
                });

            var fake = _scopeFactory.LastScope!;
            Assert.IsNotNull(fake.Tags["result"]);
            Assert.IsInstanceOfType(fake.Tags["result"], typeof(Dictionary<string, object?>));
        }

        // ─── Argument validation ────────────────────────────────────────

        [TestMethod]
        public void CaptureParameters_NullScope_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                IOperationScope scope = null!;
                var method = typeof(ISampleService).GetMethod("DoWork")!;
                scope.CaptureParameters(method.GetParameters(), new object?[] { 1, "x" });
            });
        }

        [TestMethod]
        public void CaptureParameters_NullParams_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var scope = _scopeFactory.Begin("TestOp");
                scope.CaptureParameters(null!, new object?[] { });
            });
        }

        [TestMethod]
        public void CaptureParameters_NullValues_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var scope = _scopeFactory.Begin("TestOp");
                scope.CaptureParameters(new ParameterInfo[0], null!);
            });
        }

        [TestMethod]
        public void CaptureReturnValue_NullScope_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                IOperationScope scope = null!;
                scope.CaptureReturnValue("x", typeof(string));
            });
        }

        [TestMethod]
        public void CaptureReturnValue_NullReturnType_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var scope = _scopeFactory.Begin("TestOp");
                scope.CaptureReturnValue("x", null!);
            });
        }

        // ─── Chaining ───────────────────────────────────────────────────

        [TestMethod]
        public void CaptureParameters_ReturnsSameScope()
        {
            var scope = _scopeFactory.Begin("TestOp");
            var method = typeof(ISampleService).GetMethod("DoWork")!;

            var result = scope.CaptureParameters(method.GetParameters(), new object?[] { 1, "x" });

            Assert.AreSame(scope, result);
        }

        [TestMethod]
        public void CaptureReturnValue_ReturnsSameScope()
        {
            var scope = _scopeFactory.Begin("TestOp");

            var result = scope.CaptureReturnValue("val", typeof(string));

            Assert.AreSame(scope, result);
        }

        // ─── Helper types ───────────────────────────────────────────────

        public interface ISampleService
        {
            void DoWork(int id, string data);
        }

        public class ReturnDto
        {
            public int Id { get; set; }
        }
    }
}
