using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Datadog.Tests
{
    [TestClass]
    public class DatadogMetricsExporterTests
    {
        private static DatadogOptions CreateDefaultOptions()
        {
            var options = new DatadogOptions
            {
                ServiceName = "test-service",
                AgentHost = "localhost",
                AgentPort = 8125
            };
            options.ApplyEnvironmentDefaults();
            return options;
        }

        [TestMethod]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DatadogMetricsExporter(null!));
        }

        [TestMethod]
        public void Constructor_ValidOptions_CreatesInstance()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            Assert.IsNotNull(exporter);
        }

        [TestMethod]
        public void Constructor_WithLogger_CreatesInstance()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions(), logger: null);
            Assert.IsNotNull(exporter);
        }

        [TestMethod]
        public void Counter_ValidName_DoesNotThrow()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            // DogStatsD is fire-and-forget UDP — no agent needed
            exporter.Counter("test.counter", 1);
        }

        [TestMethod]
        public void Counter_WithTags_DoesNotThrow()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            var tags = new Dictionary<string, string>
            {
                ["endpoint"] = "/api/orders",
                ["status"] = "200"
            };
            exporter.Counter("http.requests", 1, tags);
        }

        [TestMethod]
        public void Counter_NullName_ThrowsArgumentException()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            Assert.ThrowsExactly<ArgumentException>(
                () => exporter.Counter(null!, 1));
        }

        [TestMethod]
        public void Counter_EmptyName_ThrowsArgumentException()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            Assert.ThrowsExactly<ArgumentException>(
                () => exporter.Counter("", 1));
        }

        [TestMethod]
        public void Gauge_ValidName_DoesNotThrow()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Gauge("test.connections", 42.0);
        }

        [TestMethod]
        public void Gauge_NullName_ThrowsArgumentException()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            Assert.ThrowsExactly<ArgumentException>(
                () => exporter.Gauge(null!, 42.0));
        }

        [TestMethod]
        public void Histogram_ValidName_DoesNotThrow()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Histogram("test.duration", 123.45);
        }

        [TestMethod]
        public void Histogram_NullName_ThrowsArgumentException()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            Assert.ThrowsExactly<ArgumentException>(
                () => exporter.Histogram(null!, 100));
        }

        [TestMethod]
        public void Distribution_ValidName_DoesNotThrow()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Distribution("test.latency", 99.9);
        }

        [TestMethod]
        public void Distribution_NullName_ThrowsArgumentException()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            Assert.ThrowsExactly<ArgumentException>(
                () => exporter.Distribution(null!, 100));
        }

        [TestMethod]
        public void Timing_ValidName_DoesNotThrow()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Timing("test.timing", 456.78);
        }

        [TestMethod]
        public void Timing_NullName_ThrowsArgumentException()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            Assert.ThrowsExactly<ArgumentException>(
                () => exporter.Timing(null!, 100));
        }

        [TestMethod]
        public void Counter_AfterDispose_ThrowsObjectDisposedException()
        {
            var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(
                () => exporter.Counter("test", 1));
        }

        [TestMethod]
        public void Gauge_AfterDispose_ThrowsObjectDisposedException()
        {
            var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(
                () => exporter.Gauge("test", 1.0));
        }

        [TestMethod]
        public void Histogram_AfterDispose_ThrowsObjectDisposedException()
        {
            var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(
                () => exporter.Histogram("test", 1.0));
        }

        [TestMethod]
        public void Distribution_AfterDispose_ThrowsObjectDisposedException()
        {
            var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(
                () => exporter.Distribution("test", 1.0));
        }

        [TestMethod]
        public void Timing_AfterDispose_ThrowsObjectDisposedException()
        {
            var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(
                () => exporter.Timing("test", 1.0));
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Dispose();
            exporter.Dispose(); // Should not throw
        }

        [TestMethod]
        public void Counter_WithNullTags_DoesNotThrow()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Counter("test.counter", 5, tags: null);
        }

        [TestMethod]
        public void Counter_WithEmptyTags_DoesNotThrow()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            exporter.Counter("test.counter", 5, new Dictionary<string, string>());
        }

        [TestMethod]
        public void AllMetricTypes_WithTags_DoesNotThrow()
        {
            using var exporter = new DatadogMetricsExporter(CreateDefaultOptions());
            var tags = new Dictionary<string, string>
            {
                ["region"] = "us-east-1",
                ["tier"] = "premium"
            };

            exporter.Counter("test.counter", 1, tags);
            exporter.Gauge("test.gauge", 42.0, tags);
            exporter.Histogram("test.histogram", 123.45, tags);
            exporter.Distribution("test.distribution", 99.9, tags);
            exporter.Timing("test.timing", 456.78, tags);
        }

        [TestMethod]
        public void Constructor_WithGlobalTags_DoesNotThrow()
        {
            var options = new DatadogOptions
            {
                ServiceName = "tagged-service",
                Environment = "test",
                Version = "1.0.0",
                AgentHost = "localhost"
            };
            options.ApplyEnvironmentDefaults();

            using var exporter = new DatadogMetricsExporter(options);
            exporter.Counter("test.counter", 1);
        }

        [TestMethod]
        public void Constructor_WithMetricPrefix_DoesNotThrow()
        {
            var options = new DatadogOptions
            {
                MetricPrefix = "myapp",
                AgentHost = "localhost"
            };
            options.ApplyEnvironmentDefaults();

            using var exporter = new DatadogMetricsExporter(options);
            exporter.Counter("requests", 1);
        }

        [TestMethod]
        public void Constructor_WithDisabledMetrics_CreatesInstance()
        {
            var options = new DatadogOptions
            {
                EnableMetricsExporter = false,
                AgentHost = "localhost"
            };
            using var exporter = new DatadogMetricsExporter(options);
            Assert.IsNotNull(exporter);
        }

        [TestMethod]
        public void Counter_WhenDisabled_DoesNotThrow()
        {
            var options = new DatadogOptions { EnableMetricsExporter = false };
            using var exporter = new DatadogMetricsExporter(options);
            exporter.Counter("test.counter", 1);
        }

        [TestMethod]
        public void AllMetricTypes_WhenDisabled_AreNoOps()
        {
            var options = new DatadogOptions { EnableMetricsExporter = false };
            using var exporter = new DatadogMetricsExporter(options);
            var tags = new Dictionary<string, string> { ["env"] = "test" };

            exporter.Counter("c", 1, tags);
            exporter.Gauge("g", 1.0, tags);
            exporter.Histogram("h", 1.0, tags);
            exporter.Distribution("d", 1.0, tags);
            exporter.Timing("t", 1.0, tags);
        }
    }
}
