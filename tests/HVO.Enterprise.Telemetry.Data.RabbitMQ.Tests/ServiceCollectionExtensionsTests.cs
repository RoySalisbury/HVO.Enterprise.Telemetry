using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Configuration;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddRabbitMqTelemetry_RegistersOptionsValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRabbitMqTelemetry();
            var provider = services.BuildServiceProvider();

            // Assert
            var validators = provider.GetServices<IValidateOptions<RabbitMqTelemetryOptions>>();
            Assert.IsTrue(
                validators.OfType<RabbitMqTelemetryOptionsValidator>().Any(),
                "RabbitMqTelemetryOptionsValidator should be registered");
        }

        [TestMethod]
        public void AddRabbitMqTelemetry_WithConfigure_AppliesOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRabbitMqTelemetry(options =>
            {
                options.PropagateTraceContext = false;
                options.RecordRoutingKey = false;
            });

            var provider = services.BuildServiceProvider();
            var optionsAccessor = provider.GetRequiredService<IOptions<RabbitMqTelemetryOptions>>();

            // Assert
            Assert.IsFalse(optionsAccessor.Value.PropagateTraceContext);
            Assert.IsFalse(optionsAccessor.Value.RecordRoutingKey);
        }

        [TestMethod]
        public void AddRabbitMqTelemetry_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ServiceCollectionExtensions.AddRabbitMqTelemetry(null!));
        }

        [TestMethod]
        public void RabbitMqTelemetryOptions_Defaults_AreCorrect()
        {
            // Arrange & Act
            var options = new RabbitMqTelemetryOptions();

            // Assert
            Assert.IsTrue(options.PropagateTraceContext);
            Assert.IsTrue(options.RecordExchange);
            Assert.IsTrue(options.RecordRoutingKey);
            Assert.IsTrue(options.RecordBodySize);
            Assert.IsTrue(options.RecordMessageIds);
            Assert.IsTrue(options.RecordQueueName);
        }

        [TestMethod]
        public void RabbitMqTelemetryOptions_AllProperties_AreSettable()
        {
            // Arrange & Act
            var options = new RabbitMqTelemetryOptions
            {
                PropagateTraceContext = false,
                RecordExchange = false,
                RecordRoutingKey = false,
                RecordBodySize = false,
                RecordMessageIds = false,
                RecordQueueName = false
            };

            // Assert
            Assert.IsFalse(options.PropagateTraceContext);
            Assert.IsFalse(options.RecordExchange);
            Assert.IsFalse(options.RecordRoutingKey);
            Assert.IsFalse(options.RecordBodySize);
            Assert.IsFalse(options.RecordMessageIds);
            Assert.IsFalse(options.RecordQueueName);
        }

        [TestMethod]
        public void RabbitMqTelemetryOptionsValidator_ValidOptions_ReturnsSuccess()
        {
            // Arrange
            var validator = new RabbitMqTelemetryOptionsValidator();
            var options = new RabbitMqTelemetryOptions();

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void RabbitMqTelemetryOptionsValidator_NullOptions_ReturnsFail()
        {
            // Arrange
            var validator = new RabbitMqTelemetryOptionsValidator();

            // Act
            var result = validator.Validate(null, null!);

            // Assert
            Assert.IsTrue(result.Failed);
        }
    }
}
