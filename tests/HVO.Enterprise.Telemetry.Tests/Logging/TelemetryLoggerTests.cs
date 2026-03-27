using System;
using HVO.Enterprise.Telemetry.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Logging
{
    [TestClass]
    public sealed class TelemetryLoggerTests
    {
        // --- CreateEnrichedLogger ---

        [TestMethod]
        public void CreateEnrichedLogger_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                TelemetryLogger.CreateEnrichedLogger(null!));
        }

        [TestMethod]
        public void CreateEnrichedLogger_WithDefaults_ReturnsILogger()
        {
            // Arrange
            var innerLogger = NullLogger.Instance;

            // Act
            var enrichedLogger = TelemetryLogger.CreateEnrichedLogger(innerLogger);

            // Assert
            Assert.IsNotNull(enrichedLogger);
            Assert.IsInstanceOfType(enrichedLogger, typeof(ILogger));
        }

        [TestMethod]
        public void CreateEnrichedLogger_WithOptions_RespectsConfiguration()
        {
            // Arrange
            var innerLogger = new CapturingLogger();
            var options = new TelemetryLoggerOptions { EnableEnrichment = false };

            // Act
            var enrichedLogger = TelemetryLogger.CreateEnrichedLogger(innerLogger, options);
            enrichedLogger.LogInformation("Test");

            // Assert — enrichment disabled, no scope created
            Assert.AreEqual(1, innerLogger.LogEntries.Count);
            Assert.AreEqual(0, innerLogger.Scopes.Count);
        }

        [TestMethod]
        public void CreateEnrichedLogger_NullOptions_UsesDefaults()
        {
            // Arrange
            var innerLogger = NullLogger.Instance;

            // Act — should not throw
            var enrichedLogger = TelemetryLogger.CreateEnrichedLogger(innerLogger, null);

            // Assert
            Assert.IsNotNull(enrichedLogger);
        }

        // --- CreateEnrichedLoggerFactory ---

        [TestMethod]
        public void CreateEnrichedLoggerFactory_NullFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                TelemetryLogger.CreateEnrichedLoggerFactory(null!));
        }

        [TestMethod]
        public void CreateEnrichedLoggerFactory_WithDefaults_ReturnsILoggerFactory()
        {
            // Arrange
            var innerFactory = NullLoggerFactory.Instance;

            // Act
            var enrichedFactory = TelemetryLogger.CreateEnrichedLoggerFactory(innerFactory);

            // Assert
            Assert.IsNotNull(enrichedFactory);
            Assert.IsInstanceOfType(enrichedFactory, typeof(ILoggerFactory));
        }

        [TestMethod]
        public void CreateEnrichedLoggerFactory_CreatesEnrichedLoggers()
        {
            // Arrange
            var innerFactory = new CapturingLoggerFactory();
            var enrichedFactory = TelemetryLogger.CreateEnrichedLoggerFactory(innerFactory);

            // Act
            var logger = enrichedFactory.CreateLogger("MyApp.Service");

            // Assert
            Assert.IsNotNull(logger);
            CollectionAssert.Contains(innerFactory.CreatedCategories, "MyApp.Service");
        }

        [TestMethod]
        public void CreateEnrichedLoggerFactory_NullOptions_UsesDefaults()
        {
            // Arrange
            var innerFactory = NullLoggerFactory.Instance;

            // Act — should not throw
            var enrichedFactory = TelemetryLogger.CreateEnrichedLoggerFactory(innerFactory, null);

            // Assert
            Assert.IsNotNull(enrichedFactory);
        }
    }
}
