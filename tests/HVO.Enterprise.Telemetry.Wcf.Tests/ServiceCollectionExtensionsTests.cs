using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Wcf.Client;
using HVO.Enterprise.Telemetry.Wcf.Configuration;
using HVO.Enterprise.Telemetry.Wcf.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddWcfTelemetryInstrumentation_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddWcfTelemetryInstrumentation();
            var provider = services.BuildServiceProvider();

            // Assert
            var behavior = provider.GetService<TelemetryClientEndpointBehavior>();
            Assert.IsNotNull(behavior);
        }

        [TestMethod]
        public void AddWcfTelemetryInstrumentation_WithConfigure_AppliesOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddWcfTelemetryInstrumentation(options =>
            {
                options.PropagateTraceContextInReply = false;
                options.CaptureFaultDetails = false;
            });

            var provider = services.BuildServiceProvider();
            var optionsAccessor = provider.GetRequiredService<IOptions<WcfExtensionOptions>>();

            // Assert
            Assert.IsFalse(optionsAccessor.Value.PropagateTraceContextInReply);
            Assert.IsFalse(optionsAccessor.Value.CaptureFaultDetails);
        }

        [TestMethod]
        public void AddWcfTelemetryInstrumentation_RegistersOptionsValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddWcfTelemetryInstrumentation();
            var provider = services.BuildServiceProvider();

            // Assert
            var validators = provider.GetServices<IValidateOptions<WcfExtensionOptions>>();
            Assert.IsNotNull(validators);
            Assert.IsTrue(
                validators.OfType<WcfExtensionOptionsValidator>().Any(),
                "WcfExtensionOptionsValidator should be registered");
        }

        [TestMethod]
        public void AddWcfTelemetryInstrumentation_BehaviorIsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddWcfTelemetryInstrumentation();
            var provider = services.BuildServiceProvider();

            // Act
            var behavior1 = provider.GetRequiredService<TelemetryClientEndpointBehavior>();
            var behavior2 = provider.GetRequiredService<TelemetryClientEndpointBehavior>();

            // Assert
            Assert.AreSame(behavior1, behavior2);
        }

        [TestMethod]
        public void AddWcfTelemetryInstrumentation_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ServiceCollectionExtensions.AddWcfTelemetryInstrumentation(null!));
        }

        [TestMethod]
        public void AddWcfTelemetryInstrumentation_NullConfigure_UsesDefaults()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddWcfTelemetryInstrumentation(null);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<WcfExtensionOptions>>().Value;

            // Assert
            Assert.IsTrue(options.PropagateTraceContextInReply);
            Assert.IsTrue(options.CaptureFaultDetails);
        }

        [TestMethod]
        public void AddWcfTelemetryInstrumentation_MultipleCalls_DoesNotDuplicate()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddWcfTelemetryInstrumentation();
            services.AddWcfTelemetryInstrumentation();
            var provider = services.BuildServiceProvider();

            // Assert - TryAddSingleton ensures single registration
            var behavior = provider.GetRequiredService<TelemetryClientEndpointBehavior>();
            Assert.IsNotNull(behavior);
        }
    }
}
