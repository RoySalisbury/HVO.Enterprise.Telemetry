using System;
using HVO.Enterprise.Telemetry.Data.Common;

namespace HVO.Enterprise.Telemetry.Data.Tests
{
    [TestClass]
    public class SqlOperationDetectorTests
    {
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
        [DataRow("  INSERT INTO Users VALUES (@p0)", "INSERT")]
        [DataRow("  SELECT * FROM Users", "SELECT")]
        [DataRow("execute sp_GetUsers", "EXECUTE")]
        [DataRow("insert into users values (1)", "INSERT")]
        [DataRow("select 1", "SELECT")]
        [DataRow("UNKNOWN_COMMAND stuff", "EXECUTE")]
        public void DetectOperation_VariousCommands_ReturnsExpected(string? commandText, string expected)
        {
            // Act
            var result = SqlOperationDetector.DetectOperation(commandText);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
