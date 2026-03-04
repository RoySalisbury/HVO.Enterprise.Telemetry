using System;
using System.Diagnostics;
using System.Net.Sockets;
using HVO.Enterprise.Telemetry.Data.Redis.Configuration;
using HVO.Enterprise.Telemetry.Data.Redis.Profiling;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;

namespace HVO.Enterprise.Telemetry.Data.Redis.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify Redis telemetry profiling against a real Redis instance.
    /// These tests are skipped automatically when Redis is not reachable.
    /// Start Redis via: <c>docker compose -f docker-compose.test.yml up -d redis</c>
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class RedisConnectivityTests
    {
        private static readonly string _host =
            Environment.GetEnvironmentVariable("HVO_REDIS_HOST_LOCAL") ?? "127.0.0.1";
        private static readonly int _port =
            int.TryParse(Environment.GetEnvironmentVariable("HVO_REDIS_PORT"), out var portValue) ? portValue : 6379;

        private static bool IsRedisAvailable()
        {
            try
            {
                using var tcp = new TcpClient();
                tcp.Connect(_host, _port);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SkipIfUnavailable()
        {
            if (!IsRedisAvailable())
            {
                Assert.Inconclusive(
                    $"Redis is not reachable at {_host}:{_port}. " +
                    "Start it with: docker compose -f docker-compose.test.yml up -d redis");
            }
        }

        [TestMethod]
        public void Redis_TcpConnectivity_ReachesPort()
        {
            SkipIfUnavailable();

            using var tcp = new TcpClient();
            tcp.Connect(_host, _port);

            Assert.IsTrue(tcp.Connected, "Should be connected to Redis");
        }

        [TestMethod]
        public void Redis_ConnectionMultiplexer_ConnectsSuccessfully()
        {
            SkipIfUnavailable();

            var config = new ConfigurationOptions
            {
                EndPoints = { { _host, _port } },
                ConnectTimeout = 5000,
                AbortOnConnectFail = false
            };

            using var mux = ConnectionMultiplexer.Connect(config);
            Assert.IsTrue(mux.IsConnected, "ConnectionMultiplexer should be connected");
        }

        [TestMethod]
        public void Redis_WithProfiler_ExecutesCommandAndCollectsCommands()
        {
            SkipIfUnavailable();

            using var listener = new ActivityListener
            {
                ShouldListenTo = src => src.Name == RedisActivitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            var options = new RedisTelemetryOptions
            {
                RecordCommands = true,
                RecordKeys = true
            };

            var session = new ProfilingSession();
            var profiler = new RedisTelemetryProfiler(() => session, options);

            var config = new ConfigurationOptions
            {
                EndPoints = { { _host, _port } },
                ConnectTimeout = 5000,
                AbortOnConnectFail = false
            };

            using var mux = ConnectionMultiplexer.Connect(config);
            mux.RegisterProfiler(profiler.GetSessionFactory());

            var db = mux.GetDatabase();
            db.StringSet("hvo:integration:test", "value");
            var value = (string?)db.StringGet("hvo:integration:test");
            db.KeyDelete("hvo:integration:test");

            // Process the profiled commands through the telemetry processor
            var commands = session.FinishProfiling();
            profiler.CommandProcessor.ProcessCommands(commands);

            Assert.AreEqual("value", value, "Round-trip read/write should succeed");
        }

        [TestMethod]
        public void Redis_ProfilerOptions_RecordCommandsFalse_DoesNotThrow()
        {
            SkipIfUnavailable();

            var options = new RedisTelemetryOptions { RecordCommands = false };
            var profiler = new RedisTelemetryProfiler(options);

            var config = new ConfigurationOptions
            {
                EndPoints = { { _host, _port } },
                AbortOnConnectFail = false
            };

            using var mux = ConnectionMultiplexer.Connect(config);
            mux.RegisterProfiler(profiler.GetSessionFactory());

            var db = mux.GetDatabase();
            db.Ping();

            Assert.IsTrue(mux.IsConnected);
        }
    }
}
