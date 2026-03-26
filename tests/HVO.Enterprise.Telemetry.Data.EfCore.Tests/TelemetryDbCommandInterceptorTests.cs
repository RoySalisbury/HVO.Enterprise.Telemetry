using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Data.EfCore;
using HVO.Enterprise.Telemetry.Data.EfCore.Configuration;
using HVO.Enterprise.Telemetry.Data.EfCore.Interceptors;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Tests
{
    [TestClass]
    public class TelemetryDbCommandInterceptorTests
    {
        [TestMethod]
        public void Constructor_DefaultOptions_DoesNotThrow()
        {
            // Act
            var interceptor = new TelemetryDbCommandInterceptor();

            // Assert
            Assert.IsNotNull(interceptor);
        }

        [TestMethod]
        public void Constructor_WithOptions_DoesNotThrow()
        {
            // Arrange
            var options = new EfCoreTelemetryOptions { RecordStatements = false };

            // Act
            var interceptor = new TelemetryDbCommandInterceptor(options);

            // Assert
            Assert.IsNotNull(interceptor);
        }

        [TestMethod]
        public void Constructor_WithActivitySource_DoesNotThrow()
        {
            // Arrange
            using var source = new ActivitySource("test.efcore.interceptor");

            // Act
            var interceptor = new TelemetryDbCommandInterceptor(source);

            // Assert
            Assert.IsNotNull(interceptor);
        }

        [TestMethod]
        public void Constructor_NullActivitySource_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new TelemetryDbCommandInterceptor((ActivitySource)null!));
        }

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
        [DataRow("execute sp_GetUsers", "EXECUTE")]
        public void DetectOperation_VariousCommands_ReturnsExpected(string? commandText, string expected)
        {
            // Act
            var result = TelemetryDbCommandInterceptor.DetectOperation(commandText);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
