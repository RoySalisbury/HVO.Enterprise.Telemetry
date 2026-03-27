using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.AppInsights;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace HVO.Enterprise.Telemetry.AppInsights.Tests
{
    [TestClass]
    public class ApplicationInsightsBridgeTests
    {
        private TelemetryConfiguration? _configuration;
        private StubTelemetryChannel? _channel;

        [TestInitialize]
        public void Initialize()
        {
            CorrelationContext.Current = null!;
            Activity.Current = null;

            _channel = new StubTelemetryChannel();
            _configuration = new TelemetryConfiguration
            {
                TelemetryChannel = _channel,
                ConnectionString = "InstrumentationKey=test-key-00000000-0000-0000-0000-000000000000"
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _configuration?.Dispose();
            CorrelationContext.Current = null!;
            Activity.Current = null;
        }

        [TestMethod]
        public void Constructor_NullTelemetryClient_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new ApplicationInsightsBridge(null!));
        }

        [TestMethod]
        public void Constructor_ValidClient_DoesNotThrow()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client);
            Assert.IsNotNull(bridge);
        }

        [TestMethod]
        public void IsOtlpMode_ForceTrue_ReturnsTrue()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: true);
            Assert.IsTrue(bridge.IsOtlpMode);
        }

        [TestMethod]
        public void IsOtlpMode_ForceFalse_ReturnsFalse()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);
            Assert.IsFalse(bridge.IsOtlpMode);
        }

        [TestMethod]
        public void TrackRequest_DirectMode_SendsTelemetry()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);

            bridge.TrackRequest(
                "TestOperation",
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(100),
                "200",
                true);

            Assert.IsTrue(_channel!.SentItems.Count > 0);
            var request = _channel.SentItems[0] as RequestTelemetry;
            Assert.IsNotNull(request);
            Assert.AreEqual("TestOperation", request!.Name);
            Assert.AreEqual("200", request.ResponseCode);
            Assert.AreEqual(true, request.Success);
        }

        [TestMethod]
        public void TrackRequest_OtlpMode_IsNoOp()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: true);

            bridge.TrackRequest(
                "TestOperation",
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(100),
                "200",
                true);

            Assert.AreEqual(0, _channel!.SentItems.Count);
        }

        [TestMethod]
        public void TrackDependency_DirectMode_SendsTelemetry()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);

            bridge.TrackDependency(
                "SQL",
                "localhost",
                "SELECT * FROM Orders",
                "SELECT",
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(50),
                "0",
                true);

            Assert.IsTrue(_channel!.SentItems.Count > 0);
            var dependency = _channel.SentItems[0] as DependencyTelemetry;
            Assert.IsNotNull(dependency);
            Assert.AreEqual("SQL", dependency!.Type);
            Assert.AreEqual("localhost", dependency.Target);
        }

        [TestMethod]
        public void TrackDependency_OtlpMode_IsNoOp()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: true);

            bridge.TrackDependency(
                "SQL", "localhost", "SELECT", "data",
                DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(50), "0", true);

            Assert.AreEqual(0, _channel!.SentItems.Count);
        }

        [TestMethod]
        public void TrackMetric_DirectMode_SendsTelemetry()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);

            bridge.TrackMetric("test.metric", 42.5);

            Assert.IsTrue(_channel!.SentItems.Count > 0);
        }

        [TestMethod]
        public void TrackMetric_OtlpMode_IsNoOp()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: true);

            bridge.TrackMetric("test.metric", 42.5);

            Assert.AreEqual(0, _channel!.SentItems.Count);
        }

        [TestMethod]
        public void TrackException_NullException_ThrowsArgumentNullException()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);

            Assert.ThrowsExactly<ArgumentNullException>(
                () => bridge.TrackException(null!));
        }

        [TestMethod]
        public void TrackException_DirectMode_SendsTelemetry()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);

            bridge.TrackException(new InvalidOperationException("test error"));

            Assert.IsTrue(_channel!.SentItems.Count > 0);
            var exception = _channel.SentItems[0] as ExceptionTelemetry;
            Assert.IsNotNull(exception);
            Assert.AreEqual("test error", exception!.Exception.Message);
        }

        [TestMethod]
        public void TrackException_OtlpMode_IsNoOp()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: true);

            bridge.TrackException(new InvalidOperationException("test error"));

            Assert.AreEqual(0, _channel!.SentItems.Count);
        }

        [TestMethod]
        public void TrackRequest_DirectMode_EnrichesWithCorrelationId()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);

            using (CorrelationContext.BeginScope("bridge-corr-123"))
            {
                bridge.TrackRequest(
                    "TestOp",
                    DateTimeOffset.UtcNow,
                    TimeSpan.FromMilliseconds(10),
                    "200",
                    true);
            }

            var request = _channel!.SentItems[0] as RequestTelemetry;
            Assert.IsNotNull(request);
            Assert.AreEqual("bridge-corr-123", request!.Properties["CorrelationId"]);
        }

        [TestMethod]
        public void TrackRequest_DirectMode_EnrichesWithActivityTags()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);

            using var activity = new Activity("test-op")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            activity.SetTag("custom.tag", "custom-value");

            bridge.TrackRequest(
                "TestOp",
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(10),
                "200",
                true);

            var request = _channel!.SentItems[0] as RequestTelemetry;
            Assert.IsNotNull(request);
            Assert.AreEqual("custom-value", request!.Properties["custom.tag"]);
        }

        [TestMethod]
        public void Flush_DoesNotThrow()
        {
            var client = new TelemetryClient(_configuration!);
            using var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);

            bridge.Flush();
            Assert.IsTrue(_channel!.FlushCalled);
        }

        [TestMethod]
        public void Dispose_FlushesOnDispose()
        {
            var client = new TelemetryClient(_configuration!);
            var bridge = new ApplicationInsightsBridge(client, forceOtlpMode: false);

            bridge.Dispose();

            Assert.IsTrue(_channel!.FlushCalled);
        }

        [TestMethod]
        public void DetectOtlpMode_NoEnvironmentVariable_ReturnsFalse()
        {
            // Save and clear the env var if it exists
            var original = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            try
            {
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
                Assert.IsFalse(ApplicationInsightsBridge.DetectOtlpMode());
            }
            finally
            {
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", original);
            }
        }

        [TestMethod]
        public void DetectOtlpMode_WithEnvironmentVariable_ReturnsTrue()
        {
            var original = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            try
            {
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");
                Assert.IsTrue(ApplicationInsightsBridge.DetectOtlpMode());
            }
            finally
            {
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", original);
            }
        }

        /// <summary>
        /// Stub telemetry channel for capturing sent telemetry items in tests.
        /// </summary>
        private sealed class StubTelemetryChannel : ITelemetryChannel
        {
            public System.Collections.Generic.List<ITelemetry> SentItems { get; } = new System.Collections.Generic.List<ITelemetry>();
            public bool FlushCalled { get; private set; }
            public bool? DeveloperMode { get; set; }
            public string? EndpointAddress { get; set; }

            public void Dispose() { }

            public void Flush()
            {
                FlushCalled = true;
            }

            public void Send(ITelemetry item)
            {
                SentItems.Add(item);
            }
        }
    }
}
