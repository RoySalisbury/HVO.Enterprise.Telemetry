using System;
using HVO.Enterprise.Telemetry.Data.Redis;
using HVO.Enterprise.Telemetry.Data.Redis.Configuration;
using HVO.Enterprise.Telemetry.Data.Redis.Profiling;
using StackExchange.Redis.Profiling;

namespace HVO.Enterprise.Telemetry.Data.Redis.Tests
{
    [TestClass]
    public class RedisTelemetryProfilerTests
    {
        [TestMethod]
        public void Constructor_Default_DoesNotThrow()
        {
            // Act
            var profiler = new RedisTelemetryProfiler();

            // Assert
            Assert.IsNotNull(profiler);
        }

        [TestMethod]
        public void Constructor_WithOptions_DoesNotThrow()
        {
            // Arrange
            var options = new RedisTelemetryOptions { RecordCommands = false };

            // Act
            var profiler = new RedisTelemetryProfiler(options);

            // Assert
            Assert.IsNotNull(profiler);
        }

        [TestMethod]
        public void Constructor_NullSessionFactory_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new RedisTelemetryProfiler((Func<ProfilingSession>)null!));
        }

        [TestMethod]
        public void GetSession_ReturnsProfilingSession()
        {
            // Arrange
            var profiler = new RedisTelemetryProfiler();

            // Act
            var session = profiler.GetSession();

            // Assert
            Assert.IsNotNull(session);
        }

        [TestMethod]
        public void CommandProcessor_IsNotNull()
        {
            // Arrange
            var profiler = new RedisTelemetryProfiler();

            // Assert
            Assert.IsNotNull(profiler.CommandProcessor);
        }

        [TestMethod]
        public void Constructor_CustomSessionFactory_UsesProvidedFactory()
        {
            // Arrange
            var customSession = new ProfilingSession();
            var profiler = new RedisTelemetryProfiler(() => customSession);

            // Act
            var session = profiler.GetSession();

            // Assert
            Assert.AreSame(customSession, session);
        }
    }
}
