using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.BackgroundJobs;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.BackgroundJobs
{
    [TestClass]
    public class BackgroundJobExtensionsTests
    {
        [TestMethod]
        public void EnqueueWithContext_CapturesAndRestoresContext()
        {
            // Arrange
            var originalCorrelationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = originalCorrelationId;

            var executedCorrelationId = string.Empty;
            var completedEvent = new ManualResetEventSlim(false);

            // Act
            BackgroundJobExtensions.EnqueueWithContext(() =>
            {
                executedCorrelationId = CorrelationContext.Current;
                completedEvent.Set();
            });

            // Wait for background job to complete
            var completed = completedEvent.Wait(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(completed, "Background job should complete within timeout");
            Assert.AreEqual(originalCorrelationId, executedCorrelationId,
                "Correlation ID should be preserved in background job");
        }

        [TestMethod]
        public async Task EnqueueWithContextAsync_CapturesAndRestoresContext()
        {
            // Arrange
            var originalCorrelationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = originalCorrelationId;

            var executedCorrelationId = string.Empty;

            // Act
            await BackgroundJobExtensions.EnqueueWithContextAsync(async () =>
            {
                await Task.Delay(10); // Simulate async work
                executedCorrelationId = CorrelationContext.Current;
            });

            // Assert
            Assert.AreEqual(originalCorrelationId, executedCorrelationId,
                "Correlation ID should be preserved in async background job");
        }

        [TestMethod]
        public async Task EnqueueWithContextAsync_WithResult_ReturnsValue()
        {
            // Arrange
            var originalCorrelationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = originalCorrelationId;

            var expectedResult = 42;
            var executedCorrelationId = string.Empty;

            // Act
            var result = await BackgroundJobExtensions.EnqueueWithContextAsync(async () =>
            {
                await Task.Delay(10);
                executedCorrelationId = CorrelationContext.Current;
                return expectedResult;
            });

            // Assert
            Assert.AreEqual(expectedResult, result, "Result should be returned correctly");
            Assert.AreEqual(originalCorrelationId, executedCorrelationId,
                "Correlation ID should be preserved");
        }

        [TestMethod]
        public void EnqueueWithContext_IsolatesFromCallingContext()
        {
            // Arrange
            var jobCorrelationId = Guid.NewGuid().ToString("N");
            var newCorrelationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = jobCorrelationId;

            var completedEvent = new ManualResetEventSlim(false);

            // Act
            BackgroundJobExtensions.EnqueueWithContext(() =>
            {
                Thread.Sleep(50); // Ensure this runs after we change the calling context
                completedEvent.Set();
            });

            // Change calling context immediately
            CorrelationContext.Current = newCorrelationId;

            // Wait for completion
            completedEvent.Wait(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(newCorrelationId, CorrelationContext.Current,
                "Calling context should not be affected by background job");
        }

        [TestMethod]
        public void EnqueueWithContext_WithNullAction_ThrowsException()
        {
            // Act
            Assert.ThrowsExactly<ArgumentNullException>(() => BackgroundJobExtensions.EnqueueWithContext(null!));
        }

        [TestMethod]
        public async Task EnqueueWithContextAsync_WithNullAction_ThrowsException()
        {
            // Act
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await BackgroundJobExtensions.EnqueueWithContextAsync((Func<Task>)null!));
        }

        [TestMethod]
        public async Task EnqueueWithContextAsyncGeneric_WithNullFunc_ThrowsException()
        {
            // Act
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await BackgroundJobExtensions.EnqueueWithContextAsync((Func<Task<int>>)null!));
        }

        // Note: Exception propagation test removed - unhandled exceptions in ThreadPool.QueueUserWorkItem
        // crash the test host, which is expected behavior. Background job frameworks (Hangfire, etc.) 
        // handle exceptions via their own mechanisms.

        [TestMethod]
        public async Task EnqueueWithContextAsync_FlowsAcrossNestedAsync()
        {
            // Arrange
            var originalCorrelationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = originalCorrelationId;

            var level1CorrelationId = string.Empty;
            var level2CorrelationId = string.Empty;

            // Act
            await BackgroundJobExtensions.EnqueueWithContextAsync(async () =>
            {
                level1CorrelationId = CorrelationContext.Current;

                await Task.Run(async () =>
                {
                    await Task.Delay(10);
                    level2CorrelationId = CorrelationContext.Current;
                });
            });

            // Assert
            Assert.AreEqual(originalCorrelationId, level1CorrelationId,
                "Level 1 should have original correlation ID");
            Assert.AreEqual(originalCorrelationId, level2CorrelationId,
                "Level 2 should have original correlation ID");
        }
    }
}
