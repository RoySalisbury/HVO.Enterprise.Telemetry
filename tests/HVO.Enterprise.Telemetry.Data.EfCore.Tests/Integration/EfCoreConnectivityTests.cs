using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using HVO.Enterprise.Telemetry.Data.EfCore;
using HVO.Enterprise.Telemetry.Data.EfCore.Configuration;
using HVO.Enterprise.Telemetry.Data.EfCore.Extensions;
using HVO.Enterprise.Telemetry.Data.EfCore.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Tests.Integration
{
    /// <summary>
    /// Integration tests that exercise the EF Core telemetry interceptor against a real
    /// PostgreSQL database. These tests are skipped automatically when PostgreSQL is not reachable.
    /// Start PostgreSQL via: <c>docker compose -f docker-compose.test.yml up -d postgres</c>
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class EfCoreConnectivityTests
    {
        private static readonly string _host =
            Environment.GetEnvironmentVariable("HVO_POSTGRES_HOST_LOCAL") ?? "127.0.0.1";
        private static readonly int _port =
            int.TryParse(Environment.GetEnvironmentVariable("HVO_POSTGRES_PORT"), out var portValue) ? portValue : 5432;
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

        private IntegrationTestDbContext CreateContext(EfCoreTelemetryOptions? options = null)
        {
            var builder = new DbContextOptionsBuilder<IntegrationTestDbContext>()
                .UseNpgsql(ConnectionString);

            builder.AddHvoTelemetry(options);

            return new IntegrationTestDbContext(builder.Options);
        }

        [TestMethod]
        public void EfCore_CanConnect_ToPostgres()
        {
            SkipIfUnavailable();

            using var ctx = CreateContext();
            var canConnect = ctx.Database.CanConnect();

            Assert.IsTrue(canConnect, "EF Core should be able to connect to PostgreSQL");
        }

        [TestMethod]
        public void EfCore_RawSqlQuery_ReturnsResult()
        {
            SkipIfUnavailable();

            using var ctx = CreateContext();

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection — test-only constant query
            var result = ctx.Database
                .SqlQueryRaw<int>("SELECT 1 AS \"Value\"")
                .ToList();
#pragma warning restore EF1002

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0]);
        }

        [TestMethod]
        public void EfCore_WithTelemetryInterceptor_CreatesActivitiesForQueries()
        {
            SkipIfUnavailable();

            var activitiesStarted = 0;

            using var listener = new ActivityListener
            {
                ShouldListenTo = src => src.Name == EfCoreActivitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = _ => activitiesStarted++
            };
            ActivitySource.AddActivityListener(listener);

            var options = new EfCoreTelemetryOptions { RecordStatements = true };

            using var ctx = CreateContext(options);

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection — test-only constant query
            ctx.Database
                .SqlQueryRaw<int>("SELECT 1 AS \"Value\"")
                .ToList();
#pragma warning restore EF1002

            Assert.IsTrue(activitiesStarted > 0,
                "At least one activity should be created for the EF Core query");
        }

        [TestMethod]
        public void EfCore_RecordStatementsFalse_ExecutesWithoutThrow()
        {
            SkipIfUnavailable();

            var options = new EfCoreTelemetryOptions { RecordStatements = false };

            using var ctx = CreateContext(options);
            var canConnect = ctx.Database.CanConnect();

            Assert.IsTrue(canConnect);
        }
    }
}
