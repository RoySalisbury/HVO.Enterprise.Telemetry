using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Logging
{
    [TestClass]
    public sealed class TelemetryEnrichedLoggerProviderTests
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
        public void Constructor_NullInnerProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new TelemetryEnrichedLoggerProvider(null!));
        }

        [TestMethod]
        public void Constructor_NullOptions_UsesDefaults()
        {
            // Arrange
            var innerProvider = new CapturingLoggerProvider();

            // Act — should not throw
            var provider = new TelemetryEnrichedLoggerProvider(innerProvider, null);

            // Assert — can create loggers
            var logger = provider.CreateLogger("TestCategory");
            Assert.IsNotNull(logger);
        }

        [TestMethod]
        public void CreateLogger_ReturnsTelemetryEnrichedLogger()
        {
            // Arrange
            var innerProvider = new CapturingLoggerProvider();
            var provider = new TelemetryEnrichedLoggerProvider(innerProvider);

            // Act
            var logger = provider.CreateLogger("MyApp.Service");

            // Assert — verify the inner provider was called
            Assert.IsNotNull(logger);
            CollectionAssert.Contains(innerProvider.CreatedCategories, "MyApp.Service");
        }

        [TestMethod]
        public void CreateLogger_CachesSameCategoryLogger()
        {
            // Arrange
            var innerProvider = new CapturingLoggerProvider();
            var provider = new TelemetryEnrichedLoggerProvider(innerProvider);

            // Act
            var logger1 = provider.CreateLogger("CachedCategory");
            var logger2 = provider.CreateLogger("CachedCategory");

            // Assert — same instance returned
            Assert.AreSame(logger1, logger2);
            // Inner provider should only be called once
            int categoryCount = innerProvider.CreatedCategories
                .Where(cat => cat == "CachedCategory")
                .Count();
            Assert.AreEqual(1, categoryCount, "Inner provider should only create logger once per category");
        }

        [TestMethod]
        public void CreateLogger_DifferentCategoriesGetDifferentLoggers()
        {
            // Arrange
            var innerProvider = new CapturingLoggerProvider();
            var provider = new TelemetryEnrichedLoggerProvider(innerProvider);

            // Act
            var logger1 = provider.CreateLogger("Category.A");
            var logger2 = provider.CreateLogger("Category.B");

            // Assert
            Assert.AreNotSame(logger1, logger2);
        }

        [TestMethod]
        public void CreateLogger_EnrichmentFlowsToCreatedLoggers()
        {
            // Arrange
            var innerProvider = new CapturingLoggerProvider();
            var provider = new TelemetryEnrichedLoggerProvider(innerProvider);
            var logger = provider.CreateLogger("TestCategory");
            CorrelationContext.SetRawValue("provider-test-id");

            // Act
            logger.LogInformation("Enriched via provider");

            // Assert
            var innerLogger = innerProvider.Loggers["TestCategory"];
            var scope = innerLogger.GetLastDictionaryScope();
            Assert.IsNotNull(scope);
            Assert.IsTrue(scope.ContainsKey("CorrelationId"));
            Assert.AreEqual("provider-test-id", scope["CorrelationId"]);
        }

        [TestMethod]
        public void Dispose_DisposesInnerProvider()
        {
            // Arrange
            var innerProvider = new CapturingLoggerProvider();
            var provider = new TelemetryEnrichedLoggerProvider(innerProvider);
            provider.CreateLogger("Test"); // populate cache

            // Act
            provider.Dispose();

            // Assert
            Assert.IsTrue(innerProvider.Disposed, "Inner provider should be disposed");
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var innerProvider = new CapturingLoggerProvider();
            var provider = new TelemetryEnrichedLoggerProvider(innerProvider);

            // Act & Assert — should not throw
            provider.Dispose();
            provider.Dispose();
        }
    }
}
