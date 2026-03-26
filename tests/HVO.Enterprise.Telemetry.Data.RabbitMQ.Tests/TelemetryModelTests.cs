using System;
using HVO.Enterprise.Telemetry.Data.RabbitMQ;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Configuration;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Instrumentation;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Tests
{
    [TestClass]
    public class TelemetryModelTests
    {
        [TestMethod]
        public void Constructor_NullModel_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new TelemetryModel(null!));
        }

        [TestMethod]
        public void RabbitMqActivitySource_HasExpectedName()
        {
            Assert.AreEqual("HVO.Enterprise.Telemetry.Data.RabbitMQ", RabbitMqActivitySource.Name);
        }

        [TestMethod]
        public void RabbitMqActivitySource_SourceNotNull()
        {
            Assert.IsNotNull(RabbitMqActivitySource.Source);
        }
    }
}
