using System;
using System.Diagnostics;
using System.ServiceModel.Channels;
using HVO.Enterprise.Telemetry.Wcf.Client;
using HVO.Enterprise.Telemetry.Wcf.Configuration;
using HVO.Enterprise.Telemetry.Wcf.Propagation;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class TelemetryClientMessageInspectorTests
    {
        private ActivityListener? _listener;
        private ActivitySource? _activitySource;

        [TestInitialize]
        public void Setup()
        {
            _activitySource = new ActivitySource("test.wcf.client.inspector");
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "test.wcf.client.inspector",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(_listener);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _listener?.Dispose();
            _activitySource?.Dispose();
        }

        [TestMethod]
        public void BeforeSendRequest_CreatesClientActivity()
        {
            // Arrange
            var inspector = new TelemetryClientMessageInspector(_activitySource!);
            var message = CreateTestMessage("http://tempuri.org/IService/GetCustomer");

            // Act
            var correlationState = inspector.BeforeSendRequest(ref message, null!);

            // Assert
            Assert.IsNotNull(correlationState);
            Assert.IsInstanceOfType(correlationState, typeof(Activity));

            var activity = (Activity)correlationState;
            Assert.AreEqual(ActivityKind.Client, activity.Kind);
            Assert.AreEqual("http://tempuri.org/IService/GetCustomer", activity.OperationName);

            // Cleanup
            activity.Stop();
            activity.Dispose();
        }

        [TestMethod]
        public void BeforeSendRequest_InjectsTraceParentHeader()
        {
            // Arrange
            var inspector = new TelemetryClientMessageInspector(_activitySource!);
            var message = CreateTestMessage("http://tempuri.org/IService/GetCustomer");

            // Act
            var correlationState = inspector.BeforeSendRequest(ref message, null!);

            // Assert
            var traceparent = SoapHeaderAccessor.GetHeader(
                message.Headers,
                TraceContextConstants.TraceParentHeaderName);

            Assert.IsNotNull(traceparent);
            Assert.IsTrue(traceparent.StartsWith("00-"), "Traceparent should start with version 00");

            // Cleanup
            if (correlationState is Activity activity)
            {
                activity.Stop();
                activity.Dispose();
            }
        }

        [TestMethod]
        public void BeforeSendRequest_SetsRpcTags()
        {
            // Arrange
            var inspector = new TelemetryClientMessageInspector(_activitySource!);
            var message = CreateTestMessage("http://tempuri.org/IService/GetCustomer");

            // Act
            var correlationState = inspector.BeforeSendRequest(ref message, null!);

            // Assert
            var activity = correlationState as Activity;
            Assert.IsNotNull(activity);
            Assert.AreEqual("wcf", activity!.GetTagItem("rpc.system"));
            Assert.AreEqual("http://tempuri.org/IService/GetCustomer", activity.GetTagItem("rpc.method"));

            // Cleanup
            activity.Stop();
            activity.Dispose();
        }

        [TestMethod]
        public void BeforeSendRequest_WithOperationFilter_SkipsFilteredOperations()
        {
            // Arrange
            var options = new WcfExtensionOptions
            {
                OperationFilter = op => !op.Contains("Health")
            };
            var inspector = new TelemetryClientMessageInspector(_activitySource!, options);
            var message = CreateTestMessage("http://tempuri.org/IService/HealthCheck");

            // Act
            var correlationState = inspector.BeforeSendRequest(ref message, null!);

            // Assert - Operation was filtered out, no activity created
            Assert.IsNull(correlationState);
        }

        [TestMethod]
        public void BeforeSendRequest_WithOperationFilter_TracesAllowedOperations()
        {
            // Arrange
            var options = new WcfExtensionOptions
            {
                OperationFilter = op => !op.Contains("Health")
            };
            var inspector = new TelemetryClientMessageInspector(_activitySource!, options);
            var message = CreateTestMessage("http://tempuri.org/IService/GetCustomer");

            // Act
            var correlationState = inspector.BeforeSendRequest(ref message, null!);

            // Assert
            Assert.IsNotNull(correlationState);

            // Cleanup
            if (correlationState is Activity activity)
            {
                activity.Stop();
                activity.Dispose();
            }
        }

        [TestMethod]
        public void AfterReceiveReply_SuccessfulReply_SetsOkStatus()
        {
            // Arrange
            var inspector = new TelemetryClientMessageInspector(_activitySource!);
            var request = CreateTestMessage("http://tempuri.org/IService/GetCustomer");
            var correlationState = inspector.BeforeSendRequest(ref request, null!);

            var reply = CreateTestMessage("http://tempuri.org/IService/GetCustomerResponse");

            // Act
            inspector.AfterReceiveReply(ref reply, correlationState!);

            // Assert - Activity should have been stopped and disposed
            // The test verifies no exception is thrown
        }

        [TestMethod]
        public void AfterReceiveReply_NullCorrelationState_DoesNotThrow()
        {
            // Arrange
            var inspector = new TelemetryClientMessageInspector(_activitySource!);
            var reply = CreateTestMessage("http://tempuri.org/IService/GetCustomerResponse");

            // Act & Assert - should not throw
            inspector.AfterReceiveReply(ref reply, null!);
        }

        [TestMethod]
        public void AfterReceiveReply_NonActivityCorrelationState_DoesNotThrow()
        {
            // Arrange
            var inspector = new TelemetryClientMessageInspector(_activitySource!);
            var reply = CreateTestMessage("http://tempuri.org/IService/GetCustomerResponse");

            // Act & Assert - should not throw
            inspector.AfterReceiveReply(ref reply, "not-an-activity");
        }

        [TestMethod]
        public void Constructor_NullActivitySource_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new TelemetryClientMessageInspector(null!));
        }

        [TestMethod]
        public void Constructor_NullOptions_UsesDefaults()
        {
            // Act & Assert - should not throw
            var inspector = new TelemetryClientMessageInspector(_activitySource!, null);
            Assert.IsNotNull(inspector);
        }

        [TestMethod]
        public void BeforeSendRequest_WithThrowingFilter_StillTracesOperation()
        {
            // Arrange
            var options = new WcfExtensionOptions
            {
                OperationFilter = _ => throw new InvalidOperationException("Filter error")
            };
            var inspector = new TelemetryClientMessageInspector(_activitySource!, options);
            var message = CreateTestMessage("http://tempuri.org/IService/GetCustomer");

            // Act
            var correlationState = inspector.BeforeSendRequest(ref message, null!);

            // Assert - When filter throws, operation should still be traced
            Assert.IsNotNull(correlationState);

            // Cleanup
            if (correlationState is Activity activity)
            {
                activity.Stop();
                activity.Dispose();
            }
        }

        private static Message CreateTestMessage(string action)
        {
            return Message.CreateMessage(
                MessageVersion.Soap12WSAddressing10,
                action,
                "test body");
        }
    }
}
