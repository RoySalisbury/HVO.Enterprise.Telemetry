using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HVO.Enterprise.Telemetry.Datadog.Tests
{
    [TestClass]
    public class DatadogTraceExporterTests
    {
        [TestMethod]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DatadogTraceExporter(null!));
        }

        [TestMethod]
        public void Constructor_ValidOptions_CreatesInstance()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            Assert.IsNotNull(exporter);
        }

        // --- EnrichActivity ---

        [TestMethod]
        public void EnrichActivity_NullActivity_ThrowsArgumentNullException()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            Assert.ThrowsExactly<ArgumentNullException>(
                () => exporter.EnrichActivity(null!));
        }

        [TestMethod]
        public void EnrichActivity_WithServiceName_AddsTag()
        {
            var options = new DatadogOptions { ServiceName = "my-service" };
            var exporter = new DatadogTraceExporter(options);

            using var activity = new Activity("test").Start();
            exporter.EnrichActivity(activity);

            Assert.AreEqual("my-service", activity.GetTagItem("service.name"));
        }

        [TestMethod]
        public void EnrichActivity_WithEnvironment_AddsTag()
        {
            var options = new DatadogOptions { Environment = "staging" };
            var exporter = new DatadogTraceExporter(options);

            using var activity = new Activity("test").Start();
            exporter.EnrichActivity(activity);

            Assert.AreEqual("staging", activity.GetTagItem("env"));
        }

        [TestMethod]
        public void EnrichActivity_WithVersion_AddsTag()
        {
            var options = new DatadogOptions { Version = "1.2.3" };
            var exporter = new DatadogTraceExporter(options);

            using var activity = new Activity("test").Start();
            exporter.EnrichActivity(activity);

            Assert.AreEqual("1.2.3", activity.GetTagItem("version"));
        }

        [TestMethod]
        public void EnrichActivity_WithAllUnifiedTags_AddsAllTags()
        {
            var options = new DatadogOptions
            {
                ServiceName = "my-service",
                Environment = "production",
                Version = "2.0.0"
            };
            var exporter = new DatadogTraceExporter(options);

            using var activity = new Activity("test").Start();
            exporter.EnrichActivity(activity);

            Assert.AreEqual("my-service", activity.GetTagItem("service.name"));
            Assert.AreEqual("production", activity.GetTagItem("env"));
            Assert.AreEqual("2.0.0", activity.GetTagItem("version"));
        }

        [TestMethod]
        public void EnrichActivity_WithGlobalTags_AddsTags()
        {
            var options = new DatadogOptions
            {
                GlobalTags = new Dictionary<string, string>
                {
                    ["team"] = "backend",
                    ["region"] = "us-east-1"
                }
            };
            var exporter = new DatadogTraceExporter(options);

            using var activity = new Activity("test").Start();
            exporter.EnrichActivity(activity);

            Assert.AreEqual("backend", activity.GetTagItem("team"));
            Assert.AreEqual("us-east-1", activity.GetTagItem("region"));
        }

        [TestMethod]
        public void EnrichActivity_GlobalTagsDoNotOverwriteExisting()
        {
            var options = new DatadogOptions
            {
                GlobalTags = new Dictionary<string, string>
                {
                    ["existing-tag"] = "new-value"
                }
            };
            var exporter = new DatadogTraceExporter(options);

            using var activity = new Activity("test").Start();
            activity.SetTag("existing-tag", "original-value");

            exporter.EnrichActivity(activity);

            Assert.AreEqual("original-value", activity.GetTagItem("existing-tag"));
        }

        [TestMethod]
        public void EnrichActivity_WithNoOptions_DoesNotThrow()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            using var activity = new Activity("test").Start();

            exporter.EnrichActivity(activity); // Should not throw
        }

        // --- CreatePropagationHeaders ---

        [TestMethod]
        public void CreatePropagationHeaders_NoActivity_ReturnsEmptyDictionary()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());

            // Ensure no current activity
            Activity.Current = null;
            var headers = exporter.CreatePropagationHeaders(null);

            Assert.AreEqual(0, headers.Count);
        }

        [TestMethod]
        public void CreatePropagationHeaders_WithW3CActivity_ContainsTraceparent()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());

            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var headers = exporter.CreatePropagationHeaders(activity);

            Assert.IsTrue(headers.ContainsKey("traceparent"));
        }

        [TestMethod]
        public void CreatePropagationHeaders_WithW3CActivity_ContainsDatadogHeaders()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());

            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var headers = exporter.CreatePropagationHeaders(activity);

            Assert.IsTrue(headers.ContainsKey("x-datadog-trace-id"));
            Assert.IsTrue(headers.ContainsKey("x-datadog-parent-id"));
        }

        [TestMethod]
        public void CreatePropagationHeaders_DatadogTraceId_IsDecimal()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());

            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var headers = exporter.CreatePropagationHeaders(activity);

            Assert.IsTrue(ulong.TryParse(headers["x-datadog-trace-id"], out _),
                "x-datadog-trace-id should be a decimal number");
            Assert.IsTrue(ulong.TryParse(headers["x-datadog-parent-id"], out _),
                "x-datadog-parent-id should be a decimal number");
        }

        [TestMethod]
        public void CreatePropagationHeaders_WithTraceState_IncludesTracestate()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());

            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            activity.TraceStateString = "dd=s:1;o:rum";

            var headers = exporter.CreatePropagationHeaders(activity);

            Assert.IsTrue(headers.ContainsKey("tracestate"));
            Assert.AreEqual("dd=s:1;o:rum", headers["tracestate"]);
        }

        [TestMethod]
        public void CreatePropagationHeaders_FallsBackToActivityCurrent()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());

            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            // Don't pass activity — should use Activity.Current
            var headers = exporter.CreatePropagationHeaders();

            Assert.IsTrue(headers.ContainsKey("traceparent"));
        }

        // --- ExtractTraceContext ---

        [TestMethod]
        public void ExtractTraceContext_NullHeaders_ReturnsNull()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            Assert.IsNull(exporter.ExtractTraceContext(null));
        }

        [TestMethod]
        public void ExtractTraceContext_EmptyHeaders_ReturnsNull()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            Assert.IsNull(exporter.ExtractTraceContext(new Dictionary<string, string>()));
        }

        [TestMethod]
        public void ExtractTraceContext_WithTraceparent_ReturnsW3CContext()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            var headers = new Dictionary<string, string>
            {
                ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
            };

            var result = exporter.ExtractTraceContext(headers);

            Assert.IsNotNull(result);
            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", result.Value.TraceId);
            Assert.AreEqual("00f067aa0ba902b7", result.Value.ParentId);
            Assert.AreEqual("01", result.Value.SamplingPriority);
        }

        [TestMethod]
        public void ExtractTraceContext_WithDatadogHeaders_ReturnsContext()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            var headers = new Dictionary<string, string>
            {
                ["x-datadog-trace-id"] = "12345678901234567",
                ["x-datadog-parent-id"] = "9876543210987654",
                ["x-datadog-sampling-priority"] = "1"
            };

            var result = exporter.ExtractTraceContext(headers);

            Assert.IsNotNull(result);
            Assert.AreEqual("12345678901234567", result.Value.TraceId);
            Assert.AreEqual("9876543210987654", result.Value.ParentId);
            Assert.AreEqual("1", result.Value.SamplingPriority);
        }

        [TestMethod]
        public void ExtractTraceContext_DatadogHeadersWithoutSampling_ReturnsNullSampling()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            var headers = new Dictionary<string, string>
            {
                ["x-datadog-trace-id"] = "123456",
                ["x-datadog-parent-id"] = "789012"
            };

            var result = exporter.ExtractTraceContext(headers);

            Assert.IsNotNull(result);
            Assert.IsNull(result.Value.SamplingPriority);
        }

        [TestMethod]
        public void ExtractTraceContext_PrefersW3COverDatadog()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            var headers = new Dictionary<string, string>
            {
                ["traceparent"] = "00-abcdef1234567890abcdef1234567890-1234567890abcdef-01",
                ["x-datadog-trace-id"] = "99999",
                ["x-datadog-parent-id"] = "88888"
            };

            var result = exporter.ExtractTraceContext(headers);

            Assert.IsNotNull(result);
            Assert.AreEqual("abcdef1234567890abcdef1234567890", result.Value.TraceId);
        }

        [TestMethod]
        public void ExtractTraceContext_InvalidTraceparent_FallsBackToDatadog()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            var headers = new Dictionary<string, string>
            {
                ["traceparent"] = "invalid-format",
                ["x-datadog-trace-id"] = "99999",
                ["x-datadog-parent-id"] = "88888"
            };

            var result = exporter.ExtractTraceContext(headers);

            Assert.IsNotNull(result);
            Assert.AreEqual("99999", result.Value.TraceId);
        }

        [TestMethod]
        public void ExtractTraceContext_MissingDatadogParentId_ReturnsNull()
        {
            var exporter = new DatadogTraceExporter(new DatadogOptions());
            var headers = new Dictionary<string, string>
            {
                ["x-datadog-trace-id"] = "99999"
                // Missing parent-id
            };

            Assert.IsNull(exporter.ExtractTraceContext(headers));
        }

        // --- Disabled mode ---

        [TestMethod]
        public void EnrichActivity_WhenDisabled_DoesNotAddTags()
        {
            var options = new DatadogOptions
            {
                EnableTraceExporter = false,
                ServiceName = "my-service",
                Environment = "prod"
            };
            var exporter = new DatadogTraceExporter(options);

            using var activity = new Activity("test").Start();
            exporter.EnrichActivity(activity);

            Assert.IsNull(activity.GetTagItem("service.name"));
            Assert.IsNull(activity.GetTagItem("env"));
        }

        [TestMethod]
        public void EnrichActivity_WhenDisabled_NullActivity_StillThrows()
        {
            var options = new DatadogOptions { EnableTraceExporter = false };
            var exporter = new DatadogTraceExporter(options);

            Assert.ThrowsExactly<ArgumentNullException>(
                () => exporter.EnrichActivity(null!));
        }
    }
}
