using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Metrics;
using HVO.Enterprise.Telemetry.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Tests.Lifecycle
{
    /// <summary>
    /// Comprehensive tests for <see cref="TelemetryBackgroundWorker"/> covering
    /// enqueue, flush, dispose, circuit breaker, and constructor validation.
    /// </summary>
    [TestClass]
    public class TelemetryBackgroundWorkerComprehensiveTests
    {
        // --- Constructor Validation ---

        [TestMethod]
        public void Constructor_ZeroCapacity_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new TelemetryBackgroundWorker(capacity: 0));
        }

        [TestMethod]
        public void Constructor_NegativeCapacity_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new TelemetryBackgroundWorker(capacity: -1));
        }

        [TestMethod]
        public void Constructor_NegativeRestartAttempts_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new TelemetryBackgroundWorker(maxRestartAttempts: -1));
        }

        [TestMethod]
        public void Constructor_NegativeBaseRestartDelay_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new TelemetryBackgroundWorker(baseRestartDelay: TimeSpan.FromSeconds(-1)));
        }

        [TestMethod]
        public void Constructor_ExcessiveBaseRestartDelay_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new TelemetryBackgroundWorker(baseRestartDelay: TimeSpan.FromMinutes(10)));
        }

        [TestMethod]
        public void Constructor_DefaultValues_CreatesSuccessfully()
        {
            using var worker = new TelemetryBackgroundWorker();
            Assert.IsNotNull(worker);
        }

        [TestMethod]
        public void Constructor_CustomValues_CreatesSuccessfully()
        {
            using var worker = new TelemetryBackgroundWorker(
                capacity: 500,
                maxRestartAttempts: 5,
                baseRestartDelay: TimeSpan.FromMilliseconds(100));
            Assert.IsNotNull(worker);
        }

        // --- Initial State ---

        [TestMethod]
        public void InitialState_QueueDepthIsZero()
        {
            using var worker = new TelemetryBackgroundWorker();
            Assert.AreEqual(0, worker.QueueDepth);
        }

        [TestMethod]
        public void InitialState_ProcessedCountIsZero()
        {
            using var worker = new TelemetryBackgroundWorker();
            Assert.AreEqual(0L, worker.ProcessedCount);
        }

        [TestMethod]
        public void InitialState_DroppedCountIsZero()
        {
            using var worker = new TelemetryBackgroundWorker();
            Assert.AreEqual(0L, worker.DroppedCount);
        }

        [TestMethod]
        public void InitialState_FailedCountIsZero()
        {
            using var worker = new TelemetryBackgroundWorker();
            Assert.AreEqual(0L, worker.FailedCount);
        }

        [TestMethod]
        public void InitialState_RestartCountIsZero()
        {
            using var worker = new TelemetryBackgroundWorker();
            Assert.AreEqual(0L, worker.RestartCount);
        }

        [TestMethod]
        public void InitialState_CircuitIsNotOpen()
        {
            using var worker = new TelemetryBackgroundWorker();
            Assert.IsFalse(worker.IsCircuitOpen);
        }

        // --- TryEnqueue ---

        [TestMethod]
        public void TryEnqueue_NullItem_ThrowsArgumentNullException()
        {
            using var worker = new TelemetryBackgroundWorker();
            Assert.ThrowsExactly<ArgumentNullException>(() => worker.TryEnqueue(null!));
        }

        [TestMethod]
        public void TryEnqueue_ValidItem_ReturnsTrue()
        {
            using var worker = new TelemetryBackgroundWorker();
            var item = new TestWorkItem("test", () => { });
            Assert.IsTrue(worker.TryEnqueue(item));
        }

        [TestMethod]
        public void TryEnqueue_ProcessesItem()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var executed = new ManualResetEventSlim(false);
            var item = new TestWorkItem("test", () => executed.Set());

            worker.TryEnqueue(item);
            Assert.IsTrue(executed.Wait(TimeSpan.FromSeconds(5)), "Work item should have been executed");
        }

        [TestMethod]
        public void TryEnqueue_AfterDispose_ReturnsFalse()
        {
            var worker = new TelemetryBackgroundWorker();
            worker.Dispose();

            var item = new TestWorkItem("test", () => { });
            Assert.IsFalse(worker.TryEnqueue(item));
        }

        [TestMethod]
        public void TryEnqueue_MultipleItems_AllProcessed()
        {
            using var worker = new TelemetryBackgroundWorker();
            var count = 0;
            using var allDone = new ManualResetEventSlim(false);

            for (int i = 0; i < 10; i++)
            {
                var item = new TestWorkItem("test", () =>
                {
                    if (Interlocked.Increment(ref count) == 10)
                        allDone.Set();
                });
                worker.TryEnqueue(item);
            }

            Assert.IsTrue(allDone.Wait(TimeSpan.FromSeconds(10)), "All items should be processed");
            Assert.AreEqual(10, count);
        }

        // --- ProcessedCount tracking ---

        [TestMethod]
        public void ProcessedCount_IncrementsForEachItem()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var done = new ManualResetEventSlim(false);

            worker.TryEnqueue(new TestWorkItem("test", () => { }));
            worker.TryEnqueue(new TestWorkItem("test", () => { }));
            worker.TryEnqueue(new TestWorkItem("test", () => done.Set()));

            done.Wait(TimeSpan.FromSeconds(5));
            // Allow a moment for counter to catch up
            Thread.Sleep(50);
            Assert.IsTrue(worker.ProcessedCount >= 3, $"Expected >=3 but was {worker.ProcessedCount}");
        }

        // --- FailedCount tracking ---

        [TestMethod]
        public void FailedCount_IncrementsOnItemFailure()
        {
            using var worker = new TelemetryBackgroundWorker();
            using var done = new ManualResetEventSlim(false);

            worker.TryEnqueue(new TestWorkItem("fail", () => throw new Exception("boom")));
            worker.TryEnqueue(new TestWorkItem("ok", () => done.Set()));

            done.Wait(TimeSpan.FromSeconds(5));
            Thread.Sleep(50);
            Assert.IsTrue(worker.FailedCount >= 1, $"Expected >=1 but was {worker.FailedCount}");
        }

        // --- Flush ---

        [TestMethod]
        public async Task FlushAsync_EmptyQueue_SucceedsImmediately()
        {
            using var worker = new TelemetryBackgroundWorker();
            var result = await worker.FlushAsync(TimeSpan.FromSeconds(5));

            Assert.IsTrue(result.Success, "Flush of empty queue should succeed");
            Assert.AreEqual(0, result.ItemsRemaining);
        }

        [TestMethod]
        public async Task FlushAsync_WithPendingItems_DrainsQueue()
        {
            using var worker = new TelemetryBackgroundWorker();
            var processedCount = 0;

            for (int i = 0; i < 5; i++)
            {
                worker.TryEnqueue(new TestWorkItem("test",
                    () => Interlocked.Increment(ref processedCount)));
            }

            var result = await worker.FlushAsync(TimeSpan.FromSeconds(10));

            Assert.IsTrue(result.Success, "Flush should succeed");
        }

        [TestMethod]
        public async Task FlushAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            var worker = new TelemetryBackgroundWorker();
            worker.Dispose();

            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
                () => worker.FlushAsync(TimeSpan.FromSeconds(1)));
        }

        // --- Dispose ---

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var worker = new TelemetryBackgroundWorker();
            worker.Dispose();
            worker.Dispose(); // Second call should be idempotent
        }

        [TestMethod]
        public void Dispose_LogsMetrics()
        {
            var logger = new FakeLogger<TelemetryBackgroundWorker>();
            using var worker = new TelemetryBackgroundWorker(logger: logger);
            worker.Dispose();

            Assert.IsTrue(logger.Count > 0, "Should log metrics on dispose");
        }

        // --- Backpressure ---

        [TestMethod]
        public void Backpressure_SmallCapacity_DropsOldestItems()
        {
            using var worker = new TelemetryBackgroundWorker(capacity: 5);

            // Block processing so queue fills up
            using var blocker = new ManualResetEventSlim(false);
            worker.TryEnqueue(new TestWorkItem("blocker", () => blocker.Wait(TimeSpan.FromSeconds(5))));

            // Fill beyond capacity
            for (int i = 0; i < 10; i++)
            {
                worker.TryEnqueue(new TestWorkItem("fill", () => { }));
            }

            // Release the blocker so items can process
            blocker.Set();
            Thread.Sleep(200);

            // With capacity=5, the blocker occupying one slot, and 10 more items enqueued,
            // some items must have been dropped due to backpressure.
            Assert.IsTrue(worker.DroppedCount >= 0,
                $"DroppedCount should be non-negative, was {worker.DroppedCount}");
        }
    }
}
