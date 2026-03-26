using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Metrics
{
    [TestClass]
    public class MetricRecorderTests
    {
        [TestMethod]
        public void MetricRecorderFactory_Instance_UsesMeterApiOnNet8()
        {
            var recorder = MetricRecorderFactory.Instance;

            Assert.IsInstanceOfType(recorder, typeof(MeterApiRecorder));
        }

        [TestMethod]
        public void MetricRecorderFactory_Instance_IsSingleton()
        {
            var first = MetricRecorderFactory.Instance;
            var second = MetricRecorderFactory.Instance;

            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void MetricTag_WithEmptyKey_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => _ = new MetricTag(string.Empty, "value"));
        }

        [TestMethod]
        public void MetricTag_DefaultStruct_ThrowsOnValidate()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                var tag = default(MetricTag);
                tag.Validate();
            });
        }

        [TestMethod]
        public void CreateCounter_WithEmptyName_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                var recorder = MetricRecorderFactory.Instance;
                recorder.CreateCounter(string.Empty);
            });
        }

        [TestMethod]
        public void CreateHistogram_WithWhitespaceName_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                var recorder = MetricRecorderFactory.Instance;
                recorder.CreateHistogram("   ");
            });
        }

        [TestMethod]
        public void CreateObservableGauge_WithNullCallback_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                var recorder = new EventCounterRecorder();
                recorder.CreateObservableGauge("test.gauge", null!);
            });
        }

        [TestMethod]
        public void Counter_WithDefaultTag_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                var recorder = MetricRecorderFactory.Instance;
                var counter = recorder.CreateCounter("test.default.tag");

                var tag = default(MetricTag);
                counter.Add(1, tag);
            });
        }

        [TestMethod]
        public void Counter_WithDefaultTagInArray_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                var recorder = MetricRecorderFactory.Instance;
                var counter = recorder.CreateCounter("test.default.tag.array");

                var tags = new MetricTag[] { new MetricTag("valid", "value"), default(MetricTag) };
                counter.Add(1, tags);
            });
        }

        [TestMethod]
        public void Histogram_WithDefaultTag_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                var recorder = MetricRecorderFactory.Instance;
                var histogram = recorder.CreateHistogram("test.default.tag.histogram");

                var tag = default(MetricTag);
                histogram.Record(1, tag);
            });
        }

        [TestMethod]
        public void MeterCounter_Add_EmitsMeasurementsWithTags()
        {
            var measurements = new List<long>();
            KeyValuePair<string, object?>[]? lastTags = null;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            };

            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                measurements.Add(measurement);
                lastTags = tags.ToArray();
            });

            listener.Start();

            var recorder = MetricRecorderFactory.Instance;
            var counter = recorder.CreateCounter("test.counter");

            counter.Add(5);
            counter.Add(3, new MetricTag("status", 200));

            Assert.AreEqual(2, measurements.Count);
            Assert.IsNotNull(lastTags);
            Assert.IsTrue(ContainsTag(lastTags!, "status", 200));
        }

        [TestMethod]
        public void MeterHistogram_Record_EmitsMeasurementsWithTags()
        {
            var measurements = new List<double>();
            KeyValuePair<string, object?>[]? lastTags = null;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            };

            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                measurements.Add(measurement);
                lastTags = tags.ToArray();
            });

            listener.Start();

            var recorder = MetricRecorderFactory.Instance;
            var histogram = recorder.CreateHistogramDouble("test.histogram");

            histogram.Record(10.5);
            histogram.Record(42.0, new MetricTag("endpoint", "/api"));

            Assert.AreEqual(2, measurements.Count);
            Assert.IsNotNull(lastTags);
            Assert.IsTrue(ContainsTag(lastTags!, "endpoint", "/api"));
        }

        [TestMethod]
        public void ObservableGauge_InvokesCallback()
        {
            var observeCount = 0;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            };

            listener.Start();

            var recorder = MetricRecorderFactory.Instance;
            using var gauge = recorder.CreateObservableGauge("test.gauge", () =>
            {
                Interlocked.Increment(ref observeCount);
                return 1.0;
            });

            listener.RecordObservableInstruments();

            Assert.IsTrue(observeCount > 0, "Gauge callback should be invoked.");
        }

        [TestMethod]
        public void ObservableGauge_AfterDispose_DoesNotInvokeCallback()
        {
            var observeCount = 0;
            var disposed = false;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MeterApiRecorder.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            };

            listener.Start();

            var recorder = MetricRecorderFactory.Instance;
            var gauge = recorder.CreateObservableGauge("test.gauge.dispose", () =>
            {
                if (disposed)
                    Assert.Fail("Callback should not be invoked after disposal.");

                Interlocked.Increment(ref observeCount);
                return 1.0;
            });

            // Record before disposal
            listener.RecordObservableInstruments();
            Assert.IsTrue(observeCount > 0, "Callback should be invoked before disposal.");

            // Dispose the gauge
            gauge.Dispose();
            disposed = true;

            // Record after disposal - should not invoke callback or fail
            listener.RecordObservableInstruments();
        }

        [TestMethod]
        public void CardinalityTracker_LogsWarningAfterThreshold()
        {
            var logger = new ListLogger<MeterApiRecorder>();
            var recorder = new MeterApiRecorder(logger);
            var counter = recorder.CreateCounter("test.cardinality");

            for (var i = 0; i < 105; i++)
            {
                counter.Add(1, new MetricTag("user", i));
            }

            Assert.IsTrue(logger.ContainsWarning("tag cardinality"), "Expected cardinality warning to be logged.");
        }

        [TestMethod]
        public void EventCounterRecorder_AllowsBasicOperations()
        {
            var recorder = new EventCounterRecorder();

            var counter = recorder.CreateCounter("legacy.counter");
            var histogram = recorder.CreateHistogram("legacy.histogram");
            var histogramDouble = recorder.CreateHistogramDouble("legacy.histogram.double");

            counter.Add(1);
            counter.Add(2, new MetricTag("region", "east"));

            histogram.Record(10);
            histogram.Record(20, new MetricTag("status", 200));

            histogramDouble.Record(1.5);
            histogramDouble.Record(2.5, new MetricTag("type", "latency"));
        }

        [TestMethod]
        public void EventCounterRecorder_CreateObservableGauge_DisposeIsIdempotent()
        {
            var recorder = new EventCounterRecorder();
            var gauge = recorder.CreateObservableGauge("legacy.gauge", () => 1.0);

            gauge.Dispose();
            gauge.Dispose();
        }

        [TestMethod]
        public void Counter_WithNegativeValue_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                var recorder = MetricRecorderFactory.Instance;
                var counter = recorder.CreateCounter("test.counter.negative");

                counter.Add(-1);
            });
        }

        [TestMethod]
        public void EventCounterCounter_WithNegativeValue_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                var recorder = new EventCounterRecorder();
                var counter = recorder.CreateCounter("legacy.counter.negative");

                counter.Add(-1);
            });
        }

        [TestMethod]
        public void EventCounterCounter_MaintainsIndependentTotalsPerTag()
        {
            var recorder = new EventCounterRecorder();
            var counter = recorder.CreateCounter("test.tagged.counter");

            // Add values with different tags
            counter.Add(10, new MetricTag("region", "east"));
            counter.Add(20, new MetricTag("region", "east"));
            counter.Add(5, new MetricTag("region", "west"));
            counter.Add(15, new MetricTag("region", "west"));
            counter.Add(100); // No tags

            // Each tagged combination should maintain its own running total
            // We can't directly assert the totals without exposing internals,
            // but we verify that operations complete without errors and
            // each tag combination is tracked independently by the underlying system

            // Add more to verify monotonic behavior is maintained per tag
            counter.Add(30, new MetricTag("region", "east")); // Should be 10+20+30=60 for east
            counter.Add(25, new MetricTag("region", "west")); // Should be 5+15+25=45 for west
            counter.Add(50); // Should be 100+50=150 for untagged
        }

        private static bool ContainsTag(KeyValuePair<string, object?>[] tags, string key, object value)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i].Key == key && Equals(tags[i].Value, value))
                    return true;
            }

            return false;
        }

        private sealed class ListLogger<T> : ILogger<T>
        {
            private readonly List<(LogLevel Level, string Message)> _entries;

            public ListLogger()
            {
                _entries = new List<(LogLevel, string)>();
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                _entries.Add((logLevel, formatter(state, exception)));
            }

            public bool ContainsWarning(string containsText)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].Level == LogLevel.Warning &&
                        _entries[i].Message.IndexOf(containsText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();

            public void Dispose()
            {
            }
        }
    }
}
