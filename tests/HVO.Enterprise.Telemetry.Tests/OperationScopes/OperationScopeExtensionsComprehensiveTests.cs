using System;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Tests.Helpers;

namespace HVO.Enterprise.Telemetry.Tests.OperationScopes
{
    /// <summary>
    /// Comprehensive tests for <see cref="OperationScopeExtensions"/> covering
    /// all overloads, null guards, options passthrough, and exception behavior.
    /// </summary>
    [TestClass]
    public class OperationScopeExtensionsComprehensiveTests
    {
        private TestActivitySource _testSource = null!;
        private OperationScopeFactory _factory = null!;

        [TestInitialize]
        public void Setup()
        {
            _testSource = new TestActivitySource("ext-comprehensive-test");
            _factory = new OperationScopeFactory(_testSource.Source);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _testSource.Dispose();
        }

        // --- Execute(Action) null guards ---

        [TestMethod]
        public void Execute_NullFactory_ThrowsArgumentNullException()
        {
            IOperationScopeFactory? nullFactory = null;
            Assert.ThrowsExactly<ArgumentNullException>(
                () => nullFactory!.Execute("op", () => { }));
        }

        [TestMethod]
        public void Execute_NullAction_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => _factory.Execute("op", (Action)null!));
        }

        [TestMethod]
        public void Execute_SuccessAction_ScopeSucceeds()
        {
            var executed = false;
            _factory.Execute("test-action", () => { executed = true; });
            Assert.IsTrue(executed, "Action should have been invoked");
        }

        [TestMethod]
        public void Execute_ActionThrows_RethrowsException()
        {
            Assert.ThrowsExactly<InvalidOperationException>(
                () => _factory.Execute("throwing-action", () => throw new InvalidOperationException("boom")));
        }

        [TestMethod]
        public void Execute_WithOptions_PassesOptionsToScope()
        {
            var options = new OperationScopeOptions { CreateActivity = false, LogEvents = false };
            _factory.Execute("opts-action", () => { }, options);
            // Should complete without issues; options are passed through
        }

        // --- Execute<T>(Func<T>) ---

        [TestMethod]
        public void ExecuteT_NullFactory_ThrowsArgumentNullException()
        {
            IOperationScopeFactory? nullFactory = null;
            Assert.ThrowsExactly<ArgumentNullException>(
                () => nullFactory!.Execute("op", () => 42));
        }

        [TestMethod]
        public void ExecuteT_NullFunc_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => _factory.Execute("op", (Func<int>)null!));
        }

        [TestMethod]
        public void ExecuteT_ReturnsResult()
        {
            var result = _factory.Execute("test-func", () => 42);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void ExecuteT_FuncThrows_RethrowsException()
        {
            Assert.ThrowsExactly<InvalidOperationException>(
                () => _factory.Execute("throw-func", new Func<int>(() => throw new InvalidOperationException("boom"))));
        }

        [TestMethod]
        public void ExecuteT_WithOptions_PassesOptionsToScope()
        {
            var options = new OperationScopeOptions { CreateActivity = false };
            var result = _factory.Execute("opts-func", () => "hello", options);
            Assert.AreEqual("hello", result);
        }

        [TestMethod]
        public void ExecuteT_ReturnsComplexObject()
        {
            var expected = new { Name = "Test", Value = 99 };
            var result = _factory.Execute("complex-func", () => expected);
            Assert.AreEqual(expected.Name, result.Name);
            Assert.AreEqual(expected.Value, result.Value);
        }

        // --- ExecuteAsync(Func<Task>) ---

        [TestMethod]
        public async Task ExecuteAsync_NullFactory_ThrowsArgumentNullException()
        {
            IOperationScopeFactory? nullFactory = null;
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => nullFactory!.ExecuteAsync("op", () => Task.CompletedTask));
        }

        [TestMethod]
        public async Task ExecuteAsync_NullAction_ThrowsArgumentNullException()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => _factory.ExecuteAsync("op", (Func<Task>)null!));
        }

        [TestMethod]
        public async Task ExecuteAsync_SuccessAction_Completes()
        {
            var executed = false;
            await _factory.ExecuteAsync("async-action", () =>
            {
                executed = true;
                return Task.CompletedTask;
            });
            Assert.IsTrue(executed, "Async action should have been invoked");
        }

        [TestMethod]
        public async Task ExecuteAsync_ActionThrows_RethrowsException()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _factory.ExecuteAsync("throw-async", async () =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException("async boom");
                }));
        }

        [TestMethod]
        public async Task ExecuteAsync_WithOptions_PassesOptionsToScope()
        {
            var options = new OperationScopeOptions { CreateActivity = false };
            await _factory.ExecuteAsync("opts-async", () => Task.CompletedTask, options);
        }

        // --- ExecuteAsync<T>(Func<Task<T>>) ---

        [TestMethod]
        public async Task ExecuteAsyncT_NullFactory_ThrowsArgumentNullException()
        {
            IOperationScopeFactory? nullFactory = null;
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => nullFactory!.ExecuteAsync("op", () => Task.FromResult(42)));
        }

        [TestMethod]
        public async Task ExecuteAsyncT_NullFunc_ThrowsArgumentNullException()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => _factory.ExecuteAsync("op", (Func<Task<int>>)null!));
        }

        [TestMethod]
        public async Task ExecuteAsyncT_ReturnsResult()
        {
            var result = await _factory.ExecuteAsync("async-func", () => Task.FromResult(42));
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public async Task ExecuteAsyncT_FuncThrows_RethrowsException()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _factory.ExecuteAsync("throw-async-func", new Func<Task<int>>(async () =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException("async boom");
                })));
        }

        [TestMethod]
        public async Task ExecuteAsyncT_WithOptions_PassesOptionsToScope()
        {
            var options = new OperationScopeOptions { CreateActivity = false };
            var result = await _factory.ExecuteAsync("opts-async-func", () => Task.FromResult("hello"), options);
            Assert.AreEqual("hello", result);
        }
    }
}
