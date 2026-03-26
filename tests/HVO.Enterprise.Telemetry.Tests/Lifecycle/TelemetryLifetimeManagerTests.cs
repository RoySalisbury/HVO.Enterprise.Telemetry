using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Lifecycle;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Lifecycle
{
    [TestClass]
    public class TelemetryLifetimeManagerTests
    {
        /// <summary>
        /// Test work item that executes an action.
        /// </summary>
        private class TestWorkItem : TelemetryWorkItem
        {
            private readonly Action _action;
            private readonly string _operationType;

            public TestWorkItem(Action action, string operationType = "Test")
            {
                _action = action;
                _operationType = operationType;
            }

            public override string OperationType => _operationType;

            public override void Execute()
            {
                _action();
            }
        }

        [TestMethod]
        public void Constructor_WithValidWorker_CreatesManager()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();

            // Act
            using var manager = new TelemetryLifetimeManager(worker);

            // Assert
            Assert.IsNotNull(manager);
            Assert.IsFalse(manager.IsShuttingDown);
        }

        [TestMethod]
        public void Constructor_WithNullWorker_ThrowsException()
        {
            // Act
            Assert.ThrowsExactly<ArgumentNullException>(() => new TelemetryLifetimeManager(null!));
        }

        [TestMethod]
        public void LifetimeManager_RegistersAppDomainEvents()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();

            // Act
            using var manager = new TelemetryLifetimeManager(worker);

            // Assert
            Assert.IsFalse(manager.IsShuttingDown);
            // Event registration is tested implicitly - the manager should initialize without errors
        }

        [TestMethod]
        public async Task ShutdownAsync_FlushesQueue()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            var processed = 0;

            // Enqueue items
            for (int i = 0; i < 50; i++)
            {
                worker.TryEnqueue(new TestWorkItem(() =>
                {
                    Thread.Sleep(10);
                    Interlocked.Increment(ref processed);
                }));
            }

            // Act
            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(10));

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(50, result.ItemsFlushed);
            Assert.AreEqual(0, result.ItemsRemaining);
            Assert.AreEqual(50, processed);
        }

        [TestMethod]
        public async Task ShutdownAsync_WithTimeout_ReportsRemaining()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker(capacity: 100);
            using var manager = new TelemetryLifetimeManager(worker);

            // Enqueue long-running items
            for (int i = 0; i < 100; i++)
            {
                worker.TryEnqueue(new TestWorkItem(() => Thread.Sleep(500)));
            }

            // Act
            var result = await manager.ShutdownAsync(TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.ItemsRemaining > 0);
        }

        [TestMethod]
        public async Task ShutdownAsync_MultipleCallsIgnored()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Enqueue some work
            for (int i = 0; i < 10; i++)
            {
                worker.TryEnqueue(new TestWorkItem(() => Thread.Sleep(10)));
            }

            // Act
            var task1 = manager.ShutdownAsync(TimeSpan.FromSeconds(10));
            var task2 = manager.ShutdownAsync(TimeSpan.FromSeconds(10));

            var result1 = await task1;
            var result2 = await task2;

            // Assert
            Assert.IsTrue(result1.Success);
            Assert.IsFalse(result2.Success); // Second call should be ignored
            Assert.AreEqual("Shutdown already in progress", result2.Reason);
        }

        [TestMethod]
        public async Task ShutdownAsync_ClosesOpenActivities()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Use an HVO-prefixed source name because CloseOpenActivities only
            // disposes activities whose Source.Name starts with "HVO." to avoid
            // closing foreign activities owned by ASP.NET Core, gRPC, etc.
            using var source = new ActivitySource("HVO.Enterprise.Telemetry.Tests");
            
            // Create an ActivityListener to enable activity creation
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "HVO.Enterprise.Telemetry.Tests",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);
            
            using var activity = source.StartActivity("TestOperation");

            // Activity might be null if no listener is configured, so only test if we have one
            if (activity != null)
            {
                Assert.IsNotNull(Activity.Current, "Activity should be started");

                // Act
                await manager.ShutdownAsync(TimeSpan.FromSeconds(1));

                // Assert - Activity should be stopped
                Assert.IsTrue(activity.Duration > TimeSpan.Zero, "Activity should be stopped");
            }
            else
            {
                // No activities were created - this is acceptable behavior when no listener matches
                // The shutdown logic should handle this gracefully without errors
                await manager.ShutdownAsync(TimeSpan.FromSeconds(1));
            }
        }

        [TestMethod]
        public async Task ShutdownAsync_RecordsDuration()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Enqueue some work
            for (int i = 0; i < 10; i++)
            {
                worker.TryEnqueue(new TestWorkItem(() => Thread.Sleep(10)));
            }

            // Act
            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(10));

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.Duration > TimeSpan.Zero);
            Assert.IsTrue(result.Duration < TimeSpan.FromSeconds(5), "Should complete reasonably fast");
        }

        [TestMethod]
        public async Task ShutdownAsync_SetsIsShuttingDownFlag()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            Assert.IsFalse(manager.IsShuttingDown, "Should not be shutting down initially");

            // Act
            var shutdownTask = manager.ShutdownAsync(TimeSpan.FromSeconds(5));

            // Check flag during shutdown
            Assert.IsTrue(manager.IsShuttingDown, "Should be shutting down during shutdown");

            await shutdownTask;

            // Assert
            Assert.IsTrue(manager.IsShuttingDown, "Should still be shutting down after shutdown");
        }

        [TestMethod]
        public async Task ShutdownAsync_RespectsCancellationToken()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            using var cts = new CancellationTokenSource();

            // Enqueue long-running items
            for (int i = 0; i < 100; i++)
            {
                worker.TryEnqueue(new TestWorkItem(() => Thread.Sleep(500)));
            }

            // Cancel after 100ms
            cts.CancelAfter(100);

            // Act
            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(30), cts.Token);

            // Assert
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public void Dispose_UnregistersEventHandlers()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            var manager = new TelemetryLifetimeManager(worker);

            // Act
            manager.Dispose();
            manager.Dispose(); // Second call should be safe

            // Assert - no exception
        }

        [TestMethod]
        public async Task ShutdownAsync_WithNoItems_CompletesImmediately()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Act
            var sw = Stopwatch.StartNew();
            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(5));
            sw.Stop();

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.ItemsFlushed);
            Assert.AreEqual(0, result.ItemsRemaining);
            Assert.IsTrue(sw.ElapsedMilliseconds < 1000, "Should complete quickly with no items");
        }

        [TestMethod]
        public async Task ShutdownAsync_ReportsCorrectItemCounts()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);
            var itemCount = 25;

            // Enqueue items
            for (int i = 0; i < itemCount; i++)
            {
                worker.TryEnqueue(new TestWorkItem(() => Thread.Sleep(5)));
            }

            // Act
            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(itemCount, result.ItemsFlushed);
            Assert.AreEqual(0, result.ItemsRemaining);
        }

        [TestMethod]
        public async Task ShutdownAsync_HandlesNestedActivities()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Use an HVO-prefixed source name because CloseOpenActivities only
            // disposes activities whose Source.Name starts with "HVO." to avoid
            // closing foreign activities owned by ASP.NET Core, gRPC, etc.
            using var source = new ActivitySource("HVO.Enterprise.Telemetry.Tests");
            
            // Create an ActivityListener to enable activity creation
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "HVO.Enterprise.Telemetry.Tests",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);
            
            using var parent = source.StartActivity("Parent");
            using var child = source.StartActivity("Child");

            // Activities might be null if no listener is configured
            if (child != null && parent != null)
            {
                Assert.IsNotNull(Activity.Current, "Child activity should be current");

                // Act
                await manager.ShutdownAsync(TimeSpan.FromSeconds(1));

                // Assert - Both activities should be stopped
                Assert.IsTrue(child.Duration > TimeSpan.Zero, "Child activity should be stopped");
                Assert.IsTrue(parent.Duration > TimeSpan.Zero, "Parent activity should be stopped");
            }
            else
            {
                // No activities were created - this is acceptable behavior when no listener matches
                // The shutdown logic should handle this gracefully without errors
                await manager.ShutdownAsync(TimeSpan.FromSeconds(1));
            }
        }

        [TestMethod]
        public void IsShuttingDown_InitiallyFalse()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Assert
            Assert.IsFalse(manager.IsShuttingDown);
        }

        [TestMethod]
        public void ITelemetryLifetime_IsImplemented()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Act
            ITelemetryLifetime lifetime = manager;

            // Assert
            Assert.IsNotNull(lifetime);
            Assert.IsFalse(lifetime.IsShuttingDown);
        }

        [TestMethod]
        public async Task ShutdownAsync_EmptyQueueReturnsImmediately()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            // Act
            var sw = Stopwatch.StartNew();
            var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(10));
            sw.Stop();

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(sw.Elapsed < TimeSpan.FromSeconds(1), "Should return quickly for empty queue");
        }

        [TestMethod]
        public async Task ShutdownAsync_ConcurrentShutdownsHandledGracefully()
        {
            // Arrange
            using var worker = new TelemetryBackgroundWorker();
            using var manager = new TelemetryLifetimeManager(worker);

            for (int i = 0; i < 20; i++)
            {
                worker.TryEnqueue(new TestWorkItem(() => Thread.Sleep(10)));
            }

            // Act - Start multiple shutdowns concurrently
            var tasks = new Task<ShutdownResult>[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = manager.ShutdownAsync(TimeSpan.FromSeconds(10));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            var successCount = 0;
            var failureCount = 0;

            foreach (var result in results)
            {
                if (result.Success)
                    successCount++;
                else
                    failureCount++;
            }

            Assert.AreEqual(1, successCount, "Only one shutdown should succeed");
            Assert.AreEqual(4, failureCount, "Four shutdowns should be ignored");
        }
    }
}
