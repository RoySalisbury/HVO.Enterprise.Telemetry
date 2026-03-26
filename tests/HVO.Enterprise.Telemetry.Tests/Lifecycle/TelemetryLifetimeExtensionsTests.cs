using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Lifecycle
{
    [TestClass]
    public class TelemetryLifetimeExtensionsTests
    {
        [TestMethod]
        public void AddTelemetryLifetime_WithNullServices_ThrowsException()
        {
            // Act
            Assert.ThrowsExactly<ArgumentNullException>(() => TelemetryLifetimeExtensions.AddTelemetryLifetime(null!));
        }

        [TestMethod]
        public void AddTelemetryLifetime_RegistersHostedService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTelemetryLifetime();

            // Assert
            // Verify the hosted service is registered in the collection
            var hasHostedService = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                .Any();

            Assert.IsTrue(hasHostedService, "IHostedService should be registered");
        }

        [TestMethod]
        public void AddTelemetryLifetime_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddTelemetryLifetime();

            // Assert
            Assert.AreSame(services, result, "Should return the same service collection for chaining");
        }

        [TestMethod]
        public void AddTelemetryLifetime_IsIdempotent()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act - Call multiple times
            services.AddTelemetryLifetime();
            services.AddTelemetryLifetime();

            // Assert - Check that services are registered only once
            var hostedServiceCount = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                .Count();

            Assert.AreEqual(1, hostedServiceCount, "Only one hosted service should be registered when called multiple times");
        }

        [TestMethod]
        public void AddTelemetryLifetime_SupportsMethodChaining()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddTelemetryLifetime();

            // Assert
            Assert.AreSame(services, result, "Should return service collection for chaining");
        }
    }
}
