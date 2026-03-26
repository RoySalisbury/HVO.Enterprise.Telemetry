using System;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Initialization
{
    [TestClass]
    public class TelemetryOptionsExtendedTests
    {
        [TestMethod]
        public void ServiceName_DefaultsToUnknown()
        {
            var options = new TelemetryOptions();
            Assert.AreEqual("Unknown", options.ServiceName);
        }

        [TestMethod]
        public void ServiceVersion_DefaultsToNull()
        {
            var options = new TelemetryOptions();
            Assert.IsNull(options.ServiceVersion);
        }

        [TestMethod]
        public void Environment_DefaultsToNull()
        {
            var options = new TelemetryOptions();
            Assert.IsNull(options.Environment);
        }

        [TestMethod]
        public void ActivitySources_DefaultsToOneEntry()
        {
            var options = new TelemetryOptions();
            Assert.IsNotNull(options.ActivitySources);
            Assert.AreEqual(1, options.ActivitySources.Count);
            Assert.AreEqual("HVO.Enterprise.Telemetry", options.ActivitySources[0]);
        }

        [TestMethod]
        public void ResourceAttributes_DefaultsToEmpty()
        {
            var options = new TelemetryOptions();
            Assert.IsNotNull(options.ResourceAttributes);
            Assert.AreEqual(0, options.ResourceAttributes.Count);
        }

        [TestMethod]
        public void Validate_ThrowsWhenServiceNameIsEmpty()
        {
            var options = new TelemetryOptions { ServiceName = "" };
            Assert.ThrowsExactly<InvalidOperationException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_ThrowsWhenServiceNameIsWhitespace()
        {
            var options = new TelemetryOptions { ServiceName = "   " };
            Assert.ThrowsExactly<InvalidOperationException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_SucceedsWithDefaultOptions()
        {
            var options = new TelemetryOptions();
            options.Validate(); // Should not throw
        }

        [TestMethod]
        public void Validate_SucceedsWithAllPropertiesSet()
        {
            var options = new TelemetryOptions
            {
                ServiceName = "MyService",
                ServiceVersion = "1.0.0",
                Environment = "Production",
                Enabled = true,
                DefaultSamplingRate = 0.5
            };
            options.Validate(); // Should not throw
        }

        [TestMethod]
        public void Validate_ThrowsWhenSamplingRateIsNegative()
        {
            var options = new TelemetryOptions { DefaultSamplingRate = -0.1 };
            Assert.ThrowsExactly<InvalidOperationException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_ThrowsWhenSamplingRateAboveOne()
        {
            var options = new TelemetryOptions { DefaultSamplingRate = 1.1 };
            Assert.ThrowsExactly<InvalidOperationException>(() => options.Validate());
        }

        [TestMethod]
        public void Validate_EnsuresDefaultsForNullCollections()
        {
            var options = new TelemetryOptions
            {
                ActivitySources = null!,
                ResourceAttributes = null!,
                Sampling = null!,
                Logging = null!,
                Metrics = null!,
                Queue = null!,
                Features = null!
            };

            // Validate should fix nulls via EnsureDefaults
            options.Validate();

            Assert.IsNotNull(options.ActivitySources);
            Assert.IsNotNull(options.ResourceAttributes);
            Assert.IsNotNull(options.Sampling);
            Assert.IsNotNull(options.Logging);
            Assert.IsNotNull(options.Metrics);
            Assert.IsNotNull(options.Queue);
            Assert.IsNotNull(options.Features);
        }
    }
}
