using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Initialization
{
    [TestClass]
    public class StaticTelemetryTests
    {
        [TestInitialize]
        public void Initialize()
        {
            // Clear leaked AsyncLocal state from parallel test projects
            // to prevent CorrelationScope from capturing stale _previousId values
            Telemetry.Shutdown();
            CorrelationContext.Clear();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Always clean up static state after each test
            Telemetry.Shutdown();
            CorrelationContext.Clear();
        }

        [TestMethod]
        public void IsInitialized_FalseByDefault()
        {
            Assert.IsFalse(Telemetry.IsInitialized);
        }

        [TestMethod]
        public void Initialize_Default_ReturnsTrue()
        {
            var result = Telemetry.Initialize();
            Assert.IsTrue(result);
            Assert.IsTrue(Telemetry.IsInitialized);
        }

        [TestMethod]
        public void Initialize_WithOptions_ReturnsTrue()
        {
            var result = Telemetry.Initialize(new TelemetryOptions
            {
                ServiceName = "TestService",
                ServiceVersion = "1.0.0"
            });

            Assert.IsTrue(result);
            Assert.IsTrue(Telemetry.IsInitialized);
        }

        [TestMethod]
        public void Initialize_SecondCall_ReturnsFalse()
        {
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "First" });
            var second = Telemetry.Initialize(new TelemetryOptions { ServiceName = "Second" });

            Assert.IsFalse(second);
        }

        [TestMethod]
        public void Initialize_ThrowsOnNullOptions()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => Telemetry.Initialize((TelemetryOptions)null!));
        }

        [TestMethod]
        public void Initialize_ThrowsOnInvalidOptions()
        {
            var options = new TelemetryOptions { ServiceName = "" };
            Assert.ThrowsExactly<InvalidOperationException>(
                () => Telemetry.Initialize(options));
        }

        [TestMethod]
        public void Shutdown_SetsIsInitializedToFalse()
        {
            Telemetry.Initialize();
            Telemetry.Shutdown();

            Assert.IsFalse(Telemetry.IsInitialized);
        }

        [TestMethod]
        public void Shutdown_SafeWhenNotInitialized()
        {
            Telemetry.Shutdown(); // Should not throw
        }

        [TestMethod]
        public void Shutdown_CanReinitializeAfter()
        {
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "First" });
            Telemetry.Shutdown();

            var result = Telemetry.Initialize(new TelemetryOptions { ServiceName = "Second" });
            Assert.IsTrue(result);
            Assert.IsTrue(Telemetry.IsInitialized);
        }

        [TestMethod]
        public void StartOperation_ThrowsWhenNotInitialized()
        {
            Assert.ThrowsExactly<InvalidOperationException>(
                () => Telemetry.StartOperation("test"));
        }

        [TestMethod]
        public void StartOperation_WorksAfterInitialize()
        {
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "Test" });

            using var scope = Telemetry.StartOperation("TestOp");
            Assert.IsNotNull(scope);
            Assert.AreEqual("TestOp", scope.Name);
        }

        [TestMethod]
        public void TrackEvent_ThrowsWhenNotInitialized()
        {
            Assert.ThrowsExactly<InvalidOperationException>(
                () => Telemetry.TrackEvent("test"));
        }

        [TestMethod]
        public void TrackEvent_WorksAfterInitialize()
        {
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "Test" });
            Telemetry.TrackEvent("TestEvent"); // Should not throw
        }

        [TestMethod]
        public void TrackEvent_ThrowsOnNullEventName()
        {
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "Test" });
            Assert.ThrowsExactly<ArgumentException>(
                () => Telemetry.TrackEvent(null!));
        }

        [TestMethod]
        public void TrackException_ThrowsWhenNotInitialized()
        {
            Assert.ThrowsExactly<InvalidOperationException>(
                () => Telemetry.TrackException(new Exception("test")));
        }

        [TestMethod]
        public void TrackException_WorksAfterInitialize()
        {
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "Test" });
            Telemetry.TrackException(new InvalidOperationException("test")); // Should not throw
        }

        [TestMethod]
        public void RecordMetric_ThrowsWhenNotInitialized()
        {
            Assert.ThrowsExactly<InvalidOperationException>(
                () => Telemetry.RecordMetric("metric", 42.0));
        }

        [TestMethod]
        public void RecordMetric_WorksAfterInitialize()
        {
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "Test" });
            Telemetry.RecordMetric("TestMetric", 42.0); // Should not throw
        }

        [TestMethod]
        public void RecordException_GlobalAggregator_WorksWithoutInitialization()
        {
            // RecordException should work without initialization (global aggregator)
            Telemetry.RecordException(new InvalidOperationException("test"));
            var aggregator = Telemetry.GetExceptionAggregator();
            Assert.IsTrue(aggregator.TotalExceptions > 0);
        }

        [TestMethod]
        public void Statistics_ThrowsWhenNotInitialized()
        {
            Assert.ThrowsExactly<InvalidOperationException>(
                () => _ = Telemetry.Statistics);
        }

        [TestMethod]
        public void Statistics_ReturnsAfterInitialize()
        {
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "Test" });
            Assert.IsNotNull(Telemetry.Statistics);
        }

        [TestMethod]
        public void CurrentCorrelationId_NullWhenNotSet()
        {
            // With no AsyncLocal set, CurrentCorrelationId should be null
            Assert.IsNull(Telemetry.CurrentCorrelationId);
        }

        [TestMethod]
        public void SetCorrelationId_SetsAndRestores()
        {
            using (Telemetry.SetCorrelationId("test-correlation-123"))
            {
                Assert.AreEqual("test-correlation-123", Telemetry.CurrentCorrelationId);
            }

            // After scope, it should be restored (null if not previously set)
            Assert.IsNull(Telemetry.CurrentCorrelationId);
        }

        [TestMethod]
        public void SetCorrelationId_ThrowsOnEmpty()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => Telemetry.SetCorrelationId(""));
        }

        [TestMethod]
        public void SetCorrelationId_ThrowsOnNull()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => Telemetry.SetCorrelationId(null!));
        }

        [TestMethod]
        public void BeginCorrelation_GeneratesNewId()
        {
            using (Telemetry.BeginCorrelation())
            {
                var id = Telemetry.CurrentCorrelationId;
                Assert.IsNotNull(id);
                Assert.AreEqual(32, id!.Length); // Guid.ToString("N") is 32 chars
            }
        }

        [TestMethod]
        public void BeginCorrelation_RestoresPreviousId()
        {
            using (Telemetry.SetCorrelationId("outer-id"))
            {
                using (Telemetry.BeginCorrelation())
                {
                    var inner = Telemetry.CurrentCorrelationId;
                    Assert.IsNotNull(inner);
                    Assert.AreNotEqual("outer-id", inner);
                }

                Assert.AreEqual("outer-id", Telemetry.CurrentCorrelationId);
            }
        }

        [TestMethod]
        public void CurrentActivity_ReturnsNull_WhenNoActivityIsActive()
        {
            Assert.IsNull(Telemetry.CurrentActivity);
        }

        [TestMethod]
        public void ThreadSafe_OnlyOneInitializationSucceeds()
        {
            var results = new ConcurrentBag<bool>();
            var threadCount = 10;

            Parallel.For(0, threadCount, _ =>
            {
                var result = Telemetry.Initialize(new TelemetryOptions
                {
                    ServiceName = "ThreadTest"
                });
                results.Add(result);
            });

            Assert.AreEqual(1, results.Count(r => r));
            Assert.AreEqual(threadCount - 1, results.Count(r => !r));
        }
    }
}
