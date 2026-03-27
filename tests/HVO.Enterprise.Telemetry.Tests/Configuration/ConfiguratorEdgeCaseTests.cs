using System;
using System.Reflection;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class ConfiguratorEdgeCaseTests
    {
        [TestMethod]
        public void TelemetryConfigurator_ForMethod_Null_Throws()
        {
            var configurator = new TelemetryConfigurator(new ConfigurationProvider());

            Assert.ThrowsExactly<ArgumentNullException>(() => configurator.ForMethod(null!));
        }

        [TestMethod]
        public void GlobalConfigurator_AddTag_WithEmptyKey_Throws()
        {
            var provider = new ConfigurationProvider();
            var global = new GlobalConfigurator(provider);

            Assert.ThrowsExactly<ArgumentNullException>(() => global.AddTag(" ", "value"));
        }

        [TestMethod]
        public void NamespaceConfigurator_AddTag_WithEmptyKey_Throws()
        {
            var provider = new ConfigurationProvider();
            var ns = new NamespaceConfigurator(provider, "HVO.Enterprise.Telemetry.Tests");

            Assert.ThrowsExactly<ArgumentNullException>(() => ns.AddTag("", "value"));
        }

        [TestMethod]
        public void MethodConfigurator_AddTag_WithEmptyKey_Throws()
        {
            var provider = new ConfigurationProvider();
            var method = typeof(ConfiguratorEdgeCaseTests).GetMethod(nameof(SampleMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var methodConfigurator = new MethodConfigurator(provider, method!);

            Assert.ThrowsExactly<ArgumentNullException>(() => methodConfigurator.AddTag(" ", "value"));
        }

        [TestMethod]
        public void MethodConfigurator_Apply_SetsConfiguration()
        {
            var provider = new ConfigurationProvider();
            var method = typeof(ConfiguratorEdgeCaseTests).GetMethod(nameof(SampleMethod), BindingFlags.Static | BindingFlags.NonPublic);
            var methodConfigurator = new MethodConfigurator(provider, method!);

            methodConfigurator.SamplingRate(0.4).Enabled(false).Apply();

            var effective = provider.GetEffectiveConfiguration(null, method);
            Assert.AreEqual(0.4, effective.SamplingRate);
            Assert.AreEqual(false, effective.Enabled);
        }

        private static void SampleMethod()
        {
        }
    }
}
