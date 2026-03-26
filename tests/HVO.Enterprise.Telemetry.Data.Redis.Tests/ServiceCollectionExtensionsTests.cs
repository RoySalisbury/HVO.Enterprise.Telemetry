using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Data.Redis;
using HVO.Enterprise.Telemetry.Data.Redis.Configuration;
using HVO.Enterprise.Telemetry.Data.Redis.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.Redis.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddRedisTelemetry_RegistersOptionsValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRedisTelemetry();
            var provider = services.BuildServiceProvider();

            // Assert
            var validators = provider.GetServices<IValidateOptions<RedisTelemetryOptions>>();
            Assert.IsTrue(
                validators.OfType<RedisTelemetryOptionsValidator>().Any(),
                "RedisTelemetryOptionsValidator should be registered");
        }

        [TestMethod]
        public void AddRedisTelemetry_WithConfigure_AppliesOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRedisTelemetry(options =>
            {
                options.RecordCommands = false;
                options.RecordKeys = false;
            });

            var provider = services.BuildServiceProvider();
            var optionsAccessor = provider.GetRequiredService<IOptions<RedisTelemetryOptions>>();

            // Assert
            Assert.IsFalse(optionsAccessor.Value.RecordCommands);
            Assert.IsFalse(optionsAccessor.Value.RecordKeys);
        }

        [TestMethod]
        public void AddRedisTelemetry_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ServiceCollectionExtensions.AddRedisTelemetry(null!));
        }

        [TestMethod]
        public void RedisTelemetryOptions_Defaults_AreCorrect()
        {
            // Arrange & Act
            var options = new RedisTelemetryOptions();

            // Assert
            Assert.IsTrue(options.RecordKeys);
            Assert.AreEqual(100, options.MaxKeyLength);
            Assert.IsTrue(options.RecordCommands);
            Assert.IsTrue(options.RecordDatabaseIndex);
            Assert.IsTrue(options.RecordEndpoint);
        }

        [TestMethod]
        public void RedisTelemetryOptionsValidator_ValidOptions_ReturnsSuccess()
        {
            // Arrange
            var validator = new RedisTelemetryOptionsValidator();
            var options = new RedisTelemetryOptions();

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void RedisTelemetryOptionsValidator_NullOptions_ReturnsFail()
        {
            // Arrange
            var validator = new RedisTelemetryOptionsValidator();

            // Act
            var result = validator.Validate(null, null!);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void RedisTelemetryOptionsValidator_MaxKeyLengthTooLow_ReturnsFail()
        {
            // Arrange
            var validator = new RedisTelemetryOptionsValidator();
            var options = new RedisTelemetryOptions { MaxKeyLength = 5 };

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void RedisTelemetryOptionsValidator_MaxKeyLengthTooHigh_ReturnsFail()
        {
            // Arrange
            var validator = new RedisTelemetryOptionsValidator();
            var options = new RedisTelemetryOptions { MaxKeyLength = 5000 };

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void RedisActivitySource_HasExpectedName()
        {
            Assert.AreEqual("HVO.Enterprise.Telemetry.Data.Redis", RedisActivitySource.Name);
        }

        [TestMethod]
        public void RedisActivitySource_SourceNotNull()
        {
            Assert.IsNotNull(RedisActivitySource.Source);
        }
    }
}
