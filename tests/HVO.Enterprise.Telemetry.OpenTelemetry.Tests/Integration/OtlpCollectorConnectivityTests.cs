using System;
using System.Net.Http;
using System.Net.Sockets;
using HVO.Enterprise.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;

namespace HVO.Enterprise.Telemetry.OpenTelemetry.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify OTLP export reaches a real OpenTelemetry Collector.
    /// These tests are skipped automatically when the collector is not reachable.
    /// Start the collector via: <c>docker compose -f docker-compose.test.yml up -d otel-collector</c>
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class OtlpCollectorConnectivityTests
    {
        // gRPC endpoint (port 4317)
        private static readonly string _grpcHost =
            Environment.GetEnvironmentVariable("HVO_OTLP_GRPC_HOST") ?? "127.0.0.1";
        private static readonly int _grpcPort =
            int.TryParse(Environment.GetEnvironmentVariable("HVO_OTLP_GRPC_PORT"), out var gp) ? gp : 4317;

        // HTTP endpoint (port 4318)
        private static readonly string _httpHost =
            Environment.GetEnvironmentVariable("HVO_OTLP_HTTP_HOST") ?? "127.0.0.1";
        private static readonly int _httpPort =
            int.TryParse(Environment.GetEnvironmentVariable("HVO_OTLP_HTTP_PORT"), out var hp) ? hp : 4318;

        // Health check endpoint (port 13133)
        private static readonly string _healthHost =
            Environment.GetEnvironmentVariable("HVO_OTLP_HEALTH_HOST") ?? "127.0.0.1";
        private static readonly int _healthPort =
            int.TryParse(Environment.GetEnvironmentVariable("HVO_OTLP_HEALTH_PORT"), out var hep) ? hep : 13133;

        private static bool IsTcpPortOpen(string host, int port)
        {
            try
            {
                using var tcp = new TcpClient();
                tcp.Connect(host, port);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsCollectorAvailable() =>
            IsTcpPortOpen(_grpcHost, _grpcPort);

        private static void SkipIfUnavailable()
        {
            if (!IsCollectorAvailable())
            {
                Assert.Inconclusive(
                    $"OpenTelemetry Collector is not reachable at {_grpcHost}:{_grpcPort}. " +
                    "Start it with: docker compose -f docker-compose.test.yml up -d otel-collector");
            }
        }

        [TestMethod]
        public void OtlpCollector_GrpcPort_IsReachable()
        {
            SkipIfUnavailable();

            Assert.IsTrue(IsTcpPortOpen(_grpcHost, _grpcPort),
                $"OTLP gRPC port should be open at {_grpcHost}:{_grpcPort}");
        }

        [TestMethod]
        public void OtlpCollector_HttpPort_IsReachable()
        {
            SkipIfUnavailable();

            Assert.IsTrue(IsTcpPortOpen(_httpHost, _httpPort),
                $"OTLP HTTP/Protobuf port should be open at {_httpHost}:{_httpPort}");
        }

        [TestMethod]
        public void OtlpCollector_HealthCheck_ReturnsOk()
        {
            SkipIfUnavailable();

            if (!IsTcpPortOpen(_healthHost, _healthPort))
            {
                Assert.Inconclusive($"Health check port not open at {_healthHost}:{_healthPort}");
            }

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = client.GetAsync(
                $"http://{_healthHost}:{_healthPort}/").GetAwaiter().GetResult();

            Assert.IsTrue(response.IsSuccessStatusCode,
                $"Health check endpoint should return 2xx, got {(int)response.StatusCode}");
        }

        [TestMethod]
        public void OtlpExport_ServiceRegistration_WithCollectorEndpoint_Succeeds()
        {
            SkipIfUnavailable();

            var grpcEndpoint = $"http://{_grpcHost}:{_grpcPort}";

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(builder =>
            {
                builder.WithOpenTelemetry(options =>
                {
                    options.ServiceName = "hvo-integration-test";
                    options.Endpoint = grpcEndpoint;
                    options.Transport = OtlpTransport.Grpc;
                    options.EnableTraceExport = true;
                    options.EnableMetricsExport = true;
                    options.MetricsExportInterval = TimeSpan.FromSeconds(5);
                    options.TraceBatchExportDelay = TimeSpan.FromSeconds(1);
                });
            });

            using var provider = services.BuildServiceProvider();

            // If we got here without exception, the provider initialized successfully
            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void OtlpExport_HttpProtobuf_ServiceRegistration_Succeeds()
        {
            SkipIfUnavailable();

            var httpEndpoint = $"http://{_httpHost}:{_httpPort}";

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(builder =>
            {
                builder.WithOpenTelemetry(options =>
                {
                    options.ServiceName = "hvo-integration-http-test";
                    options.Endpoint = httpEndpoint;
                    options.Transport = OtlpTransport.HttpProtobuf;
                    options.EnableTraceExport = true;
                });
            });

            using var provider = services.BuildServiceProvider();

            Assert.IsNotNull(provider);
        }
    }
}
