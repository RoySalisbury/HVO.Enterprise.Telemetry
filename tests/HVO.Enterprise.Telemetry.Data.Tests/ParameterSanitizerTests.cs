using System;
using HVO.Enterprise.Telemetry.Data.Common;

namespace HVO.Enterprise.Telemetry.Data.Tests
{
    [TestClass]
    public class ParameterSanitizerTests
    {
        // ──────────────────────────────────────────────────────────────
        // SanitizeConnectionString
        // ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void SanitizeConnectionString_NullInput_ReturnsEmpty()
        {
            var result = ParameterSanitizer.SanitizeConnectionString(null);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void SanitizeConnectionString_EmptyInput_ReturnsEmpty()
        {
            var result = ParameterSanitizer.SanitizeConnectionString(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void SanitizeConnectionString_WhitespaceInput_ReturnsEmpty()
        {
            var result = ParameterSanitizer.SanitizeConnectionString("   ");
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void SanitizeConnectionString_WithPassword_RedactsPassword()
        {
            var connStr = "Server=localhost;Database=mydb;Password=SuperSecret123;";
            var result = ParameterSanitizer.SanitizeConnectionString(connStr);

            Assert.IsTrue(result.Contains(ParameterSanitizer.RedactedValue));
            Assert.IsFalse(result.Contains("SuperSecret123"));
        }

        [TestMethod]
        public void SanitizeConnectionString_WithPwd_RedactsPwd()
        {
            var connStr = "Server=localhost;Database=mydb;Pwd=SuperSecret123;";
            var result = ParameterSanitizer.SanitizeConnectionString(connStr);

            Assert.IsTrue(result.Contains(ParameterSanitizer.RedactedValue));
            Assert.IsFalse(result.Contains("SuperSecret123"));
        }

        [TestMethod]
        public void SanitizeConnectionString_NoSensitiveData_ReturnsUnchanged()
        {
            var connStr = "Server=localhost;Database=mydb;Integrated Security=true";
            var result = ParameterSanitizer.SanitizeConnectionString(connStr);

            Assert.AreEqual(connStr, result);
        }

        // ──────────────────────────────────────────────────────────────
        // IsSensitiveParameter
        // ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void IsSensitiveParameter_NullInput_ReturnsFalse()
        {
            Assert.IsFalse(ParameterSanitizer.IsSensitiveParameter(null));
        }

        [TestMethod]
        public void IsSensitiveParameter_EmptyInput_ReturnsFalse()
        {
            Assert.IsFalse(ParameterSanitizer.IsSensitiveParameter(string.Empty));
        }

        [TestMethod]
        [DataRow("password")]
        [DataRow("Password")]
        [DataRow("@password")]
        [DataRow(":password")]
        [DataRow("pwd")]
        [DataRow("secret")]
        [DataRow("token")]
        [DataRow("apikey")]
        [DataRow("ssn")]
        [DataRow("creditcard")]
        [DataRow("cvv")]
        [DataRow("pin")]
        [DataRow("authorization")]
        [DataRow("auth_token")]
        [DataRow("access_token")]
        [DataRow("refresh_token")]
        public void IsSensitiveParameter_KnownSensitiveNames_ReturnsTrue(string name)
        {
            Assert.IsTrue(ParameterSanitizer.IsSensitiveParameter(name));
        }

        [TestMethod]
        [DataRow("customerId")]
        [DataRow("name")]
        [DataRow("email")]
        [DataRow("orderId")]
        public void IsSensitiveParameter_NormalNames_ReturnsFalse(string name)
        {
            Assert.IsFalse(ParameterSanitizer.IsSensitiveParameter(name));
        }

        // ──────────────────────────────────────────────────────────────
        // SanitizeStatement
        // ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void SanitizeStatement_NullInput_ReturnsEmpty()
        {
            var result = ParameterSanitizer.SanitizeStatement(null);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void SanitizeStatement_EmptyInput_ReturnsEmpty()
        {
            var result = ParameterSanitizer.SanitizeStatement(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void SanitizeStatement_UnderMaxLength_ReturnsOriginal()
        {
            var statement = "SELECT * FROM Users";
            var result = ParameterSanitizer.SanitizeStatement(statement, 100);

            Assert.AreEqual(statement, result);
        }

        [TestMethod]
        public void SanitizeStatement_ExceedingMaxLength_Truncates()
        {
            var statement = new string('X', 3000);
            var result = ParameterSanitizer.SanitizeStatement(statement, 100);

            Assert.IsTrue(result.Length < statement.Length);
            Assert.IsTrue(result.EndsWith("... [truncated]"));
        }

        [TestMethod]
        public void SanitizeStatement_ExactlyMaxLength_ReturnsOriginal()
        {
            var statement = new string('X', 100);
            var result = ParameterSanitizer.SanitizeStatement(statement, 100);

            Assert.AreEqual(statement, result);
        }

        // ──────────────────────────────────────────────────────────────
        // FormatParameterValue
        // ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void FormatParameterValue_SensitiveName_ReturnsRedacted()
        {
            var result = ParameterSanitizer.FormatParameterValue("password", "secret123");
            Assert.AreEqual(ParameterSanitizer.RedactedValue, result);
        }

        [TestMethod]
        public void FormatParameterValue_NullValue_ReturnsNULL()
        {
            var result = ParameterSanitizer.FormatParameterValue("col1", null);
            Assert.AreEqual("NULL", result);
        }

        [TestMethod]
        public void FormatParameterValue_DBNullValue_ReturnsNULL()
        {
            var result = ParameterSanitizer.FormatParameterValue("col1", DBNull.Value);
            Assert.AreEqual("NULL", result);
        }

        [TestMethod]
        public void FormatParameterValue_ShortString_ReturnsQuotedString()
        {
            var result = ParameterSanitizer.FormatParameterValue("name", "Alice");
            Assert.AreEqual("\"Alice\"", result);
        }

        [TestMethod]
        public void FormatParameterValue_LongString_ReturnsTruncated()
        {
            var longStr = new string('A', 200);
            var result = ParameterSanitizer.FormatParameterValue("notes", longStr);

            Assert.IsTrue(result.Contains("truncated"));
        }

        [TestMethod]
        public void FormatParameterValue_ByteArray_ReturnsBinaryDescription()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var result = ParameterSanitizer.FormatParameterValue("data", bytes);

            Assert.AreEqual("<binary 3 bytes>", result);
        }

        [TestMethod]
        public void FormatParameterValue_Integer_ReturnsToString()
        {
            var result = ParameterSanitizer.FormatParameterValue("id", 42);
            Assert.AreEqual("42", result);
        }

        [TestMethod]
        public void RedactedValue_HasExpectedContent()
        {
            Assert.AreEqual("***REDACTED***", ParameterSanitizer.RedactedValue);
        }
    }
}
