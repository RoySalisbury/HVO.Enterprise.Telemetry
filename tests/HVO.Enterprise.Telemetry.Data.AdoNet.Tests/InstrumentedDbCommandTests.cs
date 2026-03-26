using System;
using HVO.Enterprise.Telemetry.Data.AdoNet;
using HVO.Enterprise.Telemetry.Data.AdoNet.Instrumentation;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Tests
{
    [TestClass]
    public class InstrumentedDbCommandTests
    {
        /// <summary>
        /// Verifies that <see cref="InstrumentedDbCommand.DetectOperation"/> delegates to
        /// the shared <see cref="HVO.Enterprise.Telemetry.Data.Common.SqlOperationDetector"/>
        /// and returns correct results for all supported SQL operation types.
        /// </summary>
        [TestMethod]
        [DataRow(null, "EXECUTE")]
        [DataRow("", "EXECUTE")]
        [DataRow("   ", "EXECUTE")]
        [DataRow("INSERT INTO Users VALUES (@p0)", "INSERT")]
        [DataRow("UPDATE Users SET Name = @p0", "UPDATE")]
        [DataRow("DELETE FROM Users WHERE Id = @p0", "DELETE")]
        [DataRow("SELECT * FROM Users", "SELECT")]
        [DataRow("MERGE INTO Target USING Source", "MERGE")]
        [DataRow("CREATE TABLE Foo (Id INT)", "CREATE")]
        [DataRow("ALTER TABLE Foo ADD Bar INT", "ALTER")]
        [DataRow("DROP TABLE Foo", "DROP")]
        [DataRow("EXEC sp_GetUsers", "EXECUTE")]
        [DataRow("EXECUTE sp_GetUsers", "EXECUTE")]
        [DataRow("  SELECT * FROM Users", "SELECT")]
        public void DetectOperation_VariousCommands_ReturnsExpected(string? commandText, string expected)
        {
            // Act
            var result = InstrumentedDbCommand.DetectOperation(commandText);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Constructor_NullCommand_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new InstrumentedDbCommand(null!));
        }

        [TestMethod]
        public void AdoNetActivitySource_HasExpectedName()
        {
            Assert.AreEqual("HVO.Enterprise.Telemetry.Data.AdoNet", AdoNetActivitySource.Name);
        }

        [TestMethod]
        public void AdoNetActivitySource_SourceNotNull()
        {
            Assert.IsNotNull(AdoNetActivitySource.Source);
        }
    }
}
