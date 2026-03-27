using System;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.OpenTelemetry;

namespace HVO.Enterprise.Telemetry.OpenTelemetry.Tests
{
    [TestClass]
    public class OtlpExportOptionsTests
    {
        [TestMethod]
        public void Defaults_AreCorrect()
        {
            var options = new OtlpExportOptions();

            Assert.AreEqual("http://localhost:4318", options.Endpoint);
            Assert.AreEqual(OtlpTransport.HttpProtobuf, options.Transport);
            Assert.IsTrue(options.EnableTraceExport);
            Assert.IsTrue(options.EnableMetricsExport);
            Assert.IsFalse(options.EnableLogExport);
            Assert.IsFalse(options.EnablePrometheusEndpoint);
            Assert.AreEqual("/metrics", options.PrometheusEndpointPath);
            Assert.AreEqual(TimeSpan.FromSeconds(60), options.MetricsExportInterval);
            Assert.AreEqual(TimeSpan.FromSeconds(5), options.TraceBatchExportDelay);
            Assert.AreEqual(512, options.TraceBatchMaxSize);
            Assert.AreEqual(2048, options.TraceBatchMaxQueueSize);
            Assert.AreEqual(MetricsTemporality.Cumulative, options.TemporalityPreference);
            Assert.IsNotNull(options.ResourceAttributes);
            Assert.AreEqual(0, options.ResourceAttributes.Count);
            Assert.IsNotNull(options.Headers);
            Assert.AreEqual(0, options.Headers.Count);
            Assert.IsNull(options.ServiceName);
            Assert.IsNull(options.ServiceVersion);
            Assert.IsNull(options.Environment);
            Assert.IsNotNull(options.AdditionalActivitySources);
            Assert.AreEqual(0, options.AdditionalActivitySources.Count);
            Assert.IsNotNull(options.AdditionalMeterNames);
            Assert.AreEqual(0, options.AdditionalMeterNames.Count);
            Assert.IsFalse(options.EnableStandardMeters);
            Assert.IsNull(options.ConfigureTracerProvider);
            Assert.IsNull(options.ConfigureMeterProvider);
        }

        [TestMethod]
        public void Properties_CanBeSet()
        {
            var options = new OtlpExportOptions
            {
                Endpoint = "http://collector:4317",
                Transport = OtlpTransport.HttpProtobuf,
                ServiceName = "my-service",
                ServiceVersion = "2.0.0",
                Environment = "production",
                EnableTraceExport = false,
                EnableMetricsExport = false,
                EnableLogExport = true,
                EnablePrometheusEndpoint = true,
                PrometheusEndpointPath = "/custom-metrics",
                MetricsExportInterval = TimeSpan.FromSeconds(30),
                TraceBatchExportDelay = TimeSpan.FromSeconds(10),
                TraceBatchMaxSize = 1024,
                TraceBatchMaxQueueSize = 4096,
                TemporalityPreference = MetricsTemporality.Delta,
            };

            Assert.AreEqual("http://collector:4317", options.Endpoint);
            Assert.AreEqual(OtlpTransport.HttpProtobuf, options.Transport);
            Assert.AreEqual("my-service", options.ServiceName);
            Assert.AreEqual("2.0.0", options.ServiceVersion);
            Assert.AreEqual("production", options.Environment);
            Assert.IsFalse(options.EnableTraceExport);
            Assert.IsFalse(options.EnableMetricsExport);
            Assert.IsTrue(options.EnableLogExport);
            Assert.IsTrue(options.EnablePrometheusEndpoint);
            Assert.AreEqual("/custom-metrics", options.PrometheusEndpointPath);
            Assert.AreEqual(TimeSpan.FromSeconds(30), options.MetricsExportInterval);
            Assert.AreEqual(TimeSpan.FromSeconds(10), options.TraceBatchExportDelay);
            Assert.AreEqual(1024, options.TraceBatchMaxSize);
            Assert.AreEqual(4096, options.TraceBatchMaxQueueSize);
            Assert.AreEqual(MetricsTemporality.Delta, options.TemporalityPreference);
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_SetsEndpointFromEnv()
        {
            var options = new OtlpExportOptions();
            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318");

            try
            {
                options.ApplyEnvironmentDefaults();
                Assert.AreEqual("http://collector:4318", options.Endpoint);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ExplicitEndpointTakesPrecedence()
        {
            var options = new OtlpExportOptions { Endpoint = "http://custom:4318" };
            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://env:4318");

            try
            {
                options.ApplyEnvironmentDefaults();
                Assert.AreEqual("http://custom:4318", options.Endpoint);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_SetsServiceNameFromEnv()
        {
            var options = new OtlpExportOptions();
            System.Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "env-service");

            try
            {
                options.ApplyEnvironmentDefaults();
                Assert.AreEqual("env-service", options.ServiceName);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ExplicitServiceNameTakesPrecedence()
        {
            var options = new OtlpExportOptions { ServiceName = "explicit-service" };
            System.Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "env-service");

            try
            {
                options.ApplyEnvironmentDefaults();
                Assert.AreEqual("explicit-service", options.ServiceName);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ParsesResourceAttributes()
        {
            System.Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES",
                "deployment.environment=staging,team=platform");

            try
            {
                var options = new OtlpExportOptions();
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual("staging", options.Environment);
                Assert.IsTrue(options.ResourceAttributes.ContainsKey("team"));
                Assert.AreEqual("platform", options.ResourceAttributes["team"]);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ExplicitEnvironmentTakesPrecedence()
        {
            var options = new OtlpExportOptions { Environment = "production" };
            System.Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES",
                "deployment.environment=staging");

            try
            {
                options.ApplyEnvironmentDefaults();
                Assert.AreEqual("production", options.Environment);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ParsesHeaders()
        {
            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS",
                "api-key=secret123,x-custom=value");

            try
            {
                var options = new OtlpExportOptions();
                options.ApplyEnvironmentDefaults();

                Assert.AreEqual("secret123", options.Headers["api-key"]);
                Assert.AreEqual("value", options.Headers["x-custom"]);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ExistingHeadersNotOverwritten()
        {
            var options = new OtlpExportOptions();
            options.Headers["api-key"] = "original";
            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS",
                "api-key=from-env");

            try
            {
                options.ApplyEnvironmentDefaults();
                Assert.AreEqual("original", options.Headers["api-key"]);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_NoEnvVars_KeepsDefaults()
        {
            // Ensure no OTel env vars are set
            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            System.Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", null);
            System.Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", null);
            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", null);

            var options = new OtlpExportOptions();
            options.ApplyEnvironmentDefaults();

            Assert.AreEqual("http://localhost:4318", options.Endpoint);
            Assert.IsNull(options.ServiceName);
            Assert.IsNull(options.Environment);
            Assert.AreEqual(0, options.Headers.Count);
        }

        [TestMethod]
        public void ResourceAttributes_CanBeModified()
        {
            var options = new OtlpExportOptions();
            options.ResourceAttributes["custom.key"] = "custom-value";

            Assert.AreEqual("custom-value", options.ResourceAttributes["custom.key"]);
        }

        [TestMethod]
        public void Headers_CanBeModified()
        {
            var options = new OtlpExportOptions();
            options.Headers["Authorization"] = "Bearer token123";

            Assert.AreEqual("Bearer token123", options.Headers["Authorization"]);
        }

        [TestMethod]
        public void AdditionalActivitySources_CanBeModified()
        {
            var options = new OtlpExportOptions();
            options.AdditionalActivitySources.Add("MyApp");
            options.AdditionalActivitySources.Add("MyApp.HttpClient");

            Assert.AreEqual(2, options.AdditionalActivitySources.Count);
            Assert.AreEqual("MyApp", options.AdditionalActivitySources[0]);
            Assert.AreEqual("MyApp.HttpClient", options.AdditionalActivitySources[1]);
        }

        [TestMethod]
        public void AdditionalMeterNames_CanBeModified()
        {
            var options = new OtlpExportOptions();
            options.AdditionalMeterNames.Add("MyApp.Metrics");

            Assert.AreEqual(1, options.AdditionalMeterNames.Count);
            Assert.AreEqual("MyApp.Metrics", options.AdditionalMeterNames[0]);
        }

        [TestMethod]
        public void EnableStandardMeters_CanBeSet()
        {
            var options = new OtlpExportOptions { EnableStandardMeters = true };

            Assert.IsTrue(options.EnableStandardMeters);
        }

        [TestMethod]
        public void ConfigureTracerProvider_CanBeSet()
        {
            var invoked = false;
            var options = new OtlpExportOptions
            {
                ConfigureTracerProvider = _ => invoked = true
            };

            Assert.IsNotNull(options.ConfigureTracerProvider);
            options.ConfigureTracerProvider(null!);
            Assert.IsTrue(invoked);
        }

        [TestMethod]
        public void ConfigureMeterProvider_CanBeSet()
        {
            var invoked = false;
            var options = new OtlpExportOptions
            {
                ConfigureMeterProvider = _ => invoked = true
            };

            Assert.IsNotNull(options.ConfigureMeterProvider);
            options.ConfigureMeterProvider(null!);
            Assert.IsTrue(invoked);
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_Port4318_SetsHttpProtobufTransport()
        {
            var options = new OtlpExportOptions();
            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4318");

            try
            {
                options.ApplyEnvironmentDefaults();
                Assert.AreEqual("http://collector:4318", options.Endpoint);
                Assert.AreEqual(OtlpTransport.HttpProtobuf, options.Transport);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_Port4317_AutoDetectsGrpcTransport()
        {
            var options = new OtlpExportOptions();
            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4317");

            try
            {
                options.ApplyEnvironmentDefaults();
                Assert.AreEqual("http://collector:4317", options.Endpoint);
                Assert.AreEqual(OtlpTransport.Grpc, options.Transport);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ExplicitTransport_NotOverriddenByPort()
        {
            // Explicitly set transport to HttpProtobuf (same as default, but explicitly assigned)
            // so that environment-based auto-detection should NOT change it, even though
            // the endpoint port (4317) would normally trigger auto-detection to Grpc.
            var options = new OtlpExportOptions
            {
                Transport = OtlpTransport.HttpProtobuf
            };

            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4317");

            try
            {
                options.ApplyEnvironmentDefaults();

                // Because Transport was explicitly set (even though it matches the default),
                // auto-detection should NOT override it.
                Assert.AreEqual(OtlpTransport.HttpProtobuf, options.Transport);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_ExplicitEndpointPort4317_AutoDetectsGrpcTransport()
        {
            // When endpoint is set programmatically (not via env var) to port 4317,
            // auto-detection should switch from the default HttpProtobuf to Grpc.
            var options = new OtlpExportOptions
            {
                Endpoint = "http://collector:4317"
            };

            options.ApplyEnvironmentDefaults();

            Assert.AreEqual(OtlpTransport.Grpc, options.Transport);
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_NonStandardPort_KeepsHttpProtobufTransport()
        {
            var options = new OtlpExportOptions();
            System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:9090");

            try
            {
                options.ApplyEnvironmentDefaults();
                Assert.AreEqual(OtlpTransport.HttpProtobuf, options.Transport);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            }
        }

        [TestMethod]
        public void ApplyEnvironmentDefaults_InvalidUri_KeepsHttpProtobufTransport()
        {
            var options = new OtlpExportOptions
            {
                Endpoint = "not-a-valid-uri"
            };

            options.ApplyEnvironmentDefaults();

            Assert.AreEqual(OtlpTransport.HttpProtobuf, options.Transport);
        }
    }
}
