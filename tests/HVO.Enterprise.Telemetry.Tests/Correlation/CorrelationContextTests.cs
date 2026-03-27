using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Correlation
{
    /// <summary>
    /// Unit tests for CorrelationContext and CorrelationScope classes.
    /// Tests cover AsyncLocal flow, scope management, Activity integration, and auto-generation.
    /// </summary>
    [TestClass]
    public class CorrelationContextTests
    {
        [TestInitialize]
        public void Initialize()
        {
            // Clear any existing correlation context before each test
            CorrelationContext.Clear();

            // Stop any existing Activity to ensure clean state
            Activity.Current?.Stop();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up after each test
            CorrelationContext.Clear();
            Activity.Current?.Stop();
        }

        #region AsyncLocal Flow Tests

        [TestMethod]
        public async Task CorrelationId_FlowsThroughAsyncAwait()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString();
            CorrelationContext.Current = testId;

            // Act & Assert
            await Task.Run(() =>
            {
                // Should have same ID in async context
                Assert.AreEqual(testId, CorrelationContext.Current);
            });
        }

        [TestMethod]
        public async Task CorrelationId_IsolatedBetweenAsyncContexts()
        {
            // Arrange
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            // Act
            var task1 = Task.Run(() =>
            {
                CorrelationContext.Current = id1;
                Thread.Sleep(50);
                Assert.AreEqual(id1, CorrelationContext.Current);
            });

            var task2 = Task.Run(() =>
            {
                CorrelationContext.Current = id2;
                Thread.Sleep(50);
                Assert.AreEqual(id2, CorrelationContext.Current);
            });

            // Assert
            await Task.WhenAll(task1, task2);
        }

        [TestMethod]
        public async Task CorrelationId_FlowsThroughNestedAsyncCalls()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString();
            CorrelationContext.Current = testId;

            // Act & Assert
            await Level1Async(testId);
        }

        private async Task Level1Async(string expectedId)
        {
            Assert.AreEqual(expectedId, CorrelationContext.Current);
            await Level2Async(expectedId);
        }

        private async Task Level2Async(string expectedId)
        {
            Assert.AreEqual(expectedId, CorrelationContext.Current);
            await Task.Delay(10);
            Assert.AreEqual(expectedId, CorrelationContext.Current);
        }

        #endregion

        #region Scope Tests

        [TestMethod]
        public void CorrelationScope_RestoresPreviousIdOnDispose()
        {
            // Arrange
            var originalId = CorrelationContext.Current;
            var scopeId = Guid.NewGuid().ToString();

            // Act
            using (CorrelationContext.BeginScope(scopeId))
            {
                Assert.AreEqual(scopeId, CorrelationContext.Current);
            }

            // Assert
            Assert.AreEqual(originalId, CorrelationContext.Current);
        }

        [TestMethod]
        public void CorrelationScope_SupportsNesting()
        {
            // Arrange
            var id1 = "id1";
            var id2 = "id2";
            var id3 = "id3";

            // Act & Assert
            using (CorrelationContext.BeginScope(id1))
            {
                Assert.AreEqual(id1, CorrelationContext.Current);

                using (CorrelationContext.BeginScope(id2))
                {
                    Assert.AreEqual(id2, CorrelationContext.Current);

                    using (CorrelationContext.BeginScope(id3))
                    {
                        Assert.AreEqual(id3, CorrelationContext.Current);
                    }

                    Assert.AreEqual(id2, CorrelationContext.Current);
                }

                Assert.AreEqual(id1, CorrelationContext.Current);
            }
        }

        [TestMethod]
        public void CorrelationScope_ThrowsOnNullCorrelationId()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                CorrelationContext.BeginScope(null!);
            });
        }

        [TestMethod]
        public void CorrelationScope_ThrowsOnEmptyCorrelationId()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                CorrelationContext.BeginScope(string.Empty);
            });
        }

        [TestMethod]
        public void CorrelationScope_CanBeDisposedMultipleTimes()
        {
            // Arrange
            var scopeId = Guid.NewGuid().ToString();
            var scope = CorrelationContext.BeginScope(scopeId);

            // Act & Assert - should not throw
            scope.Dispose();
            scope.Dispose();
            scope.Dispose();
        }

        [TestMethod]
        public async Task CorrelationScope_WorksAcrossAsyncBoundaries()
        {
            // Arrange
            var scopeId = Guid.NewGuid().ToString();

            // Act & Assert
            using (CorrelationContext.BeginScope(scopeId))
            {
                Assert.AreEqual(scopeId, CorrelationContext.Current);

                await Task.Run(() =>
                {
                    Assert.AreEqual(scopeId, CorrelationContext.Current);
                });

                Assert.AreEqual(scopeId, CorrelationContext.Current);
            }
        }

        #endregion

        #region Auto-Generation Tests

        [TestMethod]
        public void CorrelationId_AutoGeneratesWhenEmpty()
        {
            // Arrange
            CorrelationContext.Clear();

            // Act
            var id1 = CorrelationContext.Current;

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(id1));

            // Should return same ID on subsequent calls
            var id2 = CorrelationContext.Current;
            Assert.AreEqual(id1, id2);
        }

        [TestMethod]
        public void CorrelationId_AutoGeneratedIdIsValidGuid()
        {
            // Arrange
            CorrelationContext.Clear();

            // Act
            var id = CorrelationContext.Current;

            // Assert
            Assert.IsTrue(Guid.TryParse(id, out _), "Auto-generated ID should be a valid Guid");
        }

        [TestMethod]
        public void CorrelationId_AutoGeneratesUniqueIds()
        {
            // Arrange & Act
            CorrelationContext.Clear();
            var id1 = CorrelationContext.Current;

            CorrelationContext.Clear();
            var id2 = CorrelationContext.Current;

            // Assert
            Assert.AreNotEqual(id1, id2, "Each auto-generated ID should be unique");
        }

        #endregion

        #region Activity Integration Tests

        [TestMethod]
        public void CorrelationId_UsesActivityTraceIdAsFallback()
        {
            // Arrange
            CorrelationContext.Clear();
            using var activitySource = new ActivitySource("TestSource");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "TestSource",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            // Act
            using var activity = activitySource.StartActivity("TestOp");
            Assert.IsNotNull(activity, "Activity should be created");

            var correlationId = CorrelationContext.Current;

            // Assert
            Assert.AreEqual(activity.TraceId.ToString(), correlationId);
        }

        [TestMethod]
        public void CorrelationId_PrefersAsyncLocalOverActivity()
        {
            // Arrange
            var explicitId = "explicit-correlation-id";
            using var activitySource = new ActivitySource("TestSource");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "TestSource",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("TestOp");
            Assert.IsNotNull(activity, "Activity should be created");

            // Act
            CorrelationContext.Current = explicitId;

            // Assert
            Assert.AreEqual(explicitId, CorrelationContext.Current);
            Assert.AreNotEqual(activity.TraceId.ToString(), CorrelationContext.Current);
        }

        [TestMethod]
        public void CorrelationId_FallsBackToGuidWhenNoActivity()
        {
            // Arrange
            CorrelationContext.Clear();

            // Ensure no Activity is current
            Activity.Current?.Stop();

            // Act
            var correlationId = CorrelationContext.Current;

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(correlationId));
            Assert.IsTrue(Guid.TryParse(correlationId, out _), "Should be a valid Guid");
        }

        #endregion

        #region Set/Get Tests

        [TestMethod]
        public void CorrelationId_CanBeSetAndRetrieved()
        {
            // Arrange
            var testId = "test-correlation-id-123";

            // Act
            CorrelationContext.Current = testId;

            // Assert
            Assert.AreEqual(testId, CorrelationContext.Current);
        }

        [TestMethod]
        public void CorrelationId_CanBeOverwritten()
        {
            // Arrange
            CorrelationContext.Current = "first-id";

            // Act
            CorrelationContext.Current = "second-id";

            // Assert
            Assert.AreEqual("second-id", CorrelationContext.Current);
        }

        [TestMethod]
        public void CorrelationId_CanBeSetToNull()
        {
            // Arrange
            CorrelationContext.Current = "test-id";

            // Act
            CorrelationContext.Clear();
            var newId = CorrelationContext.Current;

            // Assert - should auto-generate new ID
            Assert.IsNotNull(newId);
            Assert.AreNotEqual("test-id", newId);
        }

        #endregion

        #region Thread Safety Tests

        [TestMethod]
        public void CorrelationId_IsThreadSafe()
        {
            // Arrange
            var iterations = 100;
            var threads = 10;
            var tasks = new Task[threads];
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            for (int i = 0; i < threads; i++)
            {
                var threadIndex = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < iterations; j++)
                        {
                            var id = $"thread-{threadIndex}-iteration-{j}";
                            CorrelationContext.Current = id;
                            Thread.Sleep(1);
                            Assert.AreEqual(id, CorrelationContext.Current);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert
            Assert.AreEqual(0, exceptions.Count, $"Should be thread-safe, but got {exceptions.Count} exceptions");
        }

        #endregion
    }
}
