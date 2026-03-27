using System;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class TelemetryOptionsTests
    {
        [TestMethod]
        public void TelemetryOptions_Validate_AllowsDefaults()
        {
            var options = new TelemetryOptions();
            options.Validate();
        }

        [TestMethod]
        public void TelemetryOptions_Validate_RejectsInvalidDefaultSamplingRate()
        {
            var options = new TelemetryOptions
            {
                DefaultSamplingRate = 1.5
            };

            Assert.ThrowsExactly<InvalidOperationException>(() => options.Validate());
        }

        [TestMethod]
        public void TelemetryOptions_Validate_RejectsInvalidQueueCapacity()
        {
            var options = new TelemetryOptions
            {
                Queue = new QueueOptions { Capacity = 10 }
            };

            Assert.ThrowsExactly<InvalidOperationException>(() => options.Validate());
        }
    }
}
