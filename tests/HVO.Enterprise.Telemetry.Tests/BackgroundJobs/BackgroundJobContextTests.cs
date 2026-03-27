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
    public class BackgroundJobContextTests
    {
        private ActivitySource _activitySource = null!;
        private ActivityListener _activityListener = null!;

        [TestInitialize]
        public void Setup()
        {
            // Set up Activity listening
            _activitySource = new ActivitySource("TestSource", "1.0.0");
            _activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(_activityListener);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _activitySource?.Dispose();
            _activityListener?.Dispose();
        }

        [TestMethod]
        public void Capture_CapturesCurrentCorrelationId()
        {
            // Arrange
            var expectedCorrelationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = expectedCorrelationId;

            // Act
            var context = BackgroundJobContext.Capture();

            // Assert
            Assert.IsNotNull(context);
            Assert.AreEqual(expectedCorrelationId, context.CorrelationId);
        }

        [TestMethod]
        public void Capture_CapturesParentActivity()
        {
            // Arrange
            using var activity = _activitySource.StartActivity("TestActivity");
            Assert.IsNotNull(activity);

            // Act
            var context = BackgroundJobContext.Capture();

            // Assert
            Assert.IsNotNull(context);
            Assert.AreEqual(activity.TraceId.ToString(), context.ParentActivityId);
            Assert.AreEqual(activity.SpanId.ToString(), context.ParentSpanId);
        }

        [TestMethod]
        public void Capture_WithNoActivity_StillCapturesCorrelation()
        {
            // Arrange
            Assert.IsNull(Activity.Current);
            var expectedCorrelationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = expectedCorrelationId;

            // Act
            var context = BackgroundJobContext.Capture();

            // Assert
            Assert.IsNotNull(context);
            Assert.AreEqual(expectedCorrelationId, context.CorrelationId);
            Assert.IsNull(context.ParentActivityId);
            Assert.IsNull(context.ParentSpanId);
        }

        [TestMethod]
        public void Capture_RecordsEnqueueTimestamp()
        {
            // Arrange
            var before = DateTimeOffset.UtcNow;

            // Act
            var context = BackgroundJobContext.Capture();
            var after = DateTimeOffset.UtcNow;

            // Assert
            Assert.IsNotNull(context);
            Assert.IsTrue(context.EnqueuedAt >= before);
            Assert.IsTrue(context.EnqueuedAt <= after);
        }

        [TestMethod]
        public void Capture_WithCustomMetadata_IncludesMetadata()
        {
            // Arrange
            var metadata = new System.Collections.Generic.Dictionary<string, object>
            {
                ["JobType"] = "EmailSender",
                ["Priority"] = 5
            };

            // Act
            var context = BackgroundJobContext.Capture(metadata);

            // Assert
            Assert.IsNotNull(context);
            Assert.IsNotNull(context.CustomMetadata);
            Assert.AreEqual(2, context.CustomMetadata.Count);
            Assert.AreEqual("EmailSender", context.CustomMetadata["JobType"]);
            Assert.AreEqual(5, context.CustomMetadata["Priority"]);
        }

        [TestMethod]
        public void Restore_RestoresCorrelationId()
        {
            // Arrange
            var jobCorrelationId = Guid.NewGuid().ToString("N");
            var differentCorrelationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = differentCorrelationId;

            var context = new BackgroundJobContext
            {
                CorrelationId = jobCorrelationId,
                EnqueuedAt = DateTimeOffset.UtcNow
            };

            // Act
            using (context.Restore())
            {
                // Assert - inside scope
                Assert.AreEqual(jobCorrelationId, CorrelationContext.Current);
            }

            // Assert - after scope disposed
            Assert.AreEqual(differentCorrelationId, CorrelationContext.Current);
        }

        [TestMethod]
        public async Task Restore_FlowsAcrossAsyncBoundaries()
        {
            // Arrange
            var jobCorrelationId = Guid.NewGuid().ToString("N");
            var context = new BackgroundJobContext
            {
                CorrelationId = jobCorrelationId,
                EnqueuedAt = DateTimeOffset.UtcNow
            };

            // Act & Assert
            using (context.Restore())
            {
                Assert.AreEqual(jobCorrelationId, CorrelationContext.Current);

                await Task.Run(() =>
                {
                    // Should have same correlation ID in async context
                    Assert.AreEqual(jobCorrelationId, CorrelationContext.Current);
                });

                Assert.AreEqual(jobCorrelationId, CorrelationContext.Current);
            }
        }

        [TestMethod]
        public void Restore_CreatesActivityWithParentLink()
        {
            // Arrange
            using var parentActivity = _activitySource.StartActivity("ParentActivity");
            Assert.IsNotNull(parentActivity);

            var context = BackgroundJobContext.Capture();
            parentActivity.Stop();

            // Clear current activity
            Assert.IsNull(Activity.Current);

            // Act
            using (context.Restore())
            {
                // Assert
                var currentActivity = Activity.Current;
                Assert.IsNotNull(currentActivity, "Activity should be created during restore");

                // Check parent link
                Assert.AreEqual(parentActivity.TraceId, currentActivity.TraceId, "TraceId should match parent");
            }
        }

        [TestMethod]
        public void FromValues_CreatesValidContext()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString("N");
            var traceId = ActivityTraceId.CreateRandom().ToString();
            var spanId = ActivitySpanId.CreateRandom().ToString();
            var enqueuedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

            // Act
            var context = BackgroundJobContext.FromValues(
                correlationId,
                traceId,
                spanId,
                enqueuedAt);

            // Assert
            Assert.IsNotNull(context);
            Assert.AreEqual(correlationId, context.CorrelationId);
            Assert.AreEqual(traceId, context.ParentActivityId);
            Assert.AreEqual(spanId, context.ParentSpanId);
            Assert.AreEqual(enqueuedAt, context.EnqueuedAt);
        }

        [TestMethod]
        public void FromValues_WithNullCorrelationId_ThrowsException()
        {
            // Act
            Assert.ThrowsExactly<ArgumentNullException>(() => BackgroundJobContext.FromValues(null!));
        }

        // Note: Restore_WithNullContext test removed - calling an instance method on null 
        // always throws NullReferenceException before the method can validate arguments

        [TestMethod]
        public void Restore_MultipleTimes_WorksCorrectly()
        {
            // Arrange
            var jobCorrelationId = Guid.NewGuid().ToString("N");
            var context = new BackgroundJobContext
            {
                CorrelationId = jobCorrelationId,
                EnqueuedAt = DateTimeOffset.UtcNow
            };

            // Act & Assert - First restore
            using (context.Restore())
            {
                Assert.AreEqual(jobCorrelationId, CorrelationContext.Current);
            }

            // Act & Assert - Second restore (context can be reused)
            using (context.Restore())
            {
                Assert.AreEqual(jobCorrelationId, CorrelationContext.Current);
            }
        }

        [TestMethod]
        public void Restore_WithCustomMetadata_AddsTagsToActivity()
        {
            // Arrange
            var metadata = new System.Collections.Generic.Dictionary<string, object>
            {
                ["JobType"] = "EmailSender",
                ["Priority"] = 5
            };

            using var parentActivity = _activitySource.StartActivity("ParentActivity");
            var context = BackgroundJobContext.Capture(metadata);
            parentActivity?.Stop();

            // Act
            using (context.Restore())
            {
                // Assert
                var currentActivity = Activity.Current;
                Assert.IsNotNull(currentActivity);

                // Check for metadata tags
                var found = false;
                foreach (var tag in currentActivity.Tags)
                {
                    if (tag.Key == "job.metadata.JobType" && tag.Value == "EmailSender")
                    {
                        found = true;
                        break;
                    }
                }
                Assert.IsTrue(found, "Custom metadata should be added as Activity tags");
            }
        }
    }
}
