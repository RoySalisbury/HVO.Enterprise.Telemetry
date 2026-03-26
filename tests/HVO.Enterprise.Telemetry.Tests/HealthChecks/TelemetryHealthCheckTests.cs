using System;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HVO.Enterprise.Telemetry.Tests.HealthChecks
{
    [TestClass]
    public class TelemetryHealthCheckTests
    {
        [TestMethod]
        public async Task CheckHealth_AllGood_ReturnsHealthy()
        {
            var stats = new TelemetryStatistics();
            stats.UpdateQueueDepth(10);
            var healthCheck = new TelemetryHealthCheck(stats);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Healthy, result.Status);
            Assert.IsNotNull(result.Description);
            Assert.IsTrue(result.Description!.Contains("Healthy"));
        }

        [TestMethod]
        public async Task CheckHealth_HighErrorRate_ReturnsUnhealthy()
        {
            var stats = new TelemetryStatistics();
            // Pump many errors within the rolling window
            for (int i = 0; i < 700; i++)
            {
                stats.IncrementExceptionsTracked();
            }

            var options = new TelemetryHealthCheckOptions
            {
                UnhealthyErrorRateThreshold = 10.0
            };
            var healthCheck = new TelemetryHealthCheck(stats, options);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description!.Contains("Error rate"));
        }

        [TestMethod]
        public async Task CheckHealth_MediumErrorRate_ReturnsDegraded()
        {
            var stats = new TelemetryStatistics();
            // Pump moderate errors (between degraded=1 and unhealthy=10 per sec over 60s)
            for (int i = 0; i < 120; i++)
            {
                stats.IncrementExceptionsTracked();
            }

            var options = new TelemetryHealthCheckOptions
            {
                DegradedErrorRateThreshold = 1.0,
                UnhealthyErrorRateThreshold = 10.0
            };
            var healthCheck = new TelemetryHealthCheck(stats, options);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Degraded, result.Status);
        }

        [TestMethod]
        public async Task CheckHealth_HighQueueDepth_ReturnsUnhealthy()
        {
            var stats = new TelemetryStatistics();
            stats.UpdateQueueDepth(980);

            var options = new TelemetryHealthCheckOptions
            {
                MaxExpectedQueueDepth = 1000,
                UnhealthyQueueDepthPercent = 95.0
            };
            var healthCheck = new TelemetryHealthCheck(stats, options);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description!.Contains("Queue depth"));
        }

        [TestMethod]
        public async Task CheckHealth_ModerateQueueDepth_ReturnsDegraded()
        {
            var stats = new TelemetryStatistics();
            stats.UpdateQueueDepth(800);

            var options = new TelemetryHealthCheckOptions
            {
                MaxExpectedQueueDepth = 1000,
                DegradedQueueDepthPercent = 75.0,
                UnhealthyQueueDepthPercent = 95.0
            };
            var healthCheck = new TelemetryHealthCheck(stats, options);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Degraded, result.Status);
        }

        [TestMethod]
        public async Task CheckHealth_HighDropRate_ReturnsUnhealthy()
        {
            var stats = new TelemetryStatistics();
            // 2% drop rate (unhealthy threshold = 1%)
            for (int i = 0; i < 100; i++)
            {
                stats.IncrementItemsEnqueued();
            }
            for (int i = 0; i < 2; i++)
            {
                stats.IncrementItemsDropped();
            }

            var options = new TelemetryHealthCheckOptions
            {
                DegradedDropRatePercent = 0.1,
                UnhealthyDropRatePercent = 1.0
            };
            var healthCheck = new TelemetryHealthCheck(stats, options);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description!.Contains("Items dropped"));
        }

        [TestMethod]
        public async Task CheckHealth_IncludesDataDictionary()
        {
            var stats = new TelemetryStatistics();
            stats.UpdateQueueDepth(10);
            var healthCheck = new TelemetryHealthCheck(stats);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.IsNotNull(result.Data);
            Assert.IsTrue(result.Data.ContainsKey("uptime"));
            Assert.IsTrue(result.Data.ContainsKey("activitiesCreated"));
            Assert.IsTrue(result.Data.ContainsKey("activitiesActive"));
            Assert.IsTrue(result.Data.ContainsKey("queueDepth"));
            Assert.IsTrue(result.Data.ContainsKey("maxQueueDepth"));
            Assert.IsTrue(result.Data.ContainsKey("itemsDropped"));
            Assert.IsTrue(result.Data.ContainsKey("errorRate"));
            Assert.IsTrue(result.Data.ContainsKey("throughput"));
            Assert.IsTrue(result.Data.ContainsKey("processingErrors"));
        }

        [TestMethod]
        public async Task CheckHealth_HealthyDescription_IncludesUptimeAndThroughput()
        {
            var stats = new TelemetryStatistics();
            var healthCheck = new TelemetryHealthCheck(stats);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Healthy, result.Status);
            Assert.IsTrue(result.Description!.Contains("Uptime:"));
            Assert.IsTrue(result.Description!.Contains("Throughput:"));
        }

        [TestMethod]
        public void Constructor_NullStatistics_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new TelemetryHealthCheck(null!));
        }

        [TestMethod]
        public void Constructor_InvalidOptions_Throws()
        {
            var stats = new TelemetryStatistics();
            var badOptions = new TelemetryHealthCheckOptions
            {
                DegradedErrorRateThreshold = -1.0
            };

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new TelemetryHealthCheck(stats, badOptions));
        }

        [TestMethod]
        public void Constructor_DefensivelyCopiesOptions()
        {
            var stats = new TelemetryStatistics();
            var options = new TelemetryHealthCheckOptions
            {
                DegradedErrorRateThreshold = 5.0
            };
            var healthCheck = new TelemetryHealthCheck(stats, options);

            // Mutate original options after construction
            options.DegradedErrorRateThreshold = 0.0;

            // Health check should still use the original copied value (5.0),
            // so a rate of 2.0 should be healthy (below 5.0)
            for (int i = 0; i < 120; i++)
            {
                stats.IncrementExceptionsTracked();
            }
            // Rate is ~2.0/sec (120 events / 60s window)
            var result = healthCheck.CheckHealthAsync(CreateContext()).Result;
            // With the copied threshold of 5.0, 2.0/sec should be healthy
            Assert.AreEqual(HealthStatus.Healthy, result.Status);
        }

        [TestMethod]
        public async Task CheckHealth_DefaultOptions_WorksCorrectly()
        {
            var stats = new TelemetryStatistics();
            var healthCheck = new TelemetryHealthCheck(stats);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Healthy, result.Status);
        }

        [TestMethod]
        public async Task CheckHealth_NoDropsNoEnqueues_Healthy()
        {
            var stats = new TelemetryStatistics();
            var healthCheck = new TelemetryHealthCheck(stats);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Healthy, result.Status);
        }

        [TestMethod]
        public async Task CheckHealth_DropsButNoEnqueue_Healthy()
        {
            // Edge case: dropped count > 0 but enqueued = 0 (shouldn't happen but be safe)
            var stats = new TelemetryStatistics();
            stats.IncrementItemsDropped();

            var healthCheck = new TelemetryHealthCheck(stats);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            // Should not crash on division by zero
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task CheckHealth_ErrorRateCheckedBeforeQueueDepth()
        {
            // High error rate AND high queue depth — error rate should be checked first
            var stats = new TelemetryStatistics();
            stats.UpdateQueueDepth(960);
            for (int i = 0; i < 700; i++)
            {
                stats.IncrementExceptionsTracked();
            }

            var options = new TelemetryHealthCheckOptions
            {
                MaxExpectedQueueDepth = 1000,
                UnhealthyErrorRateThreshold = 10.0,
                UnhealthyQueueDepthPercent = 95.0
            };
            var healthCheck = new TelemetryHealthCheck(stats, options);

            var result = await healthCheck.CheckHealthAsync(CreateContext());

            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            // Description should mention error rate since it's checked first
            Assert.IsTrue(result.Description!.Contains("Error rate"));
        }

        private static HealthCheckContext CreateContext()
        {
            return new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "telemetry",
                    new TelemetryHealthCheck(new TelemetryStatistics()),
                    null,
                    null)
            };
        }
    }
}
