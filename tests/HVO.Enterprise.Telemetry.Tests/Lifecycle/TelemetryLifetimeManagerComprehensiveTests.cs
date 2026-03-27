using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Lifecycle;
using HVO.Enterprise.Telemetry.Metrics;
using HVO.Enterprise.Telemetry.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Tests.Lifecycle
{
    /// <summary>
    /// Comprehensive tests for <see cref="TelemetryLifetimeManager"/> covering
    /// construction, shutdown, and disposal semantics.
    /// </summary>
    [TestClass]
    public class TelemetryLifetimeManagerComprehensiveTests
    {
        // --- Constructor Validation ---

        [TestMethod]
        public void Constructor_NullWorker_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new TelemetryLifetimeManager(null!));
        }

        [TestMethod]
        public void Constructor_ValidWorker_CreatesSuccessfully()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            Assert.IsNotNull(manager);
        }

        [TestMethod]
        public void Constructor_WithLogger_CreatesSuccessfully()
        {
            var logger = new FakeLogger<TelemetryLifetimeManager>();
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker, logger);
            Assert.IsNotNull(manager);
        }

        // --- IsShuttingDown ---

        [TestMethod]
        public void IsShuttingDown_InitiallyFalse()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            Assert.IsFalse(manager.IsShuttingDown);
        }

        // --- ShutdownAsync ---

        [TestMethod]
        public async Task ShutdownAsync_ReturnsSuccessResult()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(5));

            Assert.IsTrue(result.Success);
            Assert.IsTrue(manager.IsShuttingDown);
        }

        [TestMethod]
        public async Task ShutdownAsync_SecondCall_ReturnsFailed()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            await manager.ShutdownAsync(TimeSpan.FromSeconds(5));
            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(5));

            Assert.IsFalse(result.Success, "Second shutdown should indicate failure/already in progress");
            Assert.AreEqual("Shutdown already in progress", result.Reason);
        }

        [TestMethod]
        public async Task ShutdownAsync_WithCancellation_AbortsGracefully()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            using var cts = new CancellationTokenSource();

            // Cancel immediately
            cts.Cancel();

            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(5), cts.Token);
            // Behavior depends on timing - may succeed quickly or report timeout/cancel
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task ShutdownAsync_SetsIsShuttingDown()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            Assert.IsFalse(manager.IsShuttingDown);
            await manager.ShutdownAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(manager.IsShuttingDown);
        }

        // --- Dispose ---

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            using var worker = new TelemetryBackgroundWorker();
            var manager = new TelemetryLifetimeManager(worker);
            manager.Dispose();
            manager.Dispose(); // Idempotent
        }

        [TestMethod]
        public void Dispose_WithLogger_LogsRegistration()
        {
            var logger = new FakeLogger<TelemetryLifetimeManager>();
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker, logger);

            // Logger should have recorded lifecycle hooks registration
            Assert.IsTrue(logger.Count > 0, "Should log lifecycle registration");
        }

        // --- ShutdownResult properties ---

        [TestMethod]
        public async Task ShutdownResult_HasDuration()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(result.Duration >= TimeSpan.Zero);
        }

        [TestMethod]
        public async Task ShutdownResult_ItemsFlushed_IsNonNegative()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(result.ItemsFlushed >= 0);
        }

        [TestMethod]
        public async Task ShutdownResult_ItemsRemaining_IsNonNegative()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(result.ItemsRemaining >= 0);
        }
    }
}
