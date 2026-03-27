using System;
using System.Diagnostics.Metrics;
using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Tests.Metrics
{
    /// <summary>
    /// Tests for <see cref="OperationScopeMetrics"/> which had zero coverage.
    /// </summary>
    [TestClass]
    public class OperationScopeMetricsTests
    {
        private MeterListener _listener = null!;

        [TestInitialize]
        public void Setup()
        {
            _listener = new MeterListener();
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, _, _, _) => { });
            _listener.SetMeasurementEventCallback<double>((_, _, _, _) => { });
            _listener.Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _listener.Dispose();
        }

        // --- RecordDuration ---

        [TestMethod]
        public void RecordDuration_ValidInputs_DoesNotThrow()
        {
            OperationScopeMetrics.RecordDuration("test-op", TimeSpan.FromMilliseconds(100), false);
        }

        [TestMethod]
        public void RecordDuration_FailedTrue_DoesNotThrow()
        {
            OperationScopeMetrics.RecordDuration("failed-op", TimeSpan.FromMilliseconds(50), true);
        }

        [TestMethod]
        public void RecordDuration_NullName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => OperationScopeMetrics.RecordDuration(null!, TimeSpan.FromSeconds(1), false));
        }

        [TestMethod]
        public void RecordDuration_EmptyName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => OperationScopeMetrics.RecordDuration(string.Empty, TimeSpan.FromSeconds(1), false));
        }

        [TestMethod]
        public void RecordDuration_ZeroDuration_DoesNotThrow()
        {
            OperationScopeMetrics.RecordDuration("zero-duration", TimeSpan.Zero, false);
        }

        // --- RecordError ---

        [TestMethod]
        public void RecordError_ValidInputs_DoesNotThrow()
        {
            OperationScopeMetrics.RecordError("error-op", new InvalidOperationException("test"));
        }

        [TestMethod]
        public void RecordError_NullName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => OperationScopeMetrics.RecordError(null!, new Exception("test")));
        }

        [TestMethod]
        public void RecordError_EmptyName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => OperationScopeMetrics.RecordError(string.Empty, new Exception("test")));
        }

        [TestMethod]
        public void RecordError_NullException_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => OperationScopeMetrics.RecordError("test-op", null!));
        }

        [TestMethod]
        public void RecordError_DifferentExceptionTypes_DoesNotThrow()
        {
            OperationScopeMetrics.RecordError("multi-error-op", new InvalidOperationException("a"));
            OperationScopeMetrics.RecordError("multi-error-op", new ArgumentException("b"));
            OperationScopeMetrics.RecordError("multi-error-op", new NullReferenceException("c"));
        }
    }
}
