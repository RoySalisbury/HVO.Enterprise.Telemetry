using System;
using System.Data.Common;
using HVO.Enterprise.Telemetry.Data.AdoNet.Configuration;
using HVO.Enterprise.Telemetry.Data.AdoNet.Extensions;
using HVO.Enterprise.Telemetry.Data.AdoNet.Instrumentation;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Tests
{
    [TestClass]
    public class DbConnectionExtensionsTests
    {
        [TestMethod]
        public void WithTelemetry_ValidConnection_ReturnsInstrumented()
        {
            // Arrange
            using var inner = new FakeDbConnection();

            // Act
            using var result = inner.WithTelemetry();

            // Assert
            Assert.IsInstanceOfType(result, typeof(InstrumentedDbConnection));
        }

        [TestMethod]
        public void WithTelemetry_AlreadyInstrumented_DoesNotDoubleWrap()
        {
            // Arrange
            using var inner = new FakeDbConnection();
            using var instrumented = new InstrumentedDbConnection(inner);

            // Act
            using var result = instrumented.WithTelemetry();

            // Assert
            Assert.AreSame(instrumented, result);
        }

        [TestMethod]
        public void WithTelemetry_NullConnection_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => DbConnectionExtensions.WithTelemetry(null!));
        }

        private class FakeDbConnection : DbConnection
        {
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member
            public override string ConnectionString { get; set; } = string.Empty;
            public override string Database => "FakeDb";
            public override string DataSource => "FakeServer";
            public override string ServerVersion => "1.0";
            public override System.Data.ConnectionState State => System.Data.ConnectionState.Closed;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => throw new NotSupportedException();
            protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
        }
    }
}
