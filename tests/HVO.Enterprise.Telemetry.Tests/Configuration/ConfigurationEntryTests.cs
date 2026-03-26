using System;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class ConfigurationEntryTests
    {
        [TestMethod]
        public void Constructor_SetsProperties()
        {
            var configuration = new OperationConfiguration { SamplingRate = 0.5 };

            var entry = new ConfigurationEntry(
                ConfigurationLevel.Type,
                ConfigurationSourceKind.Code,
                "HVO.Test.Type",
                configuration);

            Assert.AreEqual(ConfigurationLevel.Type, entry.Level);
            Assert.AreEqual(ConfigurationSourceKind.Code, entry.Source);
            Assert.AreEqual("HVO.Test.Type", entry.Identifier);
            Assert.AreSame(configuration, entry.Configuration);
        }

        [TestMethod]
        public void Constructor_NullConfiguration_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ConfigurationEntry(ConfigurationLevel.Global, ConfigurationSourceKind.Code, "global", null!));
        }
    }
}
