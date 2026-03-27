using System;
using HVO.Enterprise.Telemetry.HealthChecks;

namespace HVO.Enterprise.Telemetry.Tests.HealthChecks
{
    [TestClass]
    public class RollingWindowTests
    {
        [TestMethod]
        public void GetRate_Empty_ReturnsZero()
        {
            var window = new TelemetryStatistics.RollingWindow(TimeSpan.FromMinutes(1));

            Assert.AreEqual(0.0, window.GetRate());
        }

        [TestMethod]
        public void GetRate_WithEvents_ReturnsEventsPerSecond()
        {
            var window = new TelemetryStatistics.RollingWindow(TimeSpan.FromMinutes(1));

            for (int i = 0; i < 60; i++)
            {
                window.Record(DateTimeOffset.UtcNow);
            }

            var rate = window.GetRate();
            Assert.AreEqual(1.0, rate, 0.1); // 60 events / 60 seconds = 1/sec
        }

        [TestMethod]
        public void GetRate_OldEventsExpire()
        {
            // Use a short window
            var window = new TelemetryStatistics.RollingWindow(TimeSpan.FromMinutes(1));

            // Record events with timestamps already outside the window
            var pastTime = DateTimeOffset.UtcNow.AddMinutes(-2);
            window.Record(pastTime);
            window.Record(pastTime);

            // CleanOld is called during GetRate, so old events will be removed
            Assert.AreEqual(0.0, window.GetRate());
        }

        [TestMethod]
        public void Clear_RemovesAllEvents()
        {
            var window = new TelemetryStatistics.RollingWindow(TimeSpan.FromMinutes(1));

            for (int i = 0; i < 100; i++)
            {
                window.Record(DateTimeOffset.UtcNow);
            }

            Assert.IsTrue(window.GetRate() > 0);

            window.Clear();

            Assert.AreEqual(0.0, window.GetRate());
        }

        [TestMethod]
        public void Constructor_NonPositiveDuration_Throws()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new TelemetryStatistics.RollingWindow(TimeSpan.Zero));

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new TelemetryStatistics.RollingWindow(TimeSpan.FromSeconds(-1)));
        }

        [TestMethod]
        public void Record_ThreadSafe()
        {
            var window = new TelemetryStatistics.RollingWindow(TimeSpan.FromMinutes(1));
            const int threadCount = 10;
            const int iterations = 1000;

            var tasks = new System.Threading.Tasks.Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        window.Record(DateTimeOffset.UtcNow);
                    }
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);

            // All events should be within the window, so rate should be positive
            Assert.IsTrue(window.GetRate() > 0);
        }
    }
}
