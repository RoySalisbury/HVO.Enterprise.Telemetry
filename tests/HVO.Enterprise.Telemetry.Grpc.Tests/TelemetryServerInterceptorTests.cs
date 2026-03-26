using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Grpc;
using HVO.Enterprise.Telemetry.Grpc.Server;

namespace HVO.Enterprise.Telemetry.Grpc.Tests
{
    [TestClass]
    public class TelemetryServerInterceptorTests
    {
        private ActivityListener? _listener;

        [TestInitialize]
        public void Setup()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "HVO.Enterprise.Telemetry.Grpc",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(_listener);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _listener?.Dispose();
        }

        [TestMethod]
        public void Constructor_NullOptions_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new TelemetryServerInterceptor(null!));
        }

        [TestMethod]
        public void Constructor_ValidOptions_DoesNotThrow()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryServerInterceptor(options);
            Assert.IsNotNull(interceptor);
        }

        [TestMethod]
        public void Constructor_WithLogger_DoesNotThrow()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryServerInterceptor(options, null);
            Assert.IsNotNull(interceptor);
        }

        [TestMethod]
        public void ShouldSuppress_HealthCheck_WithSuppressEnabled_ReturnsTrue()
        {
            var options = new GrpcTelemetryOptions { SuppressHealthChecks = true };
            var interceptor = new TelemetryServerInterceptor(options);

            Assert.IsTrue(interceptor.ShouldSuppress("/grpc.health.v1.Health/Check"));
        }

        [TestMethod]
        public void ShouldSuppress_HealthCheck_WithSuppressDisabled_ReturnsFalse()
        {
            var options = new GrpcTelemetryOptions { SuppressHealthChecks = false };
            var interceptor = new TelemetryServerInterceptor(options);

            Assert.IsFalse(interceptor.ShouldSuppress("/grpc.health.v1.Health/Check"));
        }

        [TestMethod]
        public void ShouldSuppress_Reflection_WithSuppressEnabled_ReturnsTrue()
        {
            var options = new GrpcTelemetryOptions { SuppressReflection = true };
            var interceptor = new TelemetryServerInterceptor(options);

            Assert.IsTrue(interceptor.ShouldSuppress("/grpc.reflection.v1alpha.ServerReflection/ServerReflectionInfo"));
        }

        [TestMethod]
        public void ShouldSuppress_Reflection_WithSuppressDisabled_ReturnsFalse()
        {
            var options = new GrpcTelemetryOptions { SuppressReflection = false };
            var interceptor = new TelemetryServerInterceptor(options);

            Assert.IsFalse(interceptor.ShouldSuppress("/grpc.reflection.v1alpha.ServerReflection/ServerReflectionInfo"));
        }

        [TestMethod]
        public void ShouldSuppress_NormalMethod_ReturnsFalse()
        {
            var options = new GrpcTelemetryOptions
            {
                SuppressHealthChecks = true,
                SuppressReflection = true
            };
            var interceptor = new TelemetryServerInterceptor(options);

            Assert.IsFalse(interceptor.ShouldSuppress("/mypackage.OrderService/GetOrder"));
        }

        [TestMethod]
        public async Task UnaryServerHandler_DisabledInterceptor_PassesThrough()
        {
            var options = new GrpcTelemetryOptions { EnableServerInterceptor = false };
            var interceptor = new TelemetryServerInterceptor(options);

            var called = false;
            UnaryServerMethod<TestRequest, TestResponse> continuation = (request, context) =>
            {
                called = true;
                return Task.FromResult(new TestResponse());
            };

            var mockContext = CreateMockServerCallContext("/test.Service/Method");
            var result = await interceptor.UnaryServerHandler(
                new TestRequest(), mockContext, continuation);

            Assert.IsTrue(called);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task UnaryServerHandler_SuppressedMethod_PassesThrough()
        {
            var options = new GrpcTelemetryOptions { SuppressHealthChecks = true };
            var interceptor = new TelemetryServerInterceptor(options);

            var called = false;
            UnaryServerMethod<TestRequest, TestResponse> continuation = (request, context) =>
            {
                called = true;
                return Task.FromResult(new TestResponse());
            };

            var mockContext = CreateMockServerCallContext("/grpc.health.v1.Health/Check");
            var result = await interceptor.UnaryServerHandler(
                new TestRequest(), mockContext, continuation);

            Assert.IsTrue(called);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task UnaryServerHandler_NormalCall_CreatesActivity()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryServerInterceptor(options);

            Activity? capturedActivity = null;
            UnaryServerMethod<TestRequest, TestResponse> continuation = (request, context) =>
            {
                capturedActivity = Activity.Current;
                return Task.FromResult(new TestResponse());
            };

            var mockContext = CreateMockServerCallContext("/mypackage.OrderService/GetOrder");
            await interceptor.UnaryServerHandler(new TestRequest(), mockContext, continuation);

            Assert.IsNotNull(capturedActivity);
            Assert.AreEqual("grpc", capturedActivity!.GetTagItem(GrpcActivityTags.RpcSystem));
            Assert.AreEqual("mypackage.OrderService", capturedActivity.GetTagItem(GrpcActivityTags.RpcService));
            Assert.AreEqual("GetOrder", capturedActivity.GetTagItem(GrpcActivityTags.RpcMethod));
            Assert.AreEqual((int)StatusCode.OK, capturedActivity.GetTagItem(GrpcActivityTags.RpcGrpcStatusCode));
        }

        [TestMethod]
        public async Task UnaryServerHandler_WithCorrelationHeader_SetsCorrelationContext()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryServerInterceptor(options);

            string? capturedCorrelation = null;
            UnaryServerMethod<TestRequest, TestResponse> continuation = (request, context) =>
            {
                capturedCorrelation = CorrelationContext.Current;
                return Task.FromResult(new TestResponse());
            };

            var metadata = new Metadata
            {
                { "x-correlation-id", "test-corr-789" }
            };
            var mockContext = CreateMockServerCallContext("/test.Service/Method", metadata);
            await interceptor.UnaryServerHandler(new TestRequest(), mockContext, continuation);

            Assert.AreEqual("test-corr-789", capturedCorrelation);
        }

        [TestMethod]
        public async Task UnaryServerHandler_WithTraceparent_ExtractsContext()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryServerInterceptor(options);

            Activity? capturedActivity = null;
            UnaryServerMethod<TestRequest, TestResponse> continuation = (request, context) =>
            {
                capturedActivity = Activity.Current;
                return Task.FromResult(new TestResponse());
            };

            var metadata = new Metadata
            {
                { "traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" }
            };
            var mockContext = CreateMockServerCallContext("/test.Service/Method", metadata);
            await interceptor.UnaryServerHandler(new TestRequest(), mockContext, continuation);

            Assert.IsNotNull(capturedActivity);
            // The activity's parent should have the extracted trace ID
            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", capturedActivity!.TraceId.ToString());
        }

        [TestMethod]
        public async Task UnaryServerHandler_RpcException_SetsErrorStatus()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryServerInterceptor(options);

            Activity? capturedActivity = null;
            UnaryServerMethod<TestRequest, TestResponse> continuation = (request, context) =>
            {
                capturedActivity = Activity.Current;
                throw new RpcException(new Status(StatusCode.NotFound, "Not found"));
            };

            var mockContext = CreateMockServerCallContext("/test.Service/GetItem");

            await Assert.ThrowsExactlyAsync<RpcException>(async () =>
                await interceptor.UnaryServerHandler(new TestRequest(), mockContext, continuation));

            Assert.IsNotNull(capturedActivity);
            Assert.AreEqual((int)StatusCode.NotFound, capturedActivity!.GetTagItem(GrpcActivityTags.RpcGrpcStatusCode));
            Assert.AreEqual(ActivityStatusCode.Error, capturedActivity.Status);
        }

        [TestMethod]
        public async Task UnaryServerHandler_GenericException_SetsInternalError()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryServerInterceptor(options);

            Activity? capturedActivity = null;
            UnaryServerMethod<TestRequest, TestResponse> continuation = (request, context) =>
            {
                capturedActivity = Activity.Current;
                throw new InvalidOperationException("Something went wrong");
            };

            var mockContext = CreateMockServerCallContext("/test.Service/GetItem");

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
                await interceptor.UnaryServerHandler(new TestRequest(), mockContext, continuation));

            Assert.IsNotNull(capturedActivity);
            Assert.AreEqual((int)StatusCode.Internal, capturedActivity!.GetTagItem(GrpcActivityTags.RpcGrpcStatusCode));
            Assert.AreEqual(ActivityStatusCode.Error, capturedActivity.Status);

            // Verify exception event was recorded
            var exceptionEvent = capturedActivity.Events.FirstOrDefault(e => e.Name == "exception");
            Assert.AreEqual("exception", exceptionEvent.Name);
        }

        private static ServerCallContext CreateMockServerCallContext(string method, Metadata? headers = null)
        {
            return new TestServerCallContext(method, headers ?? new Metadata());
        }
    }

    /// <summary>
    /// Simple test request type for gRPC interceptor testing.
    /// </summary>
    public class TestRequest { }

    /// <summary>
    /// Simple test response type for gRPC interceptor testing.
    /// </summary>
    public class TestResponse { }

    /// <summary>
    /// Minimal test implementation of <see cref="ServerCallContext"/> for unit testing.
    /// </summary>
    internal sealed class TestServerCallContext : ServerCallContext
    {
        private readonly string _method;
        private readonly Metadata _requestHeaders;

        public TestServerCallContext(string method, Metadata requestHeaders)
        {
            _method = method;
            _requestHeaders = requestHeaders;
        }

        protected override string MethodCore => _method;
        protected override string HostCore => "localhost";
        protected override string PeerCore => "127.0.0.1:50051";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => _requestHeaders;
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new Metadata();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new AuthContext(null, new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotImplementedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
