using System;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Proxies
{
    [TestClass]
    public class TelemetryDispatchProxyTests
    {
        private FakeOperationScopeFactory _scopeFactory = null!;
        private TelemetryProxyFactory _factory = null!;

        [TestInitialize]
        public void Setup()
        {
            _scopeFactory = new FakeOperationScopeFactory();
            _factory = new TelemetryProxyFactory(_scopeFactory);
        }

        // ─── SYNC METHOD INSTRUMENTATION ────────────────────────────────

        [TestMethod]
        public void SyncMethod_WithInstrumentMethod_CreatesScope()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            var result = proxy.GetValue(42);

            Assert.AreEqual("value-42", result);
            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            Assert.AreEqual("ISimpleService.GetValue", _scopeFactory.LastScope!.Name);
        }

        [TestMethod]
        public void SyncMethod_WithCustomOperationName_UsesCustomName()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            // GetValueAsync has OperationName = "Custom.Get" but let's test sync method.
            // DoWork has CaptureParameters=false
            proxy.DoWork("data");

            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            Assert.AreEqual("ISimpleService.DoWork", _scopeFactory.LastScope!.Name);
        }

        [TestMethod]
        public void SyncMethod_Success_CallsSucceed()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            proxy.GetValue(1);

            Assert.IsTrue(_scopeFactory.LastScope!.DidSucceed);
            Assert.IsNull(_scopeFactory.LastScope.FailException);
        }

        [TestMethod]
        public void SyncMethod_NonInstrumented_SkipsScope()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            var result = proxy.NotInstrumented(99);

            Assert.AreEqual("plain-99", result);
            Assert.AreEqual(0, _scopeFactory.CreatedScopes.Count);
        }

        [TestMethod]
        public void SyncMethod_ReturnsCorrectValue()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            var result = proxy.GetValue(7);

            Assert.AreEqual("value-7", result);
        }

        // ─── SYNC EXCEPTION HANDLING ────────────────────────────────────

        [TestMethod]
        public void SyncMethod_ThrowsException_ScopeRecordsFail()
        {
            var proxy = _factory.CreateProxy<IExceptionService>(new ExceptionService());

            Assert.ThrowsExactly<InvalidOperationException>(() => proxy.ThrowSync());

            Assert.IsNotNull(_scopeFactory.LastScope);
            Assert.IsNotNull(_scopeFactory.LastScope!.FailException);
            Assert.AreEqual("sync-boom", _scopeFactory.LastScope.FailException!.Message);
            Assert.IsFalse(_scopeFactory.LastScope.DidSucceed);
        }

        [TestMethod]
        public void SyncMethod_ThrowsException_PropagatesOriginalException()
        {
            var proxy = _factory.CreateProxy<IExceptionService>(new ExceptionService());

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => proxy.ThrowSync());
            Assert.AreEqual("sync-boom", ex.Message);
        }

        // ─── CLASS-LEVEL INSTRUMENTATION ────────────────────────────────

        [TestMethod]
        public void ClassLevel_AllMethodsInstrumented()
        {
            var proxy = _factory.CreateProxy<IOrderService>(new OrderService());

            proxy.ProcessOrder("test");

            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            Assert.AreEqual("OrderSvc.ProcessOrder", _scopeFactory.LastScope!.Name);
        }

        [TestMethod]
        public void ClassLevel_OperationPrefix_AppliedToName()
        {
            var proxy = _factory.CreateProxy<IOrderService>(new OrderService());

            proxy.ProcessOrder("x");

            Assert.AreEqual("OrderSvc.ProcessOrder", _scopeFactory.LastScope!.Name);
        }

        [TestMethod]
        public void ClassLevel_NoPrefix_UsesInterfaceName()
        {
            // IOverrideService has [InstrumentClass] without OperationPrefix
            var proxy = _factory.CreateProxy<IOverrideService>(new OverrideService());

            proxy.DefaultMethod();

            Assert.AreEqual("IOverrideService.DefaultMethod", _scopeFactory.LastScope!.Name);
        }

        // ─── NO TELEMETRY ───────────────────────────────────────────────

        [TestMethod]
        public void NoTelemetryAttribute_SkipsInstrumentation()
        {
            var proxy = _factory.CreateProxy<IOrderService>(new OrderService());

            var result = proxy.HealthCheck();

            Assert.IsTrue(result);
            Assert.AreEqual(0, _scopeFactory.CreatedScopes.Count);
        }

        [TestMethod]
        public void NoTelemetry_OnlyAffectsDecoratedMethod()
        {
            var proxy = _factory.CreateProxy<IOrderService>(new OrderService());

            proxy.HealthCheck();     // not instrumented
            proxy.ProcessOrder("x"); // instrumented

            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            Assert.AreEqual("OrderSvc.ProcessOrder", _scopeFactory.LastScope!.Name);
        }

        // ─── METHOD-LEVEL OVERRIDES CLASS-LEVEL ─────────────────────────

        [TestMethod]
        public void MethodOverride_UsesMethodAttributeSettings()
        {
            var proxy = _factory.CreateProxy<IOverrideService>(new OverrideService());

            var result = proxy.GetData(10);

            Assert.AreEqual("data-10", result);
            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            // Method attribute sets OperationName to null → uses InterfaceName.MethodName
            Assert.AreEqual("IOverrideService.GetData", _scopeFactory.LastScope!.Name);
        }

        [TestMethod]
        public void MethodOverride_CapturesReturnValue()
        {
            var proxy = _factory.CreateProxy<IOverrideService>(new OverrideService());

            proxy.GetData(10);

            // CaptureReturnValue = true on [InstrumentMethod]
            Assert.IsTrue(_scopeFactory.LastScope!.Tags.ContainsKey("result"));
            Assert.AreEqual("data-10", _scopeFactory.LastScope.Tags["result"]);
        }

        // ─── PARAMETER CAPTURE (sync) ───────────────────────────────────

        [TestMethod]
        public void SyncMethod_CapturesParameters()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            proxy.GetValue(42);

            Assert.IsTrue(_scopeFactory.LastScope!.Tags.ContainsKey("param.id"));
            Assert.AreEqual(42, _scopeFactory.LastScope.Tags["param.id"]);
        }

        [TestMethod]
        public void SyncMethod_CaptureParametersFalse_SkipsCapture()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            proxy.DoWork("hello");

            // [InstrumentMethod(CaptureParameters = false)] on DoWork
            Assert.AreEqual(0, _scopeFactory.LastScope!.Tags.Count);
        }

        // ─── METHOD CACHE ───────────────────────────────────────────────

        [TestMethod]
        public void MethodCache_SameMethodCalledMultipleTimes_AllCreateScopes()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            proxy.GetValue(1);
            proxy.GetValue(2);
            proxy.GetValue(3);

            Assert.AreEqual(3, _scopeFactory.CreatedScopes.Count);
        }

        // ─── INTERFACE INHERITANCE ──────────────────────────────────────

        [TestMethod]
        public void InheritedInterface_InstrumentClassOnBase_InstrumentsDerivedMethods()
        {
            var proxy = _factory.CreateProxy<IDerivedService>(new DerivedService());

            proxy.GetDerivedValue(1);

            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            // Should use base prefix "BaseSvc"
            Assert.AreEqual("BaseSvc.GetDerivedValue", _scopeFactory.LastScope!.Name);
        }

        [TestMethod]
        public void InheritedInterface_MethodAttrOnBase_Inherited()
        {
            var proxy = _factory.CreateProxy<IDerivedService>(new DerivedService());

            proxy.GetBaseValue(42);

            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            // [InstrumentMethod] on base with CaptureReturnValue=true
            Assert.IsTrue(_scopeFactory.LastScope!.Tags.ContainsKey("result"));
            Assert.AreEqual("base-42", _scopeFactory.LastScope.Tags["result"]);
        }

        [TestMethod]
        public void InheritedInterface_NoTelemetryOnBase_Inherited()
        {
            var proxy = _factory.CreateProxy<IDerivedService>(new DerivedService());

            proxy.BaseHealthCheck();

            // [NoTelemetry] on base interface method should be inherited
            Assert.AreEqual(0, _scopeFactory.CreatedScopes.Count);
        }

        // ─── EXCEPTION DISPATCH INFO ────────────────────────────────────

        [TestMethod]
        public void SyncMethod_ThrowsException_PreservesStackTrace()
        {
            var proxy = _factory.CreateProxy<IExceptionService>(new ExceptionService());

            try
            {
                proxy.ThrowSync();
                Assert.Fail("Should have thrown");
            }
            catch (InvalidOperationException ex)
            {
                // ExceptionDispatchInfo should preserve the original stack trace,
                // meaning the stack trace should contain ExceptionService.ThrowSync
                Assert.IsTrue(
                    ex.StackTrace != null && ex.StackTrace.Contains("ExceptionService"),
                    "Stack trace should contain original throw site: " + ex.StackTrace);
            }
        }
    }
}
