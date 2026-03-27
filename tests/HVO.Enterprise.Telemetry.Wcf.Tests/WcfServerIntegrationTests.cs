using System;
using HVO.Enterprise.Telemetry.Wcf.Server;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class WcfServerIntegrationTests
    {
        [TestMethod]
        public void IsWcfServerAvailable_OnNonFramework_ReturnsFalse()
        {
            // Assert - On .NET 8 test host, server-side WCF types are not available
            Assert.IsFalse(WcfServerIntegration.IsWcfServerAvailable);
        }

        [TestMethod]
        public void TryAddTelemetryInspector_NullServiceHost_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => WcfServerIntegration.TryAddTelemetryInspector(null!));
        }

        [TestMethod]
        public void TryAddTelemetryInspector_WhenWcfNotAvailable_ReturnsFalse()
        {
            // Arrange
            var fakeHost = new object();

            // Act
            var result = WcfServerIntegration.TryAddTelemetryInspector(fakeHost);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CreateDispatchInspectorProxy_WhenWcfNotAvailable_ReturnsNull()
        {
            // Act
            var proxy = WcfServerIntegration.CreateDispatchInspectorProxy(
                new Configuration.WcfExtensionOptions());

            // Assert - On .NET 8 test host, should return null
            Assert.IsNull(proxy);
        }
    }
}
