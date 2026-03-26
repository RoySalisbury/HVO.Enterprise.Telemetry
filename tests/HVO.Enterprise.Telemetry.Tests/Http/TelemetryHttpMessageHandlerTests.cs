using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Http;

namespace HVO.Enterprise.Telemetry.Tests.Http
{
    [TestClass]
    public class TelemetryHttpMessageHandlerTests
    {
        private ActivityListener _listener = null!;
        private List<Activity> _capturedActivities = null!;

        [TestInitialize]
        public void Setup()
        {
            _capturedActivities = new List<Activity>();

            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == TelemetryHttpMessageHandler.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => _capturedActivities.Add(activity)
            };
            ActivitySource.AddActivityListener(_listener);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _listener.Dispose();
        }

        /// <summary>
        /// Returns the Activity created by the last <see cref="TelemetryHttpMessageHandler.SendAsync"/> call.
        /// All tags (request + response) are guaranteed to be set because <c>await client.*Async()</c>
        /// fully completes before this is called.
        /// </summary>
        private Activity GetActivity() => _capturedActivities[_capturedActivities.Count - 1];

        // -------------------------------------------------------------------
        // Activity creation
        // -------------------------------------------------------------------

        [TestMethod]
        public async Task SendAsync_CreatesActivity_WithClientKind()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            Assert.AreEqual(1, _capturedActivities.Count);
            var activity = GetActivity();

            Assert.AreEqual(ActivityKind.Client, activity.Kind);
            Assert.IsTrue(activity.OperationName.Contains("GET"),
                $"Expected OperationName to contain 'GET', was '{activity.OperationName}'");
        }

        [TestMethod]
        public async Task SendAsync_SetsDisplayName_WithMethodAndHost()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            Assert.IsTrue(activity.DisplayName.Contains("GET"),
                $"Expected DisplayName to contain 'GET', was '{activity.DisplayName}'");
            Assert.IsTrue(activity.DisplayName.Contains("api.example.com"),
                $"Expected DisplayName to contain host, was '{activity.DisplayName}'");
        }

        // -------------------------------------------------------------------
        // Request enrichment / OpenTelemetry semantic conventions
        // -------------------------------------------------------------------

        [TestMethod]
        public async Task SendAsync_SetsHttpTags_FollowingSemanticConventions()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com:8080/users?id=123");

            var activity = GetActivity();
            Assert.AreEqual("GET", activity.GetTagItem("http.method"));
            Assert.AreEqual("https", activity.GetTagItem("http.scheme"));
            Assert.AreEqual("api.example.com", activity.GetTagItem("http.host"));
            Assert.AreEqual(8080, activity.GetTagItem("net.peer.port"));
        }

        [TestMethod]
        public async Task SendAsync_RecordsStatusCode_OnSuccess()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            Assert.AreEqual(200, activity.GetTagItem("http.status_code"));
        }

        [TestMethod]
        public async Task SendAsync_DefaultPort443_OmitsPortTag()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            Assert.IsNull(activity.GetTagItem("net.peer.port"),
                "Default HTTPS port 443 should not be tagged.");
        }

        [TestMethod]
        public async Task SendAsync_DefaultPort80_OmitsPortTag()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("http://api.example.com/users");

            var activity = GetActivity();
            Assert.IsNull(activity.GetTagItem("net.peer.port"),
                "Default HTTP port 80 should not be tagged.");
        }

        [TestMethod]
        public async Task SendAsync_NonDefaultPort_RecordsPortTag()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com:9443/data");

            var activity = GetActivity();
            Assert.AreEqual(9443, activity.GetTagItem("net.peer.port"));
        }

        // -------------------------------------------------------------------
        // W3C TraceContext propagation
        // -------------------------------------------------------------------

        [TestMethod]
        public async Task SendAsync_InjectsTraceparentHeader()
        {
            var inner = FakeHttpMessageHandler.Ok();
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            Assert.IsNotNull(inner.LastRequest);
            Assert.IsTrue(inner.LastRequest!.Headers.Contains("traceparent"),
                "traceparent header should be present.");
        }

        [TestMethod]
        public async Task SendAsync_TraceparentHeader_FollowsW3CFormat()
        {
            var inner = FakeHttpMessageHandler.Ok();
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var traceparent = string.Join("", inner.LastRequest!.Headers.GetValues("traceparent"));
            // W3C format: 00-{32 hex trace-id}-{16 hex span-id}-{2 hex flags}
            var pattern = @"^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$";
            Assert.IsTrue(Regex.IsMatch(traceparent, pattern),
                $"traceparent '{traceparent}' does not match W3C format.");
        }

        [TestMethod]
        public async Task SendAsync_TraceparentHeader_CarriesSampledFlag()
        {
            var inner = FakeHttpMessageHandler.Ok();
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var traceparent = string.Join("", inner.LastRequest!.Headers.GetValues("traceparent"));
            // Listener sets AllDataAndRecorded so flag should be "01"
            Assert.IsTrue(traceparent.EndsWith("-01"),
                $"Expected sampled flag '01', traceparent was '{traceparent}'");
        }

        [TestMethod]
        public async Task SendAsync_WithParentActivity_PropagatesToTraceId()
        {
            using var parent = new Activity("parent").Start();
            var expectedTraceId = parent.TraceId.ToHexString();

            var inner = FakeHttpMessageHandler.Ok();
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var traceparent = string.Join("", inner.LastRequest!.Headers.GetValues("traceparent"));
            Assert.IsTrue(traceparent.Contains(expectedTraceId),
                $"Expected trace-id '{expectedTraceId}' in traceparent '{traceparent}'");
        }

        [TestMethod]
        public async Task SendAsync_WithTraceState_PropagatesToHeader()
        {
            using var parent = new Activity("parent");
            parent.TraceStateString = "congo=t61rcWkgMzE";
            parent.Start();

            var inner = FakeHttpMessageHandler.Ok();
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            Assert.IsTrue(inner.LastRequest!.Headers.Contains("tracestate"),
                "tracestate header should be present when parent has tracestate.");

            var tracestate = string.Join("", inner.LastRequest!.Headers.GetValues("tracestate"));
            Assert.IsTrue(tracestate.Contains("congo=t61rcWkgMzE"),
                $"Expected tracestate to contain parent's value, was '{tracestate}'");
        }

        // -------------------------------------------------------------------
        // Error handling - HTTP error status codes
        // -------------------------------------------------------------------

        [TestMethod]
        public async Task SendAsync_ServerError500_SetsActivityStatusError()
        {
            var inner = FakeHttpMessageHandler.WithStatus(HttpStatusCode.InternalServerError, "Server Error");
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
            Assert.AreEqual("Server Error", activity.StatusDescription);
            Assert.AreEqual(500, activity.GetTagItem("http.status_code"));
        }

        [TestMethod]
        public async Task SendAsync_ClientError404_SetsStatusUnset()
        {
            var inner = FakeHttpMessageHandler.WithStatus(HttpStatusCode.NotFound);
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users/999");

            var activity = GetActivity();
            Assert.AreEqual(ActivityStatusCode.Unset, activity.Status);
            Assert.AreEqual(404, activity.GetTagItem("http.status_code"));
        }

        [TestMethod]
        public async Task SendAsync_SuccessStatus200_DoesNotSetErrorStatus()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            Assert.AreEqual(ActivityStatusCode.Unset, activity.Status);
        }

        // -------------------------------------------------------------------
        // Error handling - Exceptions
        // -------------------------------------------------------------------

        [TestMethod]
        public async Task SendAsync_Exception_SetsActivityStatusError()
        {
            var inner = FakeHttpMessageHandler.Throwing(new HttpRequestException("Connection refused"));
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await Assert.ThrowsExactlyAsync<HttpRequestException>(
                () => client.GetAsync("https://api.example.com/users"));

            var activity = GetActivity();
            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
            Assert.AreEqual("Connection refused", activity.StatusDescription);
        }

        [TestMethod]
        public async Task SendAsync_Exception_RecordsExceptionEvent()
        {
            var inner = FakeHttpMessageHandler.Throwing(new InvalidOperationException("Network failure"));
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => client.GetAsync("https://api.example.com/users"));

            var activity = GetActivity();
            var exceptionEvent = activity.Events.FirstOrDefault(e => e.Name == "exception");
            Assert.AreNotEqual(default, exceptionEvent, "Expected an 'exception' event on the activity.");

            var tags = exceptionEvent.Tags.ToDictionary(t => t.Key, t => t.Value);
            Assert.AreEqual("System.InvalidOperationException", tags["exception.type"]);
            Assert.AreEqual("Network failure", tags["exception.message"]);
        }

        [TestMethod]
        public async Task SendAsync_Exception_RethrowsOriginalException()
        {
            var expected = new HttpRequestException("Connection refused");
            var inner = FakeHttpMessageHandler.Throwing(expected);
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            var thrown = await Assert.ThrowsExactlyAsync<HttpRequestException>(
                () => client.GetAsync("https://api.example.com/users"));

            Assert.AreSame(expected, thrown, "Should rethrow the original exception.");
        }

        // -------------------------------------------------------------------
        // URL redaction
        // -------------------------------------------------------------------

        [TestMethod]
        public async Task SendAsync_DefaultOptions_RedactsQueryString()
        {
            // RedactQueryStrings defaults to true
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users?token=secret123&page=1");

            var activity = GetActivity();
            var url = activity.GetTagItem("http.url") as string;

            Assert.IsNotNull(url);
            Assert.IsFalse(url!.Contains("secret123"), "Query string value should be redacted.");
            Assert.IsTrue(url.Contains("[REDACTED]"), "Redacted URL should contain [REDACTED] marker.");
            Assert.IsTrue(url.Contains("api.example.com/users"), "Base URL should be preserved.");
        }

        [TestMethod]
        public async Task SendAsync_DefaultOptions_RedactsTargetQueryString()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users?token=secret");

            var activity = GetActivity();
            var target = activity.GetTagItem("http.target") as string;

            Assert.IsNotNull(target);
            Assert.IsFalse(target!.Contains("secret"), "Target query string should be redacted.");
            Assert.IsTrue(target.Contains("[REDACTED]"),
                "Redacted target should contain [REDACTED] marker.");
        }

        [TestMethod]
        public async Task SendAsync_RedactQueryStringsDisabled_ShowsFullUrl()
        {
            var options = new HttpInstrumentationOptions { RedactQueryStrings = false };
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok(), options);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users?token=secret123");

            var activity = GetActivity();
            var url = activity.GetTagItem("http.url") as string;

            Assert.IsNotNull(url);
            Assert.IsTrue(url!.Contains("secret123"),
                "Full query string should be visible when redaction is disabled.");
        }

        [TestMethod]
        public async Task SendAsync_NoQueryString_UrlUnmodified()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            var url = activity.GetTagItem("http.url") as string;

            Assert.IsNotNull(url);
            Assert.IsFalse(url!.Contains("[REDACTED]"),
                "URL without query string should not be modified.");
        }

        // -------------------------------------------------------------------
        // Header capture
        // -------------------------------------------------------------------

        [TestMethod]
        public async Task SendAsync_CaptureRequestHeaders_RecordsSafeHeaders()
        {
            var options = new HttpInstrumentationOptions { CaptureRequestHeaders = true };
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok(), options);
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Custom-Header", "custom-value");

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            Assert.AreEqual("custom-value",
                activity.GetTagItem("http.request.header.x-custom-header"),
                "Safe request headers should be captured.");
        }

        [TestMethod]
        public async Task SendAsync_CaptureRequestHeaders_ExcludesSensitiveHeaders()
        {
            var options = new HttpInstrumentationOptions { CaptureRequestHeaders = true };
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok(), options);
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer secret-token");

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            var hasAuth = activity.Tags.Any(t =>
                t.Key.Contains("authorization", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(hasAuth,
                "Authorization header should be excluded from capture.");
        }

        [TestMethod]
        public async Task SendAsync_CaptureResponseHeaders_RecordsHeaders()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.TryAddWithoutValidation("X-Request-Id", "req-123");
            var inner = new FakeHttpMessageHandler(response);

            var options = new HttpInstrumentationOptions { CaptureResponseHeaders = true };
            using var handler = CreateHandler(inner, options);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            Assert.AreEqual("req-123",
                activity.GetTagItem("http.response.header.x-request-id"),
                "Response headers should be captured.");
        }

        [TestMethod]
        public async Task SendAsync_HeaderCaptureDisabledByDefault_NoHeaderTags()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Custom", "value");

            await client.GetAsync("https://api.example.com/users");

            var activity = GetActivity();
            var hasHeaderTag = activity.Tags.Any(t => t.Key.StartsWith("http.request.header."));
            Assert.IsFalse(hasHeaderTag,
                "Header tags should not be present when capture is disabled (default).");
        }

        // -------------------------------------------------------------------
        // No listener scenario (unsampled)
        // -------------------------------------------------------------------

        [TestMethod]
        public async Task SendAsync_NoListener_CompletesSuccessfully()
        {
            // Dispose the listener so no activities are created
            _listener.Dispose();

            var inner = FakeHttpMessageHandler.Ok();
            using var handler = new TelemetryHttpMessageHandler(null, null)
            {
                InnerHandler = inner
            };
            using var client = new HttpClient(handler);

            var response = await client.GetAsync("https://api.example.com/users");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, inner.CallCount, "Request should still be sent.");
        }

        [TestMethod]
        public async Task SendAsync_NullRequest_ThrowsArgumentNull()
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());

            // Use reflection to call SendAsync with null — normally HttpClient prevents this
            var method = typeof(TelemetryHttpMessageHandler).GetMethod(
                "SendAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);


            var task = (Task<HttpResponseMessage>)method!.Invoke(handler,
                new object?[] { null, System.Threading.CancellationToken.None })!;

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => task);
        }

        // -------------------------------------------------------------------
        // Multiple HTTP methods
        // -------------------------------------------------------------------

        [TestMethod]
        [DataRow("GET")]
        [DataRow("POST")]
        [DataRow("PUT")]
        [DataRow("DELETE")]
        [DataRow("PATCH")]
        public async Task SendAsync_VariousMethods_RecordsMethodTag(string method)
        {
            using var handler = CreateHandler(FakeHttpMessageHandler.Ok());
            using var client = new HttpClient(handler);

            using var request = new HttpRequestMessage(new HttpMethod(method), "https://api.example.com/resources");
            await client.SendAsync(request);

            var activity = GetActivity();
            Assert.AreEqual(method, activity.GetTagItem("http.method"));
        }

        // -------------------------------------------------------------------
        // Various HTTP status codes
        // -------------------------------------------------------------------

        [TestMethod]
        [DataRow(200, ActivityStatusCode.Unset)]
        [DataRow(201, ActivityStatusCode.Unset)]
        [DataRow(204, ActivityStatusCode.Unset)]
        [DataRow(301, ActivityStatusCode.Unset)]
        [DataRow(400, ActivityStatusCode.Unset)]
        [DataRow(401, ActivityStatusCode.Unset)]
        [DataRow(403, ActivityStatusCode.Unset)]
        [DataRow(404, ActivityStatusCode.Unset)]
        [DataRow(500, ActivityStatusCode.Error)]
        [DataRow(502, ActivityStatusCode.Error)]
        [DataRow(503, ActivityStatusCode.Error)]
        public async Task SendAsync_VariousStatusCodes_SetsExpectedActivityStatus(
            int statusCode, ActivityStatusCode expectedStatus)
        {
            var inner = FakeHttpMessageHandler.WithStatus((HttpStatusCode)statusCode);
            using var handler = CreateHandler(inner);
            using var client = new HttpClient(handler);

            await client.GetAsync("https://api.example.com/resource");

            var activity = GetActivity();
            Assert.AreEqual(statusCode, activity.GetTagItem("http.status_code"));
            Assert.AreEqual(expectedStatus, activity.Status,
                $"HTTP {statusCode} should map to {expectedStatus}");
        }

        // -------------------------------------------------------------------
        // Helper
        // -------------------------------------------------------------------

        private static TelemetryHttpMessageHandler CreateHandler(
            HttpMessageHandler inner,
            HttpInstrumentationOptions? options = null)
        {
            return new TelemetryHttpMessageHandler(options, null) { InnerHandler = inner };
        }
    }
}
