using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Logging
{
    [TestClass]
    public sealed class TelemetryEnrichedLoggerFactoryTests
    {
        [TestInitialize]
        public void Setup()
        {
            Activity.Current = null;
            CorrelationContext.Clear();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Activity.Current = null;
            CorrelationContext.Clear();
        }

        [TestMethod]
        public void Constructor_NullInnerFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new TelemetryEnrichedLoggerFactory(null!, new TelemetryLoggerOptions()));
        }

        [TestMethod]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new TelemetryEnrichedLoggerFactory(NullLoggerFactory.Instance, null!));
        }

        [TestMethod]
        public void CreateLogger_DelegatesToInnerFactory()
        {
            // Arrange
            var innerFactory = new CapturingLoggerFactory();
            var factory = new TelemetryEnrichedLoggerFactory(innerFactory, new TelemetryLoggerOptions());

            // Act
            var logger = factory.CreateLogger("MyApp.Controller");

            // Assert
            Assert.IsNotNull(logger);
            CollectionAssert.Contains(innerFactory.CreatedCategories, "MyApp.Controller");
        }

        [TestMethod]
        public void CreateLogger_ReturnsEnrichedLogger()
        {
            // Arrange
            var innerFactory = new CapturingLoggerFactory();
            var factory = new TelemetryEnrichedLoggerFactory(innerFactory, new TelemetryLoggerOptions());
            var logger = factory.CreateLogger("TestCategory");
            CorrelationContext.SetRawValue("factory-corr-id");

            // Act
            logger.LogInformation("Factory test");

            // Assert — the inner capturing logger should have an enrichment scope
            var innerLogger = innerFactory.Loggers["TestCategory"];
            var scope = innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.IsTrue(scope.ContainsKey("CorrelationId"));
            Assert.AreEqual("factory-corr-id", scope["CorrelationId"]);
        }

        [TestMethod]
        public void AddProvider_ForwardsToInnerFactory()
        {
            // Arrange
            var innerFactory = new CapturingLoggerFactory();
            var factory = new TelemetryEnrichedLoggerFactory(innerFactory, new TelemetryLoggerOptions());
            var newProvider = new CapturingLoggerProvider();

            // Act
            factory.AddProvider(newProvider);

            // Assert
            Assert.AreEqual(1, innerFactory.AddedProviders.Count);
            Assert.AreSame(newProvider, innerFactory.AddedProviders[0]);
        }

        [TestMethod]
        public void Dispose_DisposesInnerFactory()
        {
            // Arrange
            var innerFactory = new CapturingLoggerFactory();
            var factory = new TelemetryEnrichedLoggerFactory(innerFactory, new TelemetryLoggerOptions());

            // Act
            factory.Dispose();

            // Assert
            Assert.IsTrue(innerFactory.Disposed);
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var innerFactory = new CapturingLoggerFactory();
            var factory = new TelemetryEnrichedLoggerFactory(innerFactory, new TelemetryLoggerOptions());

            // Act & Assert — should not throw
            factory.Dispose();
            factory.Dispose();
        }
    }
}
