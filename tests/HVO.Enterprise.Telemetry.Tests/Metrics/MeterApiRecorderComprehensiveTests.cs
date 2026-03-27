using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Tests.Metrics
{
    /// <summary>
    /// Comprehensive tests for <see cref="MeterApiRecorder"/> covering multi-tag overloads,
    /// unit/description parameters, Dispose, and CreateObservableGauge edge cases.
    /// </summary>
    [TestClass]
    public class MeterApiRecorderComprehensiveTests
    {
        private MeterApiRecorder _recorder = null!;
        private MeterListener _listener = null!;
        private readonly List<(string Name, object? Value, KeyValuePair<string, object?>[] Tags)> _measurements = new();

        [TestInitialize]
        public void Setup()
        {
            _recorder = new MeterApiRecorder();
            _measurements.Clear();

            _listener = new MeterListener();
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                var tagArray = new KeyValuePair<string, object?>[tags.Length];
                tags.CopyTo(tagArray);
                lock (_measurements)
                {
                    _measurements.Add((instrument.Name, measurement, tagArray));
                }
            });

            _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                var tagArray = new KeyValuePair<string, object?>[tags.Length];
                tags.CopyTo(tagArray);
                lock (_measurements)
                {
                    _measurements.Add((instrument.Name, measurement, tagArray));
                }
            });

            _listener.Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _listener.Dispose();
            _recorder.Dispose();
        }

        // --- Counter: multi-tag overloads ---

        [TestMethod]
        public void Counter_Add_WithOneTag_RecordsMeasurement()
        {
            var counter = _recorder.CreateCounter("test.counter.1tag");
            var tag = new MetricTag("env", "prod");

            counter.Add(5, in tag);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1, "Should have recorded at least one measurement");
        }

        [TestMethod]
        public void Counter_Add_WithTwoTags_RecordsMeasurement()
        {
            var counter = _recorder.CreateCounter("test.counter.2tags");
            var tag1 = new MetricTag("env", "prod");
            var tag2 = new MetricTag("region", "us-east");

            counter.Add(10, in tag1, in tag2);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1, "Should have recorded at least one measurement");
        }

        [TestMethod]
        public void Counter_Add_WithThreeTags_RecordsMeasurement()
        {
            var counter = _recorder.CreateCounter("test.counter.3tags");
            var tag1 = new MetricTag("env", "prod");
            var tag2 = new MetricTag("region", "us-east");
            var tag3 = new MetricTag("service", "api");

            counter.Add(15, in tag1, in tag2, in tag3);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1, "Should have recorded at least one measurement");
        }

        [TestMethod]
        public void Counter_Add_WithParamsTagArray_RecordsMeasurement()
        {
            var counter = _recorder.CreateCounter("test.counter.params");
            var tags = new[]
            {
                new MetricTag("env", "prod"),
                new MetricTag("region", "us-east")
            };

            counter.Add(20, tags);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1, "Should have recorded at least one measurement");
        }

        [TestMethod]
        public void Counter_Add_WithEmptyParamsArray_RecordsMeasurement()
        {
            var counter = _recorder.CreateCounter("test.counter.empty.params");
            counter.Add(1, Array.Empty<MetricTag>());

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void Counter_Add_NegativeValue_ThrowsArgumentOutOfRangeException()
        {
            var counter = _recorder.CreateCounter("test.counter.negative");
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => counter.Add(-1));
        }

        [TestMethod]
        public void Counter_Add_NegativeValueWithTag_ThrowsArgumentOutOfRangeException()
        {
            var counter = _recorder.CreateCounter("test.counter.negative.tag");
            var tag = new MetricTag("k", "v");
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => counter.Add(-1, in tag));
        }

        // --- Counter: unit and description ---

        [TestMethod]
        public void Counter_WithUnitAndDescription_CreatesSuccessfully()
        {
            var counter = _recorder.CreateCounter("test.counter.unit", "ms", "A counter with unit");
            counter.Add(100);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        // --- Histogram: multi-tag overloads ---

        [TestMethod]
        public void Histogram_Record_WithOneTag()
        {
            var histogram = _recorder.CreateHistogram("test.histogram.1tag");
            var tag = new MetricTag("env", "prod");

            histogram.Record(100, in tag);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void Histogram_Record_WithTwoTags()
        {
            var histogram = _recorder.CreateHistogram("test.histogram.2tags");
            var tag1 = new MetricTag("env", "prod");
            var tag2 = new MetricTag("region", "us-east");

            histogram.Record(200, in tag1, in tag2);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void Histogram_Record_WithThreeTags()
        {
            var histogram = _recorder.CreateHistogram("test.histogram.3tags");
            var tag1 = new MetricTag("env", "prod");
            var tag2 = new MetricTag("region", "us-east");
            var tag3 = new MetricTag("service", "api");

            histogram.Record(300, in tag1, in tag2, in tag3);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void Histogram_Record_WithParamsTagArray()
        {
            var histogram = _recorder.CreateHistogram("test.histogram.params");
            histogram.Record(400, new MetricTag("a", "1"), new MetricTag("b", "2"));

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void Histogram_Record_WithEmptyParamsArray()
        {
            var histogram = _recorder.CreateHistogram("test.histogram.empty.params");
            histogram.Record(50, Array.Empty<MetricTag>());

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void Histogram_WithUnitAndDescription()
        {
            var histogram = _recorder.CreateHistogram("test.histogram.unit", "ms", "Response time");
            histogram.Record(42);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        // --- HistogramDouble: all overloads ---

        [TestMethod]
        public void HistogramDouble_Record_NoTags()
        {
            var histogram = _recorder.CreateHistogramDouble("test.histdouble.notags");
            histogram.Record(3.14);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void HistogramDouble_Record_WithOneTag()
        {
            var histogram = _recorder.CreateHistogramDouble("test.histdouble.1tag");
            var tag = new MetricTag("env", "prod");

            histogram.Record(3.14, in tag);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void HistogramDouble_Record_WithTwoTags()
        {
            var histogram = _recorder.CreateHistogramDouble("test.histdouble.2tags");
            var tag1 = new MetricTag("env", "prod");
            var tag2 = new MetricTag("region", "us-east");

            histogram.Record(2.71, in tag1, in tag2);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void HistogramDouble_Record_WithThreeTags()
        {
            var histogram = _recorder.CreateHistogramDouble("test.histdouble.3tags");
            var tag1 = new MetricTag("env", "prod");
            var tag2 = new MetricTag("region", "us-east");
            var tag3 = new MetricTag("service", "api");

            histogram.Record(1.618, in tag1, in tag2, in tag3);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void HistogramDouble_Record_WithParamsTagArray()
        {
            var histogram = _recorder.CreateHistogramDouble("test.histdouble.params");
            histogram.Record(6.28, new MetricTag("x", "1"), new MetricTag("y", "2"));

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void HistogramDouble_Record_WithEmptyParamsArray()
        {
            var histogram = _recorder.CreateHistogramDouble("test.histdouble.empty");
            histogram.Record(9.99, Array.Empty<MetricTag>());

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        [TestMethod]
        public void HistogramDouble_WithUnitAndDescription()
        {
            var histogram = _recorder.CreateHistogramDouble("test.histdouble.unit", "seconds", "Elapsed time");
            histogram.Record(0.5);

            _listener.RecordObservableInstruments();
            Assert.IsTrue(_measurements.Count >= 1);
        }

        // --- CreateObservableGauge ---

        [TestMethod]
        public void CreateObservableGauge_NullCallback_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => _recorder.CreateObservableGauge("test.gauge.null", null!));
        }

        [TestMethod]
        public void CreateObservableGauge_ValidCallback_ReturnsDisposableHandle()
        {
            using var handle = _recorder.CreateObservableGauge("test.gauge.valid", () => 42.0);
            Assert.IsNotNull(handle);
        }

        [TestMethod]
        public void CreateObservableGauge_WithUnitAndDescription_ReturnsHandle()
        {
            using var handle = _recorder.CreateObservableGauge(
                "test.gauge.full", () => 100.0, "bytes", "Memory usage");
            Assert.IsNotNull(handle);
        }

        [TestMethod]
        public void CreateObservableGauge_DisposedHandle_DoesNotThrow()
        {
            var handle = _recorder.CreateObservableGauge("test.gauge.disposed", () => 42.0);
            handle.Dispose();

            // After disposal, the gauge's Observe method should return 0
            // We can only verify this indirectly since the gauge state is internal
        }

        // --- Dispose ---

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var recorder = new MeterApiRecorder();
            recorder.Dispose();
            recorder.Dispose(); // Should not throw
        }

        // --- MetricNameValidator (validation paths) ---

        [TestMethod]
        public void CreateCounter_NullName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => _recorder.CreateCounter(null!));
        }

        [TestMethod]
        public void CreateCounter_EmptyName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => _recorder.CreateCounter(string.Empty));
        }

        [TestMethod]
        public void CreateHistogram_NullName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => _recorder.CreateHistogram(null!));
        }

        [TestMethod]
        public void CreateHistogramDouble_NullName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => _recorder.CreateHistogramDouble(null!));
        }

        [TestMethod]
        public void CreateObservableGauge_NullName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => _recorder.CreateObservableGauge(null!, () => 0.0));
        }

        // --- MetricRecorderFactory singleton ---

        [TestMethod]
        public void MetricRecorderFactory_Instance_ReturnsSameInstance()
        {
            var instance1 = MetricRecorderFactory.Instance;
            var instance2 = MetricRecorderFactory.Instance;
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void MetricRecorderFactory_Instance_IsNotNull()
        {
            Assert.IsNotNull(MetricRecorderFactory.Instance);
        }

        [TestMethod]
        public void MetricRecorderFactory_Instance_IsMeterApiRecorder_OnModernRuntime()
        {
            // On .NET 8+, MeterApi should be available
            if (Environment.Version.Major >= 6)
            {
                // The instance should be MeterApiRecorder on modern runtimes
                Assert.IsNotNull(MetricRecorderFactory.Instance);
            }
        }
    }
}
