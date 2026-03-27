using System;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Proxies
{
    [TestClass]
    public class AsyncProxyTests
    {
        private FakeOperationScopeFactory _scopeFactory = null!;
        private TelemetryProxyFactory _factory = null!;

        [TestInitialize]
        public void Setup()
        {
            _scopeFactory = new FakeOperationScopeFactory();
            _factory = new TelemetryProxyFactory(_scopeFactory);
        }

        // ─── TASK<T> ───────────────────────────────────────────────────

        [TestMethod]
        public async Task AsyncMethod_TaskOfT_ReturnsResult()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            var result = await proxy.GetValueAsync(42);

            Assert.AreEqual("async-value-42", result);
        }

        [TestMethod]
        public async Task AsyncMethod_TaskOfT_CreatesScope()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            await proxy.GetValueAsync(1);

            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            // Custom operation name from [InstrumentMethod(OperationName = "Custom.Get")]
            Assert.AreEqual("Custom.Get", _scopeFactory.LastScope!.Name);
        }

        [TestMethod]
        public async Task AsyncMethod_TaskOfT_Success_CallsSucceed()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            await proxy.GetValueAsync(1);

            Assert.IsTrue(_scopeFactory.LastScope!.DidSucceed);
        }

        [TestMethod]
        public async Task AsyncMethod_TaskOfT_CapturesReturnValue()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            await proxy.GetValueAsync(5);

            // CaptureReturnValue = true on GetValueAsync
            Assert.IsTrue(_scopeFactory.LastScope!.Tags.ContainsKey("result"));
            Assert.AreEqual("async-value-5", _scopeFactory.LastScope.Tags["result"]);
        }

        [TestMethod]
        public async Task AsyncMethod_TaskOfT_CapturesParameters()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            await proxy.GetValueAsync(77);

            Assert.IsTrue(_scopeFactory.LastScope!.Tags.ContainsKey("param.id"));
            Assert.AreEqual(77, _scopeFactory.LastScope.Tags["param.id"]);
        }

        // ─── PLAIN TASK ────────────────────────────────────────────────

        [TestMethod]
        public async Task AsyncMethod_VoidTask_Completes()
        {
            var proxy = _factory.CreateProxy<IVoidTaskService>(new VoidTaskService());

            await proxy.DoWorkAsync();

            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            Assert.IsTrue(_scopeFactory.LastScope!.DidSucceed);
        }

        [TestMethod]
        public async Task AsyncMethod_VoidTask_CreatesScope()
        {
            var proxy = _factory.CreateProxy<IVoidTaskService>(new VoidTaskService());

            await proxy.DoWorkAsync();

            Assert.AreEqual("IVoidTaskService.DoWorkAsync", _scopeFactory.LastScope!.Name);
        }

        // ─── CLASS-LEVEL ASYNC ───────────────────────────────────────────

        [TestMethod]
        public async Task ClassLevel_AsyncMethod_InstrumentedWithPrefix()
        {
            var proxy = _factory.CreateProxy<IOrderService>(new OrderService());

            var result = await proxy.GetOrderAsync(123);

            Assert.AreEqual("order-123", result);
            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            Assert.AreEqual("OrderSvc.GetOrderAsync", _scopeFactory.LastScope!.Name);
        }

        // ─── ASYNC EXCEPTION HANDLING ───────────────────────────────────

        [TestMethod]
        public async Task AsyncMethod_PlainTask_Exception_ScopeRecordsFail()
        {
            var proxy = _factory.CreateProxy<IExceptionService>(new ExceptionService());

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => proxy.ThrowAsync());

            Assert.IsNotNull(_scopeFactory.LastScope);
            Assert.IsNotNull(_scopeFactory.LastScope!.FailException);
            Assert.AreEqual("async-boom", _scopeFactory.LastScope.FailException!.Message);
            Assert.IsFalse(_scopeFactory.LastScope.DidSucceed);
        }

        [TestMethod]
        public async Task AsyncMethod_TaskOfT_Exception_ScopeRecordsFail()
        {
            var proxy = _factory.CreateProxy<IExceptionService>(new ExceptionService());

            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => proxy.ThrowAsyncWithResult());

            Assert.IsNotNull(_scopeFactory.LastScope!.FailException);
            Assert.AreEqual("async-result-boom", _scopeFactory.LastScope.FailException!.Message);
        }

        [TestMethod]
        public async Task AsyncMethod_Exception_PropagatesOriginalType()
        {
            var proxy = _factory.CreateProxy<IExceptionService>(new ExceptionService());

            var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => proxy.ThrowAsync());

            Assert.AreEqual("async-boom", ex.Message);
        }

        // ─── MIXED SYNC/ASYNC ───────────────────────────────────────────

        [TestMethod]
        public async Task MixedCalls_AllGetScopes()
        {
            var proxy = _factory.CreateProxy<ISimpleService>(new SimpleService());

            proxy.GetValue(1);          // sync
            await proxy.GetValueAsync(2); // async

            Assert.AreEqual(2, _scopeFactory.CreatedScopes.Count);
            Assert.AreEqual("ISimpleService.GetValue", _scopeFactory.CreatedScopes[0].Name);
            Assert.AreEqual("Custom.Get", _scopeFactory.CreatedScopes[1].Name);
        }

        // ─── SYNC EXCEPTIONS IN ASYNC METHODS ──────────────────────────

        [TestMethod]
        public async Task AsyncMethod_SyncThrowInTaskOfT_ScopeRecordsFail()
        {
            var proxy = _factory.CreateProxy<ISyncThrowingAsyncService>(
                new SyncThrowingAsyncService());

            await Assert.ThrowsExactlyAsync<ArgumentException>(
                async () => await proxy.ValidatedGetAsync(-1));

            // The scope should have been created before invocation and recorded the failure
            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            Assert.IsNotNull(_scopeFactory.LastScope!.FailException);
            Assert.IsInstanceOfType(_scopeFactory.LastScope.FailException, typeof(ArgumentException));
        }

        [TestMethod]
        public async Task AsyncMethod_SyncThrowInPlainTask_ScopeRecordsFail()
        {
            var proxy = _factory.CreateProxy<ISyncThrowingAsyncService>(
                new SyncThrowingAsyncService());

            await Assert.ThrowsExactlyAsync<ArgumentException>(
                async () => await proxy.ValidatedDoAsync(-1));

            Assert.AreEqual(1, _scopeFactory.CreatedScopes.Count);
            Assert.IsNotNull(_scopeFactory.LastScope!.FailException);
            Assert.IsInstanceOfType(_scopeFactory.LastScope.FailException, typeof(ArgumentException));
        }

        [TestMethod]
        public async Task AsyncMethod_SyncThrowInTaskOfT_PropagatesOriginalException()
        {
            var proxy = _factory.CreateProxy<ISyncThrowingAsyncService>(
                new SyncThrowingAsyncService());

            var ex = await Assert.ThrowsExactlyAsync<ArgumentException>(
                async () => await proxy.ValidatedGetAsync(0));

            Assert.AreEqual("id", ex.ParamName);
        }

        [TestMethod]
        public async Task AsyncMethod_SyncThrowInTaskOfT_SuccessStillWorks()
        {
            var proxy = _factory.CreateProxy<ISyncThrowingAsyncService>(
                new SyncThrowingAsyncService());

            var result = await proxy.ValidatedGetAsync(5);

            Assert.AreEqual(50, result);
            Assert.IsTrue(_scopeFactory.LastScope!.DidSucceed);
        }

        [TestMethod]
        public async Task AsyncMethod_Exception_PreservesExceptionType()
        {
            var proxy = _factory.CreateProxy<IExceptionService>(new ExceptionService());

            // Verify that the exception type and message are preserved through the proxy
            var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => proxy.ThrowAsync());

            Assert.AreEqual("async-boom", ex.Message);
        }
    }
}
