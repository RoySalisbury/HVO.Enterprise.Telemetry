using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Data.Configuration;
using HVO.Enterprise.Telemetry.Data.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddDataTelemetryBase_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDataTelemetryBase();
            var provider = services.BuildServiceProvider();

            // Assert
            var validators = provider.GetServices<IValidateOptions<DataExtensionOptions>>();
            Assert.IsNotNull(validators);
            Assert.IsTrue(validators.Any());
        }

        [TestMethod]
        public void AddDataTelemetryBase_WithConfigure_AppliesOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDataTelemetryBase(options =>
            {
                options.RecordStatements = false;
                options.MaxStatementLength = 500;
            });

            var provider = services.BuildServiceProvider();
            var optionsAccessor = provider.GetRequiredService<IOptions<DataExtensionOptions>>();

            // Assert
            Assert.IsFalse(optionsAccessor.Value.RecordStatements);
            Assert.AreEqual(500, optionsAccessor.Value.MaxStatementLength);
        }

        [TestMethod]
        public void AddDataTelemetryBase_RegistersOptionsValidator()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDataTelemetryBase();
            var provider = services.BuildServiceProvider();

            // Assert
            var validators = provider.GetServices<IValidateOptions<DataExtensionOptions>>();
            Assert.IsTrue(
                validators.OfType<DataExtensionOptionsValidator>().Any(),
                "DataExtensionOptionsValidator should be registered");
        }

        [TestMethod]
        public void AddDataTelemetryBase_NullServices_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ServiceCollectionExtensions.AddDataTelemetryBase(null!));
        }

        [TestMethod]
        public void AddDataTelemetryBase_NullConfigure_UsesDefaults()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDataTelemetryBase(null);
            services.AddOptions();
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<DataExtensionOptions>>().Value;

            // Assert
            Assert.IsTrue(options.RecordStatements);
            Assert.AreEqual(2000, options.MaxStatementLength);
        }

        [TestMethod]
        public void DataActivitySource_HasExpectedName()
        {
            Assert.AreEqual("HVO.Enterprise.Telemetry.Data", DataActivitySource.BaseName);
        }

        [TestMethod]
        public void DataActivitySource_CreateSource_ReturnsNamedSource()
        {
            // Act
            var source = DataActivitySource.CreateSource("TestTech");

            // Assert
            Assert.IsNotNull(source);
            Assert.AreEqual("HVO.Enterprise.Telemetry.Data.TestTech", source.Name);
        }
    }
}
