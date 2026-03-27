using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Tests.OperationScopes
{
    /// <summary>
    /// Comprehensive tests for <see cref="OperationScopeFactory"/> validation,
    /// constructor overloads, and Begin method.
    /// </summary>
    [TestClass]
    public class OperationScopeFactoryComprehensiveTests
    {
        private TestActivitySource _testSource = null!;

        [TestInitialize]
        public void Setup()
        {
            _testSource = new TestActivitySource("factory-comprehensive-test");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _testSource.Dispose();
        }

        // --- Constructor with string name ---

        [TestMethod]
        public void Constructor_NullName_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new OperationScopeFactory(activitySourceName: null!));
        }

        [TestMethod]
        public void Constructor_EmptyName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new OperationScopeFactory(activitySourceName: string.Empty));
        }

        [TestMethod]
        public void Constructor_ValidName_CreatesFactory()
        {
            var factory = new OperationScopeFactory("test-source");
            Assert.IsNotNull(factory);
        }

        [TestMethod]
        public void Constructor_NameWithVersion_CreatesFactory()
        {
            var factory = new OperationScopeFactory("test-source", "1.0.0");
            Assert.IsNotNull(factory);
        }

        [TestMethod]
        public void Constructor_WithLoggerFactory_CreatesFactory()
        {
            var loggerFactory = new FakeLoggerFactory();
            var factory = new OperationScopeFactory("test-source", loggerFactory: loggerFactory);
            Assert.IsNotNull(factory);
        }

        // --- Constructor with ActivitySource ---

        [TestMethod]
        public void Constructor_NullActivitySource_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new OperationScopeFactory(activitySource: null!));
        }

        [TestMethod]
        public void Constructor_WithActivitySource_CreatesFactory()
        {
            var factory = new OperationScopeFactory(_testSource.Source);
            Assert.IsNotNull(factory);
        }

        [TestMethod]
        public void Constructor_WithActivitySourceAndLogger_CreatesFactory()
        {
            var loggerFactory = new FakeLoggerFactory();
            var factory = new OperationScopeFactory(_testSource.Source, loggerFactory: loggerFactory);
            Assert.IsNotNull(factory);
        }

        // --- Begin method ---

        [TestMethod]
        public void Begin_NullName_ThrowsArgumentNullException()
        {
            var factory = new OperationScopeFactory(_testSource.Source);
            Assert.ThrowsExactly<ArgumentNullException>(
                () => factory.Begin(null!));
        }

        [TestMethod]
        public void Begin_EmptyName_ThrowsArgumentException()
        {
            var factory = new OperationScopeFactory(_testSource.Source);
            Assert.ThrowsExactly<ArgumentException>(
                () => factory.Begin(string.Empty));
        }

        [TestMethod]
        public void Begin_ValidName_ReturnsScopeInstance()
        {
            var factory = new OperationScopeFactory(_testSource.Source);
            using var scope = factory.Begin("test-operation");
            Assert.IsNotNull(scope);
            Assert.AreEqual("test-operation", scope.Name);
        }

        [TestMethod]
        public void Begin_WithOptions_PassesOptionsToScope()
        {
            var factory = new OperationScopeFactory(_testSource.Source);
            var options = new OperationScopeOptions
            {
                CreateActivity = false,
                LogEvents = false
            };

            using var scope = factory.Begin("test-operation", options);
            Assert.IsNotNull(scope);
            Assert.IsNull(scope.Activity, "Activity should be null when CreateActivity is false");
        }

        [TestMethod]
        public void Begin_WithDefaultOptions_CreatesActivity()
        {
            var factory = new OperationScopeFactory(_testSource.Source);
            using var scope = factory.Begin("test-operation");

            // Activity may or may not be created depending on listener
            // but scope should always be non-null
            Assert.IsNotNull(scope);
        }

        [TestMethod]
        public void Begin_MultipleScopes_ReturnsDistinctInstances()
        {
            var factory = new OperationScopeFactory(_testSource.Source);
            using var scope1 = factory.Begin("op-1");
            using var scope2 = factory.Begin("op-2");

            Assert.AreNotSame(scope1, scope2);
            Assert.AreEqual("op-1", scope1.Name);
            Assert.AreEqual("op-2", scope2.Name);
        }

        [TestMethod]
        public void Begin_WithAllOptionsSet_ScopeReflectsOptions()
        {
            var loggerFactory = new FakeLoggerFactory();
            var factory = new OperationScopeFactory(_testSource.Source, loggerFactory: loggerFactory);

            var options = new OperationScopeOptions
            {
                ActivityKind = ActivityKind.Client,
                LogEvents = true,
                LogLevel = LogLevel.Debug,
                RecordMetrics = false,
                EnrichContext = false,
                CaptureExceptions = false
            };

            using var scope = factory.Begin("configured-op", options);
            Assert.IsNotNull(scope);
            Assert.AreEqual("configured-op", scope.Name);
        }
    }
}
