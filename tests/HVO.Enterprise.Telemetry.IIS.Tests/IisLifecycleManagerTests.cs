using System;
using HVO.Enterprise.Telemetry.IIS;
using HVO.Enterprise.Telemetry.IIS.Configuration;
using HVO.Enterprise.Telemetry.IIS.Tests.Fakes;

namespace HVO.Enterprise.Telemetry.IIS.Tests
{
    /// <summary>
    /// Tests for <see cref="IisLifecycleManager"/> lifecycle management.
    /// </summary>
    [TestClass]
    public sealed class IisLifecycleManagerTests
    {
        [TestMethod]
        public void Constructor_ThrowsInvalidOperationException_WhenNotOnIis()
        {
            // In our test environment, we're not running under IIS
            if (IisHostingEnvironment.IsIisHosted)
            {
                Assert.Inconclusive("This test requires a non-IIS environment.");
                return;
            }

            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(
                () => new IisLifecycleManager(null));
        }

        [TestMethod]
        public void Constructor_ThrowsInvalidOperationException_WithMessage()
        {
            if (IisHostingEnvironment.IsIisHosted)
            {
                Assert.Inconclusive("This test requires a non-IIS environment.");
                return;
            }

            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => new IisLifecycleManager(null));

            Assert.IsTrue(ex.Message.Contains("IisHostingEnvironment.IsIisHosted"));
        }

        [TestMethod]
        public void InternalConstructor_AllowsCreationWithoutIis()
        {
            // Arrange & Act - internal constructor skips IIS check
            var manager = new IisLifecycleManager(null, null, null, requireIis: false);

            // Assert
            Assert.IsNotNull(manager);
            Assert.IsFalse(manager.IsInitialized);
        }

        [TestMethod]
        public void InternalConstructor_ValidatesOptions()
        {
            // Arrange
            var options = new IisExtensionOptions { ShutdownTimeout = TimeSpan.FromSeconds(-1) };

            // Act & Assert
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new IisLifecycleManager(null, options, null, requireIis: false));
        }

        [TestMethod]
        public void Initialize_SetsIsInitialized()
        {
            // Arrange
            var manager = new IisLifecycleManager(null, null, null, requireIis: false);

            // Act
            manager.Initialize();

            // Assert
            Assert.IsTrue(manager.IsInitialized);

            // Cleanup
            manager.Dispose();
        }

        [TestMethod]
        public void Initialize_ThrowsInvalidOperationException_WhenCalledTwice()
        {
            // Arrange
            var manager = new IisLifecycleManager(null, null, null, requireIis: false);
            manager.Initialize();

            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() => manager.Initialize());

            // Cleanup
            manager.Dispose();
        }

        [TestMethod]
        public void Initialize_WithTelemetryService_DoesNotStartService()
        {
            // Arrange - lifecycle manager doesn't start telemetry; that's done separately
            var fakeTelemetry = new FakeTelemetryService();
            var manager = new IisLifecycleManager(fakeTelemetry, null, null, requireIis: false);

            // Act
            manager.Initialize();

            // Assert - telemetry Start() should NOT be called by the IIS extension
            Assert.IsFalse(fakeTelemetry.StartCalled);

            // Cleanup
            manager.Dispose();
        }

        [TestMethod]
        public void Dispose_IsIdempotent()
        {
            // Arrange
            var manager = new IisLifecycleManager(null, null, null, requireIis: false);
            manager.Initialize();

            // Act - multiple dispose calls should not throw
            manager.Dispose();
            manager.Dispose();
            manager.Dispose();
        }

        [TestMethod]
        public void Dispose_WorksWithoutInitialize()
        {
            // Arrange
            var manager = new IisLifecycleManager(null, null, null, requireIis: false);

            // Act & Assert - dispose without initialize should not throw
            manager.Dispose();
        }

        [TestMethod]
        public void ShutdownHandler_IsAccessible()
        {
            // Arrange
            var manager = new IisLifecycleManager(null, null, null, requireIis: false);

            // Assert
            Assert.IsNotNull(manager.ShutdownHandler);

            // Cleanup
            manager.Dispose();
        }

        [TestMethod]
        public void Constructor_AcceptsCustomOptions()
        {
            // Arrange
            var options = new IisExtensionOptions
            {
                ShutdownTimeout = TimeSpan.FromSeconds(15),
                AutoInitialize = false,
                RegisterWithHostingEnvironment = false
            };

            // Act
            var manager = new IisLifecycleManager(null, options, null, requireIis: false);

            // Assert
            Assert.IsNotNull(manager);

            // Cleanup
            manager.Dispose();
        }
    }
}
