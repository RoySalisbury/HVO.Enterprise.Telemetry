using System;
using System.Data;
using System.Data.Common;
using HVO.Enterprise.Telemetry.Data.AdoNet.Configuration;
using HVO.Enterprise.Telemetry.Data.AdoNet.Instrumentation;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Tests
{
    [TestClass]
    public class InstrumentedDbConnectionTests
    {
        [TestMethod]
        public void Constructor_ValidConnection_DoesNotThrow()
        {
            // Arrange
            using var inner = new FakeDbConnection();

            // Act
            using var conn = new InstrumentedDbConnection(inner);

            // Assert
            Assert.IsNotNull(conn);
        }

        [TestMethod]
        public void Constructor_NullConnection_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new InstrumentedDbConnection(null!));
        }

        [TestMethod]
        public void InnerConnection_ReturnsWrappedConnection()
        {
            // Arrange
            using var inner = new FakeDbConnection();
            using var conn = new InstrumentedDbConnection(inner);

            // Assert
            Assert.AreSame(inner, conn.InnerConnection);
        }

        [TestMethod]
        public void ConnectionString_DelegatesToInner()
        {
            // Arrange
            using var inner = new FakeDbConnection { ConnectionString = "Server=test" };
            using var conn = new InstrumentedDbConnection(inner);

            // Assert
            Assert.AreEqual("Server=test", conn.ConnectionString);
        }

        [TestMethod]
        public void Database_DelegatesToInner()
        {
            // Arrange
            using var inner = new FakeDbConnection();
            using var conn = new InstrumentedDbConnection(inner);

            // Assert
            Assert.AreEqual("FakeDb", conn.Database);
        }

        [TestMethod]
        public void State_DelegatesToInner()
        {
            // Arrange
            using var inner = new FakeDbConnection();
            using var conn = new InstrumentedDbConnection(inner);

            // Assert
            Assert.AreEqual(ConnectionState.Closed, conn.State);
        }

        [TestMethod]
        public void Open_DelegatesToInner()
        {
            // Arrange
            using var inner = new FakeDbConnection();
            using var conn = new InstrumentedDbConnection(inner);

            // Act
            conn.Open();

            // Assert
            Assert.IsTrue(inner.WasOpened);
        }

        [TestMethod]
        public void Close_DelegatesToInner()
        {
            // Arrange
            using var inner = new FakeDbConnection();
            using var conn = new InstrumentedDbConnection(inner);

            // Act
            conn.Close();

            // Assert
            Assert.IsTrue(inner.WasClosed);
        }

        [TestMethod]
        public void CreateCommand_ReturnsInstrumentedDbCommand()
        {
            // Arrange
            using var inner = new FakeDbConnection();
            using var conn = new InstrumentedDbConnection(inner);

            // Act
            var cmd = conn.CreateCommand();

            // Assert
            Assert.IsInstanceOfType(cmd, typeof(InstrumentedDbCommand));
        }

        /// <summary>Simple in-memory fake DbConnection for testing.</summary>
        private class FakeDbConnection : DbConnection
        {
            public bool WasOpened { get; private set; }
            public bool WasClosed { get; private set; }

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member
            public override string ConnectionString { get; set; } = string.Empty;
            public override string Database => "FakeDb";
            public override string DataSource => "FakeServer";
            public override string ServerVersion => "1.0";
            public override ConnectionState State => WasOpened && !WasClosed ? ConnectionState.Open : ConnectionState.Closed;

            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { WasClosed = true; }
            public override void Open() { WasOpened = true; }
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
            protected override DbCommand CreateDbCommand() => new FakeDbCommand();
        }

        /// <summary>Simple in-memory fake DbCommand for testing.</summary>
        private class FakeDbCommand : DbCommand
        {
            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
            protected override DbTransaction? DbTransaction { get; set; }

            public override void Cancel() { }
            public override int ExecuteNonQuery() => 0;
            public override object ExecuteScalar() => 0;
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
        }
    }
}
