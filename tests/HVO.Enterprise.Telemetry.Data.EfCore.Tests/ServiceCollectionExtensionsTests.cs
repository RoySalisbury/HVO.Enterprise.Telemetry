using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Data.EfCore.Configuration;
using HVO.Enterprise.Telemetry.Data.EfCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddEfCoreTelemetry_RegistersOptionsValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEfCoreTelemetry();
            var provider = services.BuildServiceProvider();

            // Assert
            var validators = provider.GetServices<IValidateOptions<EfCoreTelemetryOptions>>();
            Assert.IsTrue(
                validators.OfType<EfCoreTelemetryOptionsValidator>().Any(),
                "EfCoreTelemetryOptionsValidator should be registered");
        }

        [TestMethod]
        public void AddEfCoreTelemetry_WithConfigure_AppliesOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEfCoreTelemetry(options =>
            {
                options.RecordConnectionInfo = true;
                options.RecordStatements = false;
            });

            var provider = services.BuildServiceProvider();
            var optionsAccessor = provider.GetRequiredService<IOptions<EfCoreTelemetryOptions>>();

            // Assert
            Assert.IsTrue(optionsAccessor.Value.RecordConnectionInfo);
            Assert.IsFalse(optionsAccessor.Value.RecordStatements);
        }

        [TestMethod]
        public void AddEfCoreTelemetry_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ServiceCollectionExtensions.AddEfCoreTelemetry(null!));
        }

        [TestMethod]
        public void AddEfCoreTelemetry_NullConfigure_UsesDefaults()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEfCoreTelemetry(null);
            services.AddOptions();
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<EfCoreTelemetryOptions>>().Value;

            // Assert
            Assert.IsTrue(options.RecordStatements);
            Assert.IsFalse(options.RecordConnectionInfo);
        }
    }
}
