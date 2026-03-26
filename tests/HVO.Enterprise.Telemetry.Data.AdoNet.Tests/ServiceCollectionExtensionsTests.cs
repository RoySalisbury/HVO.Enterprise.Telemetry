using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Data.AdoNet.Configuration;
using HVO.Enterprise.Telemetry.Data.AdoNet.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddAdoNetTelemetry_RegistersOptionsValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddAdoNetTelemetry();
            var provider = services.BuildServiceProvider();

            // Assert
            var validators = provider.GetServices<IValidateOptions<AdoNetTelemetryOptions>>();
            Assert.IsTrue(
                validators.OfType<AdoNetTelemetryOptionsValidator>().Any(),
                "AdoNetTelemetryOptionsValidator should be registered");
        }

        [TestMethod]
        public void AddAdoNetTelemetry_WithConfigure_AppliesOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddAdoNetTelemetry(options =>
            {
                options.RecordStatements = false;
                options.RecordConnectionInfo = true;
            });

            var provider = services.BuildServiceProvider();
            var optionsAccessor = provider.GetRequiredService<IOptions<AdoNetTelemetryOptions>>();

            // Assert
            Assert.IsFalse(optionsAccessor.Value.RecordStatements);
            Assert.IsTrue(optionsAccessor.Value.RecordConnectionInfo);
        }

        [TestMethod]
        public void AddAdoNetTelemetry_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ServiceCollectionExtensions.AddAdoNetTelemetry(null!));
        }

        [TestMethod]
        public void AdoNetTelemetryOptions_Defaults_AreCorrect()
        {
            // Arrange & Act
            var options = new AdoNetTelemetryOptions();

            // Assert
            Assert.IsTrue(options.RecordStatements);
            Assert.IsFalse(options.RecordConnectionInfo);
            Assert.IsFalse(options.RecordParameters);
        }

        [TestMethod]
        public void AdoNetTelemetryOptionsValidator_ValidOptions_ReturnsSuccess()
        {
            // Arrange
            var validator = new AdoNetTelemetryOptionsValidator();
            var options = new AdoNetTelemetryOptions();

            // Act
            var result = validator.Validate(null, options);

            // Assert
            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void AdoNetTelemetryOptionsValidator_NullOptions_ReturnsFail()
        {
            // Arrange
            var validator = new AdoNetTelemetryOptionsValidator();

            // Act
            var result = validator.Validate(null, null!);

            // Assert
            Assert.IsTrue(result.Failed);
        }
    }
}
