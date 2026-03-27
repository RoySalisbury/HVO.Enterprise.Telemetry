using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.IIS;
using HVO.Enterprise.Telemetry.IIS.Configuration;
using HVO.Enterprise.Telemetry.IIS.Tests.Fakes;

namespace HVO.Enterprise.Telemetry.IIS.Tests
{
    /// <summary>
    /// Tests for <see cref="IisShutdownHandler"/> shutdown coordination.
    /// </summary>
    [TestClass]
    public sealed class IisShutdownHandlerTests
    {
        [TestMethod]
        public async Task OnGracefulShutdownAsync_CallsShutdown_OnTelemetryService()
        {
            // Arrange
            var fakeTelemetry = new FakeTelemetryService();
            var handler = new IisShutdownHandler(fakeTelemetry);

            // Act
            await handler.OnGracefulShutdownAsync(CancellationToken.None);

            // Assert
            Assert.IsTrue(fakeTelemetry.ShutdownCalled);
            Assert.AreEqual(1, fakeTelemetry.ShutdownCallCount);
        }

        [TestMethod]
        public async Task OnGracefulShutdownAsync_SetsIsShutdownStarted()
        {
            // Arrange
            var handler = new IisShutdownHandler(null);

            Assert.IsFalse(handler.IsShutdownStarted);

            // Act
            await handler.OnGracefulShutdownAsync(CancellationToken.None);

            // Assert
            Assert.IsTrue(handler.IsShutdownStarted);
        }

        [TestMethod]
        public async Task OnGracefulShutdownAsync_IsIdempotent()
        {
            // Arrange
            var fakeTelemetry = new FakeTelemetryService();
            var handler = new IisShutdownHandler(fakeTelemetry);

            // Act - call twice
            await handler.OnGracefulShutdownAsync(CancellationToken.None);
            await handler.OnGracefulShutdownAsync(CancellationToken.None);

            // Assert - shutdown called only once
            Assert.AreEqual(1, fakeTelemetry.ShutdownCallCount);
        }

        [TestMethod]
        public async Task OnGracefulShutdownAsync_WorksWithNullTelemetryService()
        {
            // Arrange
            var handler = new IisShutdownHandler(null);

            // Act & Assert - should not throw
            await handler.OnGracefulShutdownAsync(CancellationToken.None);
            Assert.IsTrue(handler.IsShutdownStarted);
        }

        [TestMethod]
        public async Task OnGracefulShutdownAsync_InvokesPreShutdownHandler()
        {
            // Arrange
            var preShutdownCalled = false;
            var options = new IisExtensionOptions
            {
                OnPreShutdown = async (ct) =>
                {
                    preShutdownCalled = true;
                    await Task.CompletedTask;
                }
            };
            var handler = new IisShutdownHandler(null, options);

            // Act
            await handler.OnGracefulShutdownAsync(CancellationToken.None);

            // Assert
            Assert.IsTrue(preShutdownCalled);
        }

        [TestMethod]
        public async Task OnGracefulShutdownAsync_InvokesPostShutdownHandler()
        {
            // Arrange
            var postShutdownCalled = false;
            var options = new IisExtensionOptions
            {
                OnPostShutdown = async (ct) =>
                {
                    postShutdownCalled = true;
                    await Task.CompletedTask;
                }
            };
            var handler = new IisShutdownHandler(null, options);

            // Act
            await handler.OnGracefulShutdownAsync(CancellationToken.None);

            // Assert
            Assert.IsTrue(postShutdownCalled);
        }

        [TestMethod]
        public async Task OnGracefulShutdownAsync_InvokesHandlersInCorrectOrder()
        {
            // Arrange
            var order = new System.Collections.Generic.List<string>();
            var fakeTelemetry = new FakeTelemetryService();
            var options = new IisExtensionOptions
            {
                OnPreShutdown = async (ct) =>
                {
                    order.Add("pre");
                    await Task.CompletedTask;
                },
                OnPostShutdown = async (ct) =>
                {
                    order.Add("post");
                    await Task.CompletedTask;
                }
            };
            var handler = new IisShutdownHandler(fakeTelemetry, options);

            // Act
            await handler.OnGracefulShutdownAsync(CancellationToken.None);

            // Assert - order is: pre-shutdown, shutdown, post-shutdown
            Assert.AreEqual(3, order.Count + fakeTelemetry.ShutdownCallCount);
            Assert.AreEqual("pre", order[0]);
            Assert.IsTrue(fakeTelemetry.ShutdownCalled);
            Assert.AreEqual("post", order[1]);
        }

        [TestMethod]
        public async Task OnGracefulShutdownAsync_ContinuesAfterPreShutdownFailure()
        {
            // Arrange
            var fakeTelemetry = new FakeTelemetryService();
            var options = new IisExtensionOptions
            {
                OnPreShutdown = (ct) => throw new InvalidOperationException("Pre-shutdown failed")
            };
            var handler = new IisShutdownHandler(fakeTelemetry, options);

            // Act - should not throw despite pre-shutdown handler failure
            await handler.OnGracefulShutdownAsync(CancellationToken.None);

            // Assert - shutdown still called
            Assert.IsTrue(fakeTelemetry.ShutdownCalled);
        }

        [TestMethod]
        public async Task OnGracefulShutdownAsync_RespectsCancellation()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancel

            var options = new IisExtensionOptions
            {
                OnPreShutdown = async (ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.CompletedTask;
                }
            };
            var handler = new IisShutdownHandler(null, options);

            // Act & Assert
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => handler.OnGracefulShutdownAsync(cts.Token));
        }

        [TestMethod]
        public void OnImmediateShutdown_CallsShutdown_OnTelemetryService()
        {
            // Arrange
            var fakeTelemetry = new FakeTelemetryService();
            var handler = new IisShutdownHandler(fakeTelemetry);

            // Act
            handler.OnImmediateShutdown();

            // Assert
            Assert.IsTrue(fakeTelemetry.ShutdownCalled);
        }

        [TestMethod]
        public void OnImmediateShutdown_SetsIsShutdownStarted()
        {
            // Arrange
            var handler = new IisShutdownHandler(null);

            // Act
            handler.OnImmediateShutdown();

            // Assert
            Assert.IsTrue(handler.IsShutdownStarted);
        }

        [TestMethod]
        public void OnImmediateShutdown_IsIdempotent()
        {
            // Arrange
            var fakeTelemetry = new FakeTelemetryService();
            var handler = new IisShutdownHandler(fakeTelemetry);

            // Act - call twice
            handler.OnImmediateShutdown();
            handler.OnImmediateShutdown();

            // Assert - shutdown called only once
            Assert.AreEqual(1, fakeTelemetry.ShutdownCallCount);
        }

        [TestMethod]
        public void OnImmediateShutdown_WorksWithNullTelemetryService()
        {
            // Arrange
            var handler = new IisShutdownHandler(null);

            // Act & Assert - should not throw
            handler.OnImmediateShutdown();
            Assert.IsTrue(handler.IsShutdownStarted);
        }

        [TestMethod]
        public async Task GracefulThenImmediate_OnlyExecutesOnce()
        {
            // Arrange
            var fakeTelemetry = new FakeTelemetryService();
            var handler = new IisShutdownHandler(fakeTelemetry);

            // Act
            await handler.OnGracefulShutdownAsync(CancellationToken.None);
            handler.OnImmediateShutdown();

            // Assert - shutdown called only once
            Assert.AreEqual(1, fakeTelemetry.ShutdownCallCount);
        }

        [TestMethod]
        public async Task ImmediateThenGraceful_OnlyExecutesOnce()
        {
            // Arrange
            var fakeTelemetry = new FakeTelemetryService();
            var handler = new IisShutdownHandler(fakeTelemetry);

            // Act
            handler.OnImmediateShutdown();
            await handler.OnGracefulShutdownAsync(CancellationToken.None);

            // Assert - shutdown called only once
            Assert.AreEqual(1, fakeTelemetry.ShutdownCallCount);
        }
    }
}
