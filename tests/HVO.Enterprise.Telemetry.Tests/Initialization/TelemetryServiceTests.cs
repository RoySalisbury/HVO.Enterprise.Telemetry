using System;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Initialization
{
    [TestClass]
    public class TelemetryServiceTests
    {
        private static TelemetryOptions CreateDefaultOptions()
        {
            return new TelemetryOptions
            {
                ServiceName = "TestService",
                ServiceVersion = "1.0.0",
                Environment = "Test"
            };
        }

        private static TelemetryService CreateService(TelemetryOptions? options = null)
        {
            return new TelemetryService(
                options ?? CreateDefaultOptions(),
                NullLoggerFactory.Instance);
        }

        [TestMethod]
        public void Constructor_Static_CreatesServiceSuccessfully()
        {
            using var service = CreateService();
            Assert.IsNotNull(service);
            Assert.IsTrue(service.IsEnabled);
        }

        [TestMethod]
        public void Constructor_Static_ThrowsOnNullOptions()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new TelemetryService((TelemetryOptions)null!, NullLoggerFactory.Instance));
        }

        [TestMethod]
        public void Constructor_DI_CreatesServiceSuccessfully()
        {
            var options = Options.Create(CreateDefaultOptions());
            var stats = new TelemetryStatistics();
            var factory = new OperationScopeFactory("HVO.Enterprise.Telemetry");
            var logger = NullLogger<TelemetryService>.Instance;

            using var service = new TelemetryService(options, stats, factory, logger);
            Assert.IsNotNull(service);
            Assert.IsTrue(service.IsEnabled);
        }

        [TestMethod]
        public void Constructor_DI_ThrowsOnNullOptions()
        {
            var stats = new TelemetryStatistics();
            var factory = new OperationScopeFactory("HVO.Enterprise.Telemetry");
            var logger = NullLogger<TelemetryService>.Instance;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new TelemetryService((IOptions<TelemetryOptions>)null!, stats, factory, logger));
        }

        [TestMethod]
        public void IsEnabled_ReturnsFalseWhenDisabled()
        {
            var options = CreateDefaultOptions();
            options.Enabled = false;

            using var service = CreateService(options);
            Assert.IsFalse(service.IsEnabled);
        }

        [TestMethod]
        public void IsEnabled_ReturnsTrueByDefault()
        {
            using var service = CreateService();
            Assert.IsTrue(service.IsEnabled);
        }

        [TestMethod]
        public void Statistics_ReturnsNonNullInstance()
        {
            using var service = CreateService();
            Assert.IsNotNull(service.Statistics);
        }

        [TestMethod]
        public void Start_SetsServiceAsStarted()
        {
            using var service = CreateService();
            service.Start();
            // No exception means success; service is running
            Assert.IsTrue(service.IsEnabled);
        }

        [TestMethod]
        public void Start_Idempotent_CanCallMultipleTimes()
        {
            using var service = CreateService();
            service.Start();
            service.Start(); // Should not throw
        }

        [TestMethod]
        public void Shutdown_StopsService()
        {
            using var service = CreateService();
            service.Start();
            service.Shutdown();
            // No exception means success
        }

        [TestMethod]
        public void Shutdown_SafeWhenNotStarted()
        {
            using var service = CreateService();
            service.Shutdown(); // Should not throw
        }

        [TestMethod]
        public void StartOperation_ReturnsNonNullScope()
        {
            using var service = CreateService();
            service.Start();

            using var scope = service.StartOperation("TestOperation");
            Assert.IsNotNull(scope);
            Assert.AreEqual("TestOperation", scope.Name);
        }

        [TestMethod]
        public void StartOperation_ThrowsOnNullName()
        {
            using var service = CreateService();
            Assert.ThrowsExactly<ArgumentException>(
                () => service.StartOperation(null!));
        }

        [TestMethod]
        public void StartOperation_ThrowsOnEmptyName()
        {
            using var service = CreateService();
            Assert.ThrowsExactly<ArgumentException>(
                () => service.StartOperation(""));
        }

        [TestMethod]
        public void StartOperation_WhenDisabled_ReturnsNoOpScope()
        {
            var options = CreateDefaultOptions();
            options.Enabled = false;

            using var service = CreateService(options);
            using var scope = service.StartOperation("TestOperation");

            Assert.IsNotNull(scope);
            Assert.IsNull(scope.Activity); // NoOp scope has no activity
        }

        [TestMethod]
        public void StartOperation_IncrementsStatistics()
        {
            using var service = CreateService();
            service.Start();

            var before = service.Statistics.ActivitiesCreated;
            using (service.StartOperation("TestOperation"))
            {
                // Scope is active
            }

            Assert.IsTrue(service.Statistics.ActivitiesCreated > before);
        }

        [TestMethod]
        public void TrackException_RecordsException()
        {
            using var service = CreateService();
            service.Start();

            var before = service.Statistics.ExceptionsTracked;
            service.TrackException(new InvalidOperationException("test"));

            Assert.AreEqual(before + 1, service.Statistics.ExceptionsTracked);
        }

        [TestMethod]
        public void TrackException_ThrowsOnNull()
        {
            using var service = CreateService();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => service.TrackException(null!));
        }

        [TestMethod]
        public void TrackException_WhenDisabled_DoesNotRecord()
        {
            var options = CreateDefaultOptions();
            options.Enabled = false;

            using var service = CreateService(options);
            var before = service.Statistics.ExceptionsTracked;

            service.TrackException(new InvalidOperationException("test"));

            Assert.AreEqual(before, service.Statistics.ExceptionsTracked);
        }

        [TestMethod]
        public void TrackEvent_IncrementsStatistics()
        {
            using var service = CreateService();
            service.Start();

            var before = service.Statistics.EventsRecorded;
            service.TrackEvent("TestEvent");

            Assert.AreEqual(before + 1, service.Statistics.EventsRecorded);
        }

        [TestMethod]
        public void TrackEvent_ThrowsOnNullName()
        {
            using var service = CreateService();
            Assert.ThrowsExactly<ArgumentException>(
                () => service.TrackEvent(null!));
        }

        [TestMethod]
        public void TrackEvent_ThrowsOnEmptyName()
        {
            using var service = CreateService();
            Assert.ThrowsExactly<ArgumentException>(
                () => service.TrackEvent(""));
        }

        [TestMethod]
        public void TrackEvent_WhenDisabled_DoesNotRecord()
        {
            var options = CreateDefaultOptions();
            options.Enabled = false;

            using var service = CreateService(options);
            var before = service.Statistics.EventsRecorded;

            service.TrackEvent("TestEvent");

            Assert.AreEqual(before, service.Statistics.EventsRecorded);
        }

        [TestMethod]
        public void RecordMetric_IncrementsStatistics()
        {
            using var service = CreateService();
            service.Start();

            var before = service.Statistics.MetricsRecorded;
            service.RecordMetric("TestMetric", 42.0);

            Assert.AreEqual(before + 1, service.Statistics.MetricsRecorded);
        }

        [TestMethod]
        public void RecordMetric_ThrowsOnNullName()
        {
            using var service = CreateService();
            Assert.ThrowsExactly<ArgumentException>(
                () => service.RecordMetric(null!, 0));
        }

        [TestMethod]
        public void RecordMetric_ThrowsOnEmptyName()
        {
            using var service = CreateService();
            Assert.ThrowsExactly<ArgumentException>(
                () => service.RecordMetric("", 0));
        }

        [TestMethod]
        public void RecordMetric_WhenDisabled_DoesNotRecord()
        {
            var options = CreateDefaultOptions();
            options.Enabled = false;

            using var service = CreateService(options);
            var before = service.Statistics.MetricsRecorded;

            service.RecordMetric("TestMetric", 42.0);

            Assert.AreEqual(before, service.Statistics.MetricsRecorded);
        }

        [TestMethod]
        public void Dispose_ShutsDownService()
        {
            var service = CreateService();
            service.Start();
            service.Dispose();

            // After dispose, IsEnabled should be false
            Assert.IsFalse(service.IsEnabled);
        }

        [TestMethod]
        public void Dispose_Idempotent_CanCallMultipleTimes()
        {
            var service = CreateService();
            service.Dispose();
            service.Dispose(); // Should not throw
        }
    }
}
