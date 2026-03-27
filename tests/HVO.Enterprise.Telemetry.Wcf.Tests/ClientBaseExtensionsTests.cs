using System;
using System.Linq;
using System.ServiceModel.Description;
using HVO.Enterprise.Telemetry.Wcf.Client;
using HVO.Enterprise.Telemetry.Wcf.Configuration;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class ClientBaseExtensionsTests
    {
        [TestMethod]
        public void AddTelemetryBehavior_NullEndpoint_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ClientBaseExtensions.AddTelemetryBehavior(null!));
        }

        [TestMethod]
        public void AddTelemetryBehavior_ReturnsEndpointForChaining()
        {
            // Arrange
            var endpoint = CreateTestEndpoint();

            // Act
            var result = endpoint.AddTelemetryBehavior();

            // Assert
            Assert.AreSame(endpoint, result);
        }

        [TestMethod]
        public void AddTelemetryBehavior_AddsBehaviorToEndpoint()
        {
            // Arrange
            var endpoint = CreateTestEndpoint();

            // Act
            endpoint.AddTelemetryBehavior();

            // Assert
            Assert.IsTrue(
                endpoint.EndpointBehaviors.OfType<TelemetryClientEndpointBehavior>().Any(),
                "TelemetryClientEndpointBehavior should be added");
        }

        [TestMethod]
        public void AddTelemetryBehavior_WithOptions_PassesOptionsThrough()
        {
            // Arrange
            var endpoint = CreateTestEndpoint();
            var options = new WcfExtensionOptions
            {
                PropagateTraceContextInReply = false
            };

            // Act
            endpoint.AddTelemetryBehavior(options);

            // Assert
            Assert.IsTrue(
                endpoint.EndpointBehaviors.OfType<TelemetryClientEndpointBehavior>().Any());
        }

        private static ServiceEndpoint CreateTestEndpoint()
        {
            // Create a minimal ServiceEndpoint for testing
            var contract = new ContractDescription("ITestService");
            var endpoint = new ServiceEndpoint(contract);
            return endpoint;
        }
    }
}
