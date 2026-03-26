using System;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.OperationScopes
{
    [TestClass]
    public class OperationScopeExtensionsTests
    {
        [TestMethod]
        public void Execute_RunsAction()
        {
            var factory = CreateFactory();
            var called = false;

            factory.Execute("Test", () => called = true);

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Execute_ThrowsOnFailure()
        {
            var factory = CreateFactory();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                factory.Execute("Test", () => throw new InvalidOperationException("boom")));
        }

        [TestMethod]
        public async Task ExecuteAsync_RunsAction()
        {
            var factory = CreateFactory();
            var called = false;

            await factory.ExecuteAsync("Test", () =>
            {
                called = true;
                return Task.CompletedTask;
            });

            Assert.IsTrue(called);
        }

        [TestMethod]
        public async Task ExecuteAsync_ThrowsOnFailure()
        {
            var factory = CreateFactory();

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                factory.ExecuteAsync("Test", () => throw new InvalidOperationException("boom")));
        }

        [TestMethod]
        public async Task ExecuteAsyncGeneric_ReturnsResult()
        {
            var factory = CreateFactory();

            var result = await factory.ExecuteAsync("Test", () => Task.FromResult(42));

            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public async Task ExecuteAsyncGeneric_ThrowsOnFailure()
        {
            var factory = CreateFactory();

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                factory.ExecuteAsync<int>("Test", () => throw new InvalidOperationException("boom")));
        }

        private static OperationScopeFactory CreateFactory()
        {
            var sourceName = "HVO.Enterprise.Telemetry.Tests." + Guid.NewGuid().ToString("N");
            return new OperationScopeFactory(sourceName, "1.0.0");
        }
    }
}
