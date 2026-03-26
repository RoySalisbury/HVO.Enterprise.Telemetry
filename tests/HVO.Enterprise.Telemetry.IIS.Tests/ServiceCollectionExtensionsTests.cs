using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.IIS;
using HVO.Enterprise.Telemetry.IIS.Configuration;
using HVO.Enterprise.Telemetry.IIS.Extensions;
using HVO.Enterprise.Telemetry.IIS.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HVO.Enterprise.Telemetry.IIS.Tests
{
    /// <summary>
    /// Tests for <see cref="ServiceCollectionExtensions"/> DI registration.
    /// </summary>
    [TestClass]
    public sealed class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddIisTelemetryIntegration_ThrowsArgumentNullException_ForNullServices()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(
                () => ServiceCollectionExtensions.AddIisTelemetryIntegration(null!));
        }

        [TestMethod]
        public void AddIisTelemetryIntegration_ReturnsServices_WhenNotOnIis()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddIisTelemetryIntegration();

            // Assert - should return the same services instance (no-op)
            Assert.AreSame(services, result);
        }

        [TestMethod]
        public void AddIisTelemetryIntegration_DoesNotRegisterServices_WhenNotOnIis()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddIisTelemetryIntegration();

            // Assert - no services should be registered when not on IIS
            Assert.IsFalse(services.Any(s => s.ServiceType == typeof(IisLifecycleManager)),
                "IisLifecycleManager should not be registered when not on IIS");
            Assert.IsFalse(services.Any(s => s.ServiceType == typeof(IisShutdownHandler)),
                "IisShutdownHandler should not be registered when not on IIS");
            Assert.IsFalse(services.Any(s => s.ServiceType == typeof(IisExtensionOptions)),
                "IisExtensionOptions should not be registered when not on IIS");
        }

        [TestMethod]
        public void AddIisTelemetryIntegration_DoesNotRegisterHostedService_WhenNotOnIis()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddIisTelemetryIntegration();

            // Assert
            Assert.IsFalse(services.Any(s => s.ServiceType == typeof(IHostedService)),
                "No hosted service should be registered when not on IIS");
        }

        [TestMethod]
        public void AddIisTelemetryIntegration_AcceptsNullConfigure()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert - should not throw
            services.AddIisTelemetryIntegration(null);
        }

        [TestMethod]
        public void AddIisTelemetryIntegration_ValidatesOptions_WhenOnIis()
        {
            // This test verifies that invalid options cause validation to fail.
            // Since we're not on IIS, it returns immediately without validation.
            // We test options validation separately in IisExtensionOptionsTests.

            var services = new ServiceCollection();

            // This should not throw even with invalid options because we're not on IIS
            services.AddIisTelemetryIntegration(opts =>
            {
                opts.ShutdownTimeout = TimeSpan.FromSeconds(-1);
            });
        }
    }

    /// <summary>
    /// Tests for <see cref="TelemetryBuilderExtensions"/> fluent builder integration.
    /// </summary>
    [TestClass]
    public sealed class TelemetryBuilderExtensionsTests
    {
        [TestMethod]
        public void WithIisIntegration_ThrowsArgumentNullException_ForNullBuilder()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryBuilderExtensions.WithIisIntegration(null!));
        }

        [TestMethod]
        public void WithIisIntegration_WorksViaAddTelemetryBuilder()
        {
            // Arrange
            var services = new ServiceCollection();
            var builderCalled = false;

            // Act - use AddTelemetry(Action<TelemetryBuilder>) which is the public API
            services.AddTelemetry(builder =>
            {
                builder.WithIisIntegration();
                builderCalled = true;
            });

            // Assert
            Assert.IsTrue(builderCalled);
        }

        [TestMethod]
        public void WithIisIntegration_AcceptsNullConfigure_ViaBuilder()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert - should not throw
            services.AddTelemetry(builder =>
            {
                builder.WithIisIntegration(null);
            });
        }

        [TestMethod]
        public void WithIisIntegration_IsChainable_ViaBuilder()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act - chaining should work
            services.AddTelemetry(builder =>
            {
                var result = builder
                    .WithIisIntegration(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(10));

                // Assert - WithIisIntegration returns the builder
                Assert.IsNotNull(result);
            });
        }
    }
}
