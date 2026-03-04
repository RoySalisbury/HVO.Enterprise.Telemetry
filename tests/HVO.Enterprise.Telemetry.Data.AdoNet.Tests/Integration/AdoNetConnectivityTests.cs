using System;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using HVO.Enterprise.Telemetry.Data.AdoNet;
using HVO.Enterprise.Telemetry.Data.AdoNet.Configuration;
using HVO.Enterprise.Telemetry.Data.AdoNet.Instrumentation;
using Npgsql;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Tests.Integration
{
    /// <summary>
    /// Integration tests that exercise <see cref="InstrumentedDbConnection"/> and
    /// <see cref="InstrumentedDbCommand"/> against a real PostgreSQL database.
    /// These tests are skipped automatically when PostgreSQL is not reachable.
    /// Start PostgreSQL via: <c>docker compose -f docker-compose.test.yml up -d postgres</c>
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class AdoNetConnectivityTests
    {
        private static readonly string _host =
            Environment.GetEnvironmentVariable("HVO_POSTGRES_HOST_LOCAL") ?? "127.0.0.1";
        private static readonly int _port =
            int.TryParse(Environment.GetEnvironmentVariable("HVO_POSTGRES_PORT"), out var p) ? p : 5432;
        private static readonly string _database =
            Environment.GetEnvironmentVariable("HVO_POSTGRES_DATABASE") ?? "hvo";
        private static readonly string _username =
            Environment.GetEnvironmentVariable("HVO_POSTGRES_USERNAME") ?? "hvo_dev";
        private static readonly string _password =
            Environment.GetEnvironmentVariable("HVO_POSTGRES_PASSWORD") ?? "hvo_dev_password";

        private string ConnectionString =>
            $"Host={_host};Port={_port};Database={_database};Username={_username};Password={_password};Timeout=5";

        private static bool IsPostgresAvailable()
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
            if (!IsPostgresAvailable())
            {
                Assert.Inconclusive(
                    $"PostgreSQL is not reachable at {_host}:{_port}. " +
                    "Start it with: docker compose -f docker-compose.test.yml up -d postgres");
            }
        }

        [TestMethod]
        public void Postgres_TcpConnectivity_ReachesPort()
        {
            SkipIfUnavailable();

            using var tcp = new TcpClient();
            tcp.Connect(_host, _port);

            Assert.IsTrue(tcp.Connected, "Should be connected to PostgreSQL port");
        }

        [TestMethod]
        public void InstrumentedDbConnection_Open_ConnectsToPostgres()
        {
            SkipIfUnavailable();

            using var inner = new NpgsqlConnection(ConnectionString);
            using var instrumented = new InstrumentedDbConnection(inner);

            instrumented.Open();

            Assert.AreEqual(ConnectionState.Open, instrumented.State);
        }

        [TestMethod]
        public void InstrumentedDbConnection_ExecuteScalar_ReturnsResult()
        {
            SkipIfUnavailable();

            using var inner = new NpgsqlConnection(ConnectionString);
            using var instrumented = new InstrumentedDbConnection(inner);

            instrumented.Open();

            using var cmd = instrumented.CreateCommand();
            cmd.CommandText = "SELECT 1";

            var result = cmd.ExecuteScalar();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, Convert.ToInt32(result));
        }

        [TestMethod]
        public void InstrumentedDbConnection_WithActivity_CreatesSpansForCommands()
        {
            SkipIfUnavailable();

            var activitiesStarted = 0;

            using var listener = new ActivityListener
            {
                ShouldListenTo = src => src.Name == AdoNetActivitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = _ => activitiesStarted++
            };
            ActivitySource.AddActivityListener(listener);

            var options = new AdoNetTelemetryOptions
            {
                RecordStatements = true,
                RecordConnectionInfo = false
            };

            using var inner = new NpgsqlConnection(ConnectionString);
            using var instrumented = new InstrumentedDbConnection(inner, options);

            instrumented.Open();

            using var cmd = instrumented.CreateCommand();
            cmd.CommandText = "SELECT current_timestamp";
            cmd.ExecuteScalar();

            Assert.IsTrue(activitiesStarted > 0,
                "At least one activity should be created for the query");
        }

        [TestMethod]
        public void InstrumentedDbConnection_RecordStatementsFalse_DoesNotThrow()
        {
            SkipIfUnavailable();

            var options = new AdoNetTelemetryOptions { RecordStatements = false };

            using var inner = new NpgsqlConnection(ConnectionString);
            using var instrumented = new InstrumentedDbConnection(inner, options);

            instrumented.Open();

            using var cmd = instrumented.CreateCommand();
            cmd.CommandText = "SELECT 42";
            var result = cmd.ExecuteScalar();

            Assert.AreEqual(42, Convert.ToInt32(result));
        }
    }
}
