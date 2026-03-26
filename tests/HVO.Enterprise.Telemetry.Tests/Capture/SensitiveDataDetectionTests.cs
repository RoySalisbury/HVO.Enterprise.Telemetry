using System;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.Capture;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Capture
{
    [TestClass]
    public class SensitiveDataDetectionTests
    {
        private ParameterCapture _capture = null!;

        [TestInitialize]
        public void Setup()
        {
            _capture = new ParameterCapture();
        }

        // ─── Built-in pattern detection ─────────────────────────────────

        [TestMethod]
        [DataRow("password", true)]
        [DataRow("Password", true)]
        [DataRow("userPassword", true)]
        [DataRow("passwd", true)]
        [DataRow("pwd", true)]
        [DataRow("token", true)]
        [DataRow("accessToken", true)]
        [DataRow("apikey", true)]
        [DataRow("api_key", true)]
        [DataRow("secret", true)]
        [DataRow("clientSecret", true)]
        [DataRow("credential", true)]
        [DataRow("authorization", true)]
        [DataRow("ssn", true)]
        [DataRow("socialSecurity", true)]
        [DataRow("creditCard", true)]
        [DataRow("credit_card", true)]
        [DataRow("cardNumber", true)]
        [DataRow("cvv", true)]
        [DataRow("cvc", true)]
        [DataRow("email", true)]
        [DataRow("emailAddress", true)]
        [DataRow("phone", true)]
        [DataRow("phoneNumber", true)]
        [DataRow("mobile", true)]
        [DataRow("orderId", false)]
        [DataRow("customerName", false)]
        [DataRow("quantity", false)]
        [DataRow("price", false)]
        [DataRow("description", false)]
        public void IsSensitive_DetectsPatterns(string name, bool expected)
        {
            Assert.AreEqual(expected, _capture.IsSensitive(name));
        }

        [TestMethod]
        public void IsSensitive_NullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(_capture.IsSensitive(null!));
            Assert.IsFalse(_capture.IsSensitive(""));
        }

        [TestMethod]
        public void IsSensitive_CaseSensitivity_MatchesIgnoreCase()
        {
            Assert.IsTrue(_capture.IsSensitive("PASSWORD"));
            Assert.IsTrue(_capture.IsSensitive("Token"));
            Assert.IsTrue(_capture.IsSensitive("SSN"));
            Assert.IsTrue(_capture.IsSensitive("CreditCard"));
        }

        // ─── Redaction strategy per pattern ─────────────────────────────

        [TestMethod]
        public void GetRedactionStrategy_AuthPatterns_ReturnsMask()
        {
            Assert.AreEqual(RedactionStrategy.Mask, _capture.GetRedactionStrategy("password"));
            Assert.AreEqual(RedactionStrategy.Mask, _capture.GetRedactionStrategy("token"));
            Assert.AreEqual(RedactionStrategy.Mask, _capture.GetRedactionStrategy("secret"));
            Assert.AreEqual(RedactionStrategy.Mask, _capture.GetRedactionStrategy("apikey"));
        }

        [TestMethod]
        public void GetRedactionStrategy_FinancialPatterns_ReturnsHash()
        {
            Assert.AreEqual(RedactionStrategy.Hash, _capture.GetRedactionStrategy("creditCard"));
            Assert.AreEqual(RedactionStrategy.Hash, _capture.GetRedactionStrategy("cardNumber"));
            Assert.AreEqual(RedactionStrategy.Hash, _capture.GetRedactionStrategy("accountnumber"));
        }

        [TestMethod]
        public void GetRedactionStrategy_PiiPatterns_ReturnsHash()
        {
            Assert.AreEqual(RedactionStrategy.Hash, _capture.GetRedactionStrategy("ssn"));
            Assert.AreEqual(RedactionStrategy.Hash, _capture.GetRedactionStrategy("socialSecurity"));
            Assert.AreEqual(RedactionStrategy.Hash, _capture.GetRedactionStrategy("taxid"));
        }

        [TestMethod]
        public void GetRedactionStrategy_ContactPatterns_ReturnsPartial()
        {
            Assert.AreEqual(RedactionStrategy.Partial, _capture.GetRedactionStrategy("email"));
            Assert.AreEqual(RedactionStrategy.Partial, _capture.GetRedactionStrategy("phone"));
            Assert.AreEqual(RedactionStrategy.Partial, _capture.GetRedactionStrategy("mobile"));
        }

        [TestMethod]
        public void GetRedactionStrategy_NonSensitive_ReturnsMaskDefault()
        {
            Assert.AreEqual(RedactionStrategy.Mask, _capture.GetRedactionStrategy("orderId"));
        }

        // ─── Custom pattern registration ────────────────────────────────

        [TestMethod]
        public void RegisterSensitivePattern_CustomPattern_Detected()
        {
            _capture.RegisterSensitivePattern("vendorid", RedactionStrategy.Hash);

            Assert.IsTrue(_capture.IsSensitive("vendorId"));
            Assert.IsTrue(_capture.IsSensitive("myVendorIdField"));
            Assert.AreEqual(RedactionStrategy.Hash, _capture.GetRedactionStrategy("vendorId"));
        }

        [TestMethod]
        public void RegisterSensitivePattern_NullPattern_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _capture.RegisterSensitivePattern(null!, RedactionStrategy.Mask));
        }

        [TestMethod]
        public void RegisterSensitivePattern_EmptyPattern_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _capture.RegisterSensitivePattern("", RedactionStrategy.Mask));
        }

        [TestMethod]
        public void RegisterSensitivePattern_ClearsCache()
        {
            // First, check that "xyzfield" is not sensitive.
            Assert.IsFalse(_capture.IsSensitive("xyzfield"));

            // Register, then verify cache was cleared.
            _capture.RegisterSensitivePattern("xyzfield", RedactionStrategy.Mask);
            Assert.IsTrue(_capture.IsSensitive("xyzfield"));
        }

        // ─── No-defaults constructor ────────────────────────────────────

        [TestMethod]
        public void Constructor_NoDefaults_HasNoPatterns()
        {
            var capture = new ParameterCapture(registerDefaults: false);

            Assert.IsFalse(capture.IsSensitive("password"));
            Assert.IsFalse(capture.IsSensitive("ssn"));
            Assert.IsFalse(capture.IsSensitive("email"));
        }

        [TestMethod]
        public void Constructor_WithDefaults_HasPatterns()
        {
            var capture = new ParameterCapture(registerDefaults: true);

            Assert.IsTrue(capture.IsSensitive("password"));
            Assert.IsTrue(capture.IsSensitive("ssn"));
            Assert.IsTrue(capture.IsSensitive("email"));
        }

        // ─── Auto-detect integration ────────────────────────────────────

        [TestMethod]
        public void CaptureParameter_SensitiveAutoDetected_Redacted()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Standard,
                AutoDetectSensitiveData = true,
                RedactionStrategy = RedactionStrategy.Mask
            };

            var result = _capture.CaptureParameter("password", "secret123", typeof(string), options);

            Assert.AreEqual("***", result);
        }

        [TestMethod]
        public void CaptureParameter_SensitiveAutoDetectDisabled_CapturesPlainText()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Standard,
                AutoDetectSensitiveData = false
            };

            var result = _capture.CaptureParameter("password", "secret123", typeof(string), options);

            Assert.AreEqual("secret123", result);
        }

        [TestMethod]
        public void CaptureParameter_SensitiveEmail_PartialRedacted()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Standard,
                AutoDetectSensitiveData = true
            };

            var result = _capture.CaptureParameter("emailAddress", "user@example.com", typeof(string), options);

            // "email" pattern has Partial strategy.
            var str = (string)result!;
            Assert.IsTrue(str.Contains("***"), $"Expected partial redaction, got: {str}");
            Assert.AreNotEqual("user@example.com", str);
        }

        [TestMethod]
        public void CaptureParameter_SensitiveSsn_Hashed()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Standard,
                AutoDetectSensitiveData = true
            };

            var result = _capture.CaptureParameter("ssn", "123-45-6789", typeof(string), options);

            // "ssn" pattern has Hash strategy.
            var str = (string)result!;
            Assert.AreNotEqual("123-45-6789", str);
            Assert.AreNotEqual("***", str);
            Assert.AreEqual(8, str.Length); // 8-char hex hash
        }

        // ─── [SensitiveData] attribute integration ──────────────────────

        [TestMethod]
        public void CaptureParameters_SensitiveDataAttribute_Respects()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };
            var method = typeof(ISecureService).GetMethod("Process")!;
            var parameters = method.GetParameters();
            var values = new object?[] { 42, "secret-pw" };

            var result = _capture.CaptureParameters(parameters, values, options);

            Assert.AreEqual(42, result["id"]);
            Assert.AreEqual("***", result["password"]); // [SensitiveData] with Mask
        }

        [TestMethod]
        public void CaptureParameters_SensitiveData_HashStrategy()
        {
            var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };
            var method = typeof(ISecureService).GetMethod("Track")!;
            var parameters = method.GetParameters();
            var values = new object?[] { "user@test.com" };

            var result = _capture.CaptureParameters(parameters, values, options);

            var captured = (string)result["email"]!;
            Assert.AreNotEqual("user@test.com", captured);
            Assert.AreEqual(8, captured.Length);
        }

        // ─── Complex object with sensitive properties ───────────────────

        [TestMethod]
        public void CaptureParameter_ComplexObject_SensitiveProperty_Redacted()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                CapturePropertyNames = true,
                UseCustomToString = false,
                AutoDetectSensitiveData = true
            };
            var obj = new PersonDto { Name = "Alice", Ssn = "123-45-6789" };

            var result = _capture.CaptureParameter("person", obj, typeof(PersonDto), options);

            Assert.IsInstanceOfType(result, typeof(Dictionary<string, object?>));
            var dict = (Dictionary<string, object?>)result;
            Assert.AreEqual("Alice", dict["Name"]);
            // Ssn matched by auto-detect "ssn" pattern with Hash strategy
            Assert.AreNotEqual("123-45-6789", dict["Ssn"]);
        }

        [TestMethod]
        public void CaptureParameter_ComplexObject_SensitiveDataAttribute_Redacted()
        {
            var options = new ParameterCaptureOptions
            {
                Level = CaptureLevel.Verbose,
                CapturePropertyNames = true,
                UseCustomToString = false
            };
            var obj = new SecureDto { Id = 1, ApiKey = "abc-123" };

            var result = _capture.CaptureParameter("dto", obj, typeof(SecureDto), options);

            Assert.IsInstanceOfType(result, typeof(Dictionary<string, object?>));
            var dict = (Dictionary<string, object?>)result;
            Assert.AreEqual(1, dict["Id"]);
            // apikey matched by auto-detect, or [SensitiveData] on property
            Assert.AreNotEqual("abc-123", dict["ApiKey"]);
        }

        // ─── Helper types ───────────────────────────────────────────────

        public interface ISecureService
        {
            void Process(int id, [SensitiveData] string password);
            void Track([SensitiveData(Strategy = RedactionStrategy.Hash)] string email);
        }

        public class PersonDto
        {
            public string? Name { get; set; }
            public string? Ssn { get; set; }
        }

        public class SecureDto
        {
            public int Id { get; set; }

            [SensitiveData(Strategy = RedactionStrategy.Remove)]
            public string? ApiKey { get; set; }
        }
    }
}
