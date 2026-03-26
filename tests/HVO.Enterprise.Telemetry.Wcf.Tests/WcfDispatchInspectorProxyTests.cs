using System;
using System.Diagnostics;
using System.Reflection;
using HVO.Enterprise.Telemetry.Wcf.Configuration;
using HVO.Enterprise.Telemetry.Wcf.Server;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class WcfDispatchInspectorProxyTests
    {
        [TestMethod]
        public void Proxy_IsAssignableFromDispatchProxy()
        {
            // Assert
            Assert.IsTrue(typeof(DispatchProxy).IsAssignableFrom(typeof(WcfDispatchInspectorProxy)));
        }

        [TestMethod]
        public void Proxy_CanBeInstantiatedDirectly()
        {
            // Act
            var proxy = new WcfDispatchInspectorProxy();

            // Assert
            Assert.IsNotNull(proxy);
        }

        [TestMethod]
        public void Initialize_SetsInternalState()
        {
            // Arrange
            var proxy = new WcfDispatchInspectorProxy();
            using var source = new ActivitySource("test.proxy");
            var options = new WcfExtensionOptions();

            // Act & Assert - should not throw
            proxy.Initialize(source, options);
        }

        [TestMethod]
        public void Initialize_NullActivitySource_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                // Arrange
                var proxy = new WcfDispatchInspectorProxy();

                // Act
                proxy.Initialize(null!, new WcfExtensionOptions());
            });
        }

        [TestMethod]
        public void Initialize_NullOptions_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                // Arrange
                var proxy = new WcfDispatchInspectorProxy();
                using var source = new ActivitySource("test.proxy");

                // Act
                proxy.Initialize(source, null!);
            });
        }
    }
}
