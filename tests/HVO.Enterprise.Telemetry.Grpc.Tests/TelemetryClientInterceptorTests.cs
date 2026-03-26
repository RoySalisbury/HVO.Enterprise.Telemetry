using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Grpc;
using HVO.Enterprise.Telemetry.Grpc.Client;

namespace HVO.Enterprise.Telemetry.Grpc.Tests
{
    [TestClass]
    public class TelemetryClientInterceptorTests
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
                new TelemetryClientInterceptor(null!));
        }

        [TestMethod]
        public void Constructor_ValidOptions_DoesNotThrow()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);
            Assert.IsNotNull(interceptor);
        }

        [TestMethod]
        public void Constructor_WithLogger_DoesNotThrow()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options, null);
            Assert.IsNotNull(interceptor);
        }

        [TestMethod]
        public void BlockingUnaryCall_DisabledInterceptor_PassesThrough()
        {
            var options = new GrpcTelemetryOptions { EnableClientInterceptor = false };
            var interceptor = new TelemetryClientInterceptor(options);

            var called = false;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "localhost:50051", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    called = true;
                    return new TestResponse();
                };

            var result = interceptor.BlockingUnaryCall(new TestRequest(), context, continuation);

            Assert.IsTrue(called);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void BlockingUnaryCall_EnabledInterceptor_CreatesActivity()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);

            Activity? capturedActivity = null;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "localhost:50051", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedActivity = Activity.Current;
                    return new TestResponse();
                };

            interceptor.BlockingUnaryCall(new TestRequest(), context, continuation);

            Assert.IsNotNull(capturedActivity);
            Assert.AreEqual(ActivityKind.Client, capturedActivity!.Kind);
            Assert.AreEqual("grpc", capturedActivity.GetTagItem(GrpcActivityTags.RpcSystem));
            Assert.AreEqual("test.TestService", capturedActivity.GetTagItem(GrpcActivityTags.RpcService));
            Assert.AreEqual("TestMethod", capturedActivity.GetTagItem(GrpcActivityTags.RpcMethod));
        }

        [TestMethod]
        public void BlockingUnaryCall_WithHost_SetsServerAddress()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);

            Activity? capturedActivity = null;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "api.example.com:443", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedActivity = Activity.Current;
                    return new TestResponse();
                };

            interceptor.BlockingUnaryCall(new TestRequest(), context, continuation);

            Assert.IsNotNull(capturedActivity);
            Assert.AreEqual("api.example.com", capturedActivity!.GetTagItem(GrpcActivityTags.ServerAddress));
            Assert.AreEqual(443, capturedActivity.GetTagItem(GrpcActivityTags.ServerPort));
        }

        [TestMethod]
        public void BlockingUnaryCall_HostWithoutPort_SetsOnlyAddress()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);

            Activity? capturedActivity = null;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "api.example.com", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedActivity = Activity.Current;
                    return new TestResponse();
                };

            interceptor.BlockingUnaryCall(new TestRequest(), context, continuation);

            Assert.IsNotNull(capturedActivity);
            Assert.AreEqual("api.example.com", capturedActivity!.GetTagItem(GrpcActivityTags.ServerAddress));
            Assert.IsNull(capturedActivity.GetTagItem(GrpcActivityTags.ServerPort));
        }

        [TestMethod]
        public void BlockingUnaryCall_InjectsTraceparent()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);

            Metadata? capturedHeaders = null;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "localhost:50051", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedHeaders = ctx.Options.Headers;
                    return new TestResponse();
                };

            interceptor.BlockingUnaryCall(new TestRequest(), context, continuation);

            Assert.IsNotNull(capturedHeaders);
            var traceparent = capturedHeaders!.FirstOrDefault(e => e.Key == "traceparent");
            Assert.IsNotNull(traceparent);
            Assert.IsTrue(traceparent.Value.StartsWith("00-"));
        }

        [TestMethod]
        public void BlockingUnaryCall_InjectsCorrelationId()
        {
            using var scope = CorrelationContext.BeginScope("client-corr-456");

            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);

            Metadata? capturedHeaders = null;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "localhost:50051", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedHeaders = ctx.Options.Headers;
                    return new TestResponse();
                };

            interceptor.BlockingUnaryCall(new TestRequest(), context, continuation);

            Assert.IsNotNull(capturedHeaders);
            var correlation = capturedHeaders!.FirstOrDefault(e => e.Key == "x-correlation-id");
            Assert.IsNotNull(correlation);
            Assert.AreEqual("client-corr-456", correlation.Value);
        }

        [TestMethod]
        public void BlockingUnaryCall_CustomCorrelationHeaderName_UsesCustomName()
        {
            using var scope = CorrelationContext.BeginScope("custom-corr");

            var options = new GrpcTelemetryOptions { CorrelationHeaderName = "x-request-id" };
            var interceptor = new TelemetryClientInterceptor(options);

            Metadata? capturedHeaders = null;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "localhost:50051", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedHeaders = ctx.Options.Headers;
                    return new TestResponse();
                };

            interceptor.BlockingUnaryCall(new TestRequest(), context, continuation);

            Assert.IsNotNull(capturedHeaders);
            var correlation = capturedHeaders!.FirstOrDefault(e => e.Key == "x-request-id");
            Assert.IsNotNull(correlation);
            Assert.AreEqual("custom-corr", correlation.Value);
        }

        [TestMethod]
        public void BlockingUnaryCall_RpcException_SetsErrorStatus()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);

            Activity? capturedActivity = null;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "localhost:50051", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedActivity = Activity.Current;
                    throw new RpcException(new Status(StatusCode.Unavailable, "Service unavailable"));
                };

            Assert.ThrowsExactly<RpcException>(() =>
                interceptor.BlockingUnaryCall(new TestRequest(), context, continuation));

            Assert.IsNotNull(capturedActivity);
            Assert.AreEqual((int)StatusCode.Unavailable, capturedActivity!.GetTagItem(GrpcActivityTags.RpcGrpcStatusCode));
            Assert.AreEqual(ActivityStatusCode.Error, capturedActivity.Status);
        }

        [TestMethod]
        public void BlockingUnaryCall_GenericException_SetsInternalError()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);

            Activity? capturedActivity = null;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "localhost:50051", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedActivity = Activity.Current;
                    throw new TimeoutException("Connection timed out");
                };

            Assert.ThrowsExactly<TimeoutException>(() =>
                interceptor.BlockingUnaryCall(new TestRequest(), context, continuation));

            Assert.IsNotNull(capturedActivity);
            Assert.AreEqual((int)StatusCode.Internal, capturedActivity!.GetTagItem(GrpcActivityTags.RpcGrpcStatusCode));
            Assert.AreEqual(ActivityStatusCode.Error, capturedActivity.Status);

            var exceptionEvent = capturedActivity.Events.FirstOrDefault(e => e.Name == "exception");
            Assert.AreEqual("exception", exceptionEvent.Name);
        }

        [TestMethod]
        public void BlockingUnaryCall_Success_SetsOkStatus()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);

            Activity? capturedActivity = null;
            var method = CreateTestMethod();
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "localhost:50051", new CallOptions());

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedActivity = Activity.Current;
                    return new TestResponse();
                };

            interceptor.BlockingUnaryCall(new TestRequest(), context, continuation);

            Assert.IsNotNull(capturedActivity);
            Assert.AreEqual((int)StatusCode.OK, capturedActivity!.GetTagItem(GrpcActivityTags.RpcGrpcStatusCode));
        }

        [TestMethod]
        public void BlockingUnaryCall_ExistingHeaders_PreservesHeaders()
        {
            var options = new GrpcTelemetryOptions();
            var interceptor = new TelemetryClientInterceptor(options);

            Metadata? capturedHeaders = null;
            var method = CreateTestMethod();
            var existingHeaders = new Metadata { { "authorization", "Bearer token123" } };
            var context = new ClientInterceptorContext<TestRequest, TestResponse>(
                method, "localhost:50051", new CallOptions(existingHeaders));

            Interceptor.BlockingUnaryCallContinuation<TestRequest, TestResponse> continuation =
                (request, ctx) =>
                {
                    capturedHeaders = ctx.Options.Headers;
                    return new TestResponse();
                };

            interceptor.BlockingUnaryCall(new TestRequest(), context, continuation);

            Assert.IsNotNull(capturedHeaders);
            var auth = capturedHeaders!.FirstOrDefault(e => e.Key == "authorization");
            Assert.IsNotNull(auth);
            Assert.AreEqual("Bearer token123", auth.Value);

            // Should also have traceparent
            var traceparent = capturedHeaders.FirstOrDefault(e => e.Key == "traceparent");
            Assert.IsNotNull(traceparent);
        }

        private static Method<TestRequest, TestResponse> CreateTestMethod()
        {
            return new Method<TestRequest, TestResponse>(
                MethodType.Unary,
                "test.TestService",
                "TestMethod",
                Marshallers.Create(
                    _ => Array.Empty<byte>(),
                    _ => new TestRequest()),
                Marshallers.Create(
                    _ => Array.Empty<byte>(),
                    _ => new TestResponse()));
        }
    }
}
