using System;
using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Context.Providers;
using HVO.Enterprise.Telemetry.Logging;
using HVO.Enterprise.Telemetry.Logging.Enrichers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Logging
{
    [TestClass]
    public sealed class BuiltInEnricherTests
    {
        // =====================================================================
        // EnvironmentLogEnricher Tests
        // =====================================================================

        [TestMethod]
        public void EnvironmentEnricher_AddsMachineName()
        {
            // Arrange
            var enricher = new EnvironmentLogEnricher();
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert
            Assert.IsTrue(props.ContainsKey("MachineName"));
            Assert.IsNotNull(props["MachineName"]);
            Assert.AreEqual(Environment.MachineName, props["MachineName"]!.ToString());
        }

        [TestMethod]
        public void EnvironmentEnricher_AddsProcessId()
        {
            // Arrange
            var enricher = new EnvironmentLogEnricher();
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert
            Assert.IsTrue(props.ContainsKey("ProcessId"));
            Assert.IsNotNull(props["ProcessId"]);

            using var currentProcess = Process.GetCurrentProcess();
            Assert.AreEqual(currentProcess.Id.ToString(), props["ProcessId"]!.ToString());
        }

        [TestMethod]
        public void EnvironmentEnricher_NullProperties_ThrowsArgumentNullException()
        {
            // Arrange
            var enricher = new EnvironmentLogEnricher();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => enricher.Enrich(null!));
        }

        [TestMethod]
        public void EnvironmentEnricher_CalledMultipleTimes_ReturnsSameValues()
        {
            // Arrange
            var enricher = new EnvironmentLogEnricher();
            var props1 = new Dictionary<string, object?>();
            var props2 = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props1);
            enricher.Enrich(props2);

            // Assert — cached values, should be identical
            Assert.AreEqual(props1["MachineName"], props2["MachineName"]);
            Assert.AreEqual(props1["ProcessId"], props2["ProcessId"]);
        }

        // =====================================================================
        // UserContextLogEnricher Tests
        // =====================================================================

        [TestMethod]
        public void UserContextEnricher_WithUserContext_AddsUserIdAndUsername()
        {
            // Arrange
            var accessor = new FakeUserContextAccessor
            {
                UserContext = new UserContext
                {
                    UserId = "user-123",
                    Username = "johndoe"
                }
            };
            var enricher = new UserContextLogEnricher(accessor);
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert
            Assert.AreEqual("user-123", props["UserId"]);
            Assert.AreEqual("johndoe", props["Username"]);
        }

        [TestMethod]
        public void UserContextEnricher_WithNullUserContext_AddsNothing()
        {
            // Arrange
            var accessor = new FakeUserContextAccessor { UserContext = null };
            var enricher = new UserContextLogEnricher(accessor);
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert
            Assert.AreEqual(0, props.Count);
        }

        [TestMethod]
        public void UserContextEnricher_DoesNotAddEmptyFields()
        {
            // Arrange
            var accessor = new FakeUserContextAccessor
            {
                UserContext = new UserContext
                {
                    UserId = null,
                    Username = ""
                }
            };
            var enricher = new UserContextLogEnricher(accessor);
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert — null UserId and empty Username should be omitted
            Assert.IsFalse(props.ContainsKey("UserId"));
            Assert.IsFalse(props.ContainsKey("Username"));
        }

        [TestMethod]
        public void UserContextEnricher_DoesNotIncludeEmailOrTenantId()
        {
            // Arrange — email and tenantId deliberately excluded for PII safety
            var accessor = new FakeUserContextAccessor
            {
                UserContext = new UserContext
                {
                    UserId = "u1",
                    Username = "user1",
                    Email = "user@example.com",
                    TenantId = "tenant-42"
                }
            };
            var enricher = new UserContextLogEnricher(accessor);
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert
            Assert.IsTrue(props.ContainsKey("UserId"));
            Assert.IsTrue(props.ContainsKey("Username"));
            Assert.IsFalse(props.ContainsKey("Email"), "Email should not be exposed in logs");
            Assert.IsFalse(props.ContainsKey("TenantId"), "TenantId should not be exposed in logs");
        }

        [TestMethod]
        public void UserContextEnricher_NullProperties_ThrowsArgumentNullException()
        {
            // Arrange
            var enricher = new UserContextLogEnricher(new FakeUserContextAccessor());

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => enricher.Enrich(null!));
        }

        [TestMethod]
        public void UserContextEnricher_DefaultConstructor_DoesNotThrow()
        {
            // Arrange & Act — uses DefaultUserContextAccessor internally
            // Construction should not throw regardless of platform
            var enricher = new UserContextLogEnricher();

            // Assert — enricher was created successfully
            Assert.IsNotNull(enricher);
        }

        // =====================================================================
        // HttpRequestLogEnricher Tests
        // =====================================================================

        [TestMethod]
        public void HttpRequestEnricher_WithRequestInfo_AddsMethodPathUrl()
        {
            // Arrange
            var accessor = new FakeHttpRequestAccessor
            {
                RequestInfo = new HttpRequestInfo
                {
                    Method = "POST",
                    Path = "/api/orders",
                    Url = "https://example.com/api/orders"
                }
            };
            var enricher = new HttpRequestLogEnricher(accessor);
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert
            Assert.AreEqual("POST", props["HttpMethod"]);
            Assert.AreEqual("/api/orders", props["HttpPath"]);
            Assert.AreEqual("https://example.com/api/orders", props["HttpUrl"]);
        }

        [TestMethod]
        public void HttpRequestEnricher_WithNullRequest_AddsNothing()
        {
            // Arrange
            var accessor = new FakeHttpRequestAccessor { RequestInfo = null };
            var enricher = new HttpRequestLogEnricher(accessor);
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert
            Assert.AreEqual(0, props.Count);
        }

        [TestMethod]
        public void HttpRequestEnricher_DoesNotAddEmptyFields()
        {
            // Arrange
            var accessor = new FakeHttpRequestAccessor
            {
                RequestInfo = new HttpRequestInfo
                {
                    Method = "GET",
                    Path = "",
                    Url = ""
                }
            };
            var enricher = new HttpRequestLogEnricher(accessor);
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert
            Assert.IsTrue(props.ContainsKey("HttpMethod"));
            Assert.IsFalse(props.ContainsKey("HttpPath"), "Empty path should be omitted");
            Assert.IsFalse(props.ContainsKey("HttpUrl"), "Empty URL should be omitted");
        }

        [TestMethod]
        public void HttpRequestEnricher_DoesNotIncludeQueryOrHeaders()
        {
            // Arrange — query, headers, user agent, client IP deliberately excluded
            var accessor = new FakeHttpRequestAccessor
            {
                RequestInfo = new HttpRequestInfo
                {
                    Method = "GET",
                    Path = "/api/users",
                    Url = "https://example.com/api/users?token=secret",
                    QueryString = "token=secret",
                    UserAgent = "TestAgent/1.0"
                }
            };
            var enricher = new HttpRequestLogEnricher(accessor);
            var props = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(props);

            // Assert
            Assert.IsFalse(props.ContainsKey("QueryString"), "QueryString should not be exposed");
            Assert.IsFalse(props.ContainsKey("UserAgent"), "UserAgent should not be exposed");
            Assert.IsFalse(props.ContainsKey("Headers"), "Headers should not be exposed");
            Assert.IsFalse(props.ContainsKey("ClientIp"), "ClientIp should not be exposed");
        }

        [TestMethod]
        public void HttpRequestEnricher_NullProperties_ThrowsArgumentNullException()
        {
            // Arrange
            var enricher = new HttpRequestLogEnricher(new FakeHttpRequestAccessor());

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => enricher.Enrich(null!));
        }

        [TestMethod]
        public void HttpRequestEnricher_DefaultConstructor_DoesNotThrow()
        {
            // Arrange & Act — uses DefaultHttpRequestAccessor internally
            var enricher = new HttpRequestLogEnricher();
            var props = new Dictionary<string, object?>();

            // Should not throw even without a configured accessor
            enricher.Enrich(props);
        }
    }
}
