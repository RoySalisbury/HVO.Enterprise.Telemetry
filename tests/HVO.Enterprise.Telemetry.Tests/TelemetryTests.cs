using System;
using HVO.Enterprise.Telemetry.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests
{
    [TestClass]
    public class TelemetryTests
    {
        [TestMethod]
        public void RecordException_ThrowsOnNull()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => Telemetry.RecordException(null!));
        }

        [TestMethod]
        public void GetExceptionAggregator_ReturnsSingleton()
        {
            var first = Telemetry.GetExceptionAggregator();
            var second = TelemetryExceptionExtensions.GetAggregator();

            Assert.IsNotNull(first);
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void RecordException_IncrementsAggregatorCount()
        {
            var aggregator = Telemetry.GetExceptionAggregator();
            var before = aggregator.TotalExceptions;

            Telemetry.RecordException(new InvalidOperationException("boom"));

            Assert.AreEqual(before + 1, aggregator.TotalExceptions);
        }
    }
}
