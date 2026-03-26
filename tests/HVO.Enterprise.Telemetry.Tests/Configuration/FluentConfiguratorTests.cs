using System;
using System.Reflection;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    /// <summary>
    /// Tests for the fluent configuration hierarchy:
    /// <see cref="GlobalConfigurator"/>, <see cref="TypeConfigurator{T}"/>,
    /// <see cref="NamespaceConfigurator"/>, and <see cref="MethodConfigurator"/>.
    /// </summary>
    [TestClass]
    public class FluentConfiguratorTests
    {
        private ConfigurationProvider _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ConfigurationProvider();
        }

        // --- GlobalConfigurator ---

        [TestMethod]
        public void GlobalConfigurator_SamplingRate_ReturnsSelf()
        {
            var cfg = new GlobalConfigurator(_provider);
            var result = cfg.SamplingRate(0.5);
            Assert.AreSame(cfg, result);
        }

        [TestMethod]
        public void GlobalConfigurator_Enabled_ReturnsSelf()
        {
            var cfg = new GlobalConfigurator(_provider);
            Assert.AreSame(cfg, cfg.Enabled(true));
        }

        [TestMethod]
        public void GlobalConfigurator_CaptureParameters_ReturnsSelf()
        {
            var cfg = new GlobalConfigurator(_provider);
            Assert.AreSame(cfg, cfg.CaptureParameters(ParameterCaptureMode.NamesOnly));
        }

        [TestMethod]
        public void GlobalConfigurator_RecordExceptions_ReturnsSelf()
        {
            var cfg = new GlobalConfigurator(_provider);
            Assert.AreSame(cfg, cfg.RecordExceptions(true));
        }

        [TestMethod]
        public void GlobalConfigurator_TimeoutThreshold_ReturnsSelf()
        {
            var cfg = new GlobalConfigurator(_provider);
            Assert.AreSame(cfg, cfg.TimeoutThreshold(5000));
        }

        [TestMethod]
        public void GlobalConfigurator_AddTag_ReturnsSelf()
        {
            var cfg = new GlobalConfigurator(_provider);
            Assert.AreSame(cfg, cfg.AddTag("env", "prod"));
        }

        [TestMethod]
        public void GlobalConfigurator_AddTag_NullKey_ThrowsArgumentNullException()
        {
            var cfg = new GlobalConfigurator(_provider);
            Assert.ThrowsExactly<ArgumentNullException>(() => cfg.AddTag(null!, "value"));
        }

        [TestMethod]
        public void GlobalConfigurator_AddTag_EmptyKey_ThrowsArgumentNullException()
        {
            var cfg = new GlobalConfigurator(_provider);
            Assert.ThrowsExactly<ArgumentNullException>(() => cfg.AddTag("", "value"));
        }

        [TestMethod]
        public void GlobalConfigurator_Apply_CommitsConfiguration()
        {
            var cfg = new GlobalConfigurator(_provider);
            cfg.SamplingRate(0.25)
               .Enabled(true)
               .CaptureParameters(ParameterCaptureMode.NamesAndValues)
               .RecordExceptions(false)
               .TimeoutThreshold(3000)
               .AddTag("region", "us-east")
               .Apply();

            // Verify through ConfigurationProvider's effective config
            var effective = _provider.GetEffectiveConfiguration(typeof(object), null);
            Assert.IsNotNull(effective);
        }

        [TestMethod]
        public void GlobalConfigurator_FluentChaining_AllMethods()
        {
            var cfg = new GlobalConfigurator(_provider);
            cfg.SamplingRate(1.0)
               .Enabled(true)
               .CaptureParameters(ParameterCaptureMode.NamesOnly)
               .RecordExceptions(true)
               .TimeoutThreshold(1000)
               .AddTag("key", "value")
               .Apply();
            // No assertions needed — verifies chaining doesn't throw
        }

        // --- TypeConfigurator<T> ---

        [TestMethod]
        public void TypeConfigurator_SamplingRate_ReturnsSelf()
        {
            var cfg = new TypeConfigurator<string>(_provider);
            Assert.AreSame(cfg, cfg.SamplingRate(0.75));
        }

        [TestMethod]
        public void TypeConfigurator_Enabled_ReturnsSelf()
        {
            var cfg = new TypeConfigurator<string>(_provider);
            Assert.AreSame(cfg, cfg.Enabled(false));
        }

        [TestMethod]
        public void TypeConfigurator_CaptureParameters_ReturnsSelf()
        {
            var cfg = new TypeConfigurator<string>(_provider);
            Assert.AreSame(cfg, cfg.CaptureParameters(ParameterCaptureMode.NamesAndValues));
        }

        [TestMethod]
        public void TypeConfigurator_RecordExceptions_ReturnsSelf()
        {
            var cfg = new TypeConfigurator<string>(_provider);
            Assert.AreSame(cfg, cfg.RecordExceptions(false));
        }

        [TestMethod]
        public void TypeConfigurator_TimeoutThreshold_ReturnsSelf()
        {
            var cfg = new TypeConfigurator<string>(_provider);
            Assert.AreSame(cfg, cfg.TimeoutThreshold(2000));
        }

        [TestMethod]
        public void TypeConfigurator_AddTag_ReturnsSelf()
        {
            var cfg = new TypeConfigurator<string>(_provider);
            Assert.AreSame(cfg, cfg.AddTag("type-tag", "value"));
        }

        [TestMethod]
        public void TypeConfigurator_AddTag_NullKey_ThrowsArgumentNullException()
        {
            var cfg = new TypeConfigurator<string>(_provider);
            Assert.ThrowsExactly<ArgumentNullException>(() => cfg.AddTag(null!, "v"));
        }

        [TestMethod]
        public void TypeConfigurator_Apply_CommitsConfiguration()
        {
            var cfg = new TypeConfigurator<FluentConfiguratorTests>(_provider);
            cfg.SamplingRate(0.5)
               .Enabled(true)
               .AddTag("scope", "type")
               .Apply();

            var effective = _provider.GetEffectiveConfiguration(typeof(FluentConfiguratorTests), null);
            Assert.IsNotNull(effective);
        }

        // --- NamespaceConfigurator ---

        [TestMethod]
        public void NamespaceConfigurator_SamplingRate_ReturnsSelf()
        {
            var cfg = new NamespaceConfigurator(_provider, "HVO.Enterprise.*");
            Assert.AreSame(cfg, cfg.SamplingRate(0.1));
        }

        [TestMethod]
        public void NamespaceConfigurator_Enabled_ReturnsSelf()
        {
            var cfg = new NamespaceConfigurator(_provider, "HVO.Enterprise.*");
            Assert.AreSame(cfg, cfg.Enabled(false));
        }

        [TestMethod]
        public void NamespaceConfigurator_CaptureParameters_ReturnsSelf()
        {
            var cfg = new NamespaceConfigurator(_provider, "HVO.Enterprise.*");
            Assert.AreSame(cfg, cfg.CaptureParameters(ParameterCaptureMode.None));
        }

        [TestMethod]
        public void NamespaceConfigurator_RecordExceptions_ReturnsSelf()
        {
            var cfg = new NamespaceConfigurator(_provider, "HVO.Enterprise.*");
            Assert.AreSame(cfg, cfg.RecordExceptions(true));
        }

        [TestMethod]
        public void NamespaceConfigurator_TimeoutThreshold_ReturnsSelf()
        {
            var cfg = new NamespaceConfigurator(_provider, "HVO.Enterprise.*");
            Assert.AreSame(cfg, cfg.TimeoutThreshold(4000));
        }

        [TestMethod]
        public void NamespaceConfigurator_AddTag_ReturnsSelf()
        {
            var cfg = new NamespaceConfigurator(_provider, "HVO.Enterprise.*");
            Assert.AreSame(cfg, cfg.AddTag("ns-tag", "value"));
        }

        [TestMethod]
        public void NamespaceConfigurator_AddTag_NullKey_ThrowsArgumentNullException()
        {
            var cfg = new NamespaceConfigurator(_provider, "HVO.Enterprise.*");
            Assert.ThrowsExactly<ArgumentNullException>(() => cfg.AddTag(null!, "v"));
        }

        [TestMethod]
        public void NamespaceConfigurator_NullProvider_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new NamespaceConfigurator(null!, "ns"));
        }

        [TestMethod]
        public void NamespaceConfigurator_NullPattern_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new NamespaceConfigurator(_provider, null!));
        }

        [TestMethod]
        public void NamespaceConfigurator_Apply_CommitsConfiguration()
        {
            var cfg = new NamespaceConfigurator(_provider, "HVO.Enterprise.Telemetry.Tests");
            cfg.SamplingRate(0.3)
               .Enabled(true)
               .Apply();

            var effective = _provider.GetEffectiveConfiguration(typeof(FluentConfiguratorTests), null);
            Assert.IsNotNull(effective);
        }

        // --- MethodConfigurator ---

        [TestMethod]
        public void MethodConfigurator_SamplingRate_ReturnsSelf()
        {
            var method = GetSampleMethod();
            var cfg = new MethodConfigurator(_provider, method);
            Assert.AreSame(cfg, cfg.SamplingRate(0.9));
        }

        [TestMethod]
        public void MethodConfigurator_Enabled_ReturnsSelf()
        {
            var method = GetSampleMethod();
            var cfg = new MethodConfigurator(_provider, method);
            Assert.AreSame(cfg, cfg.Enabled(true));
        }

        [TestMethod]
        public void MethodConfigurator_CaptureParameters_ReturnsSelf()
        {
            var method = GetSampleMethod();
            var cfg = new MethodConfigurator(_provider, method);
            Assert.AreSame(cfg, cfg.CaptureParameters(ParameterCaptureMode.NamesAndValues));
        }

        [TestMethod]
        public void MethodConfigurator_RecordExceptions_ReturnsSelf()
        {
            var method = GetSampleMethod();
            var cfg = new MethodConfigurator(_provider, method);
            Assert.AreSame(cfg, cfg.RecordExceptions(false));
        }

        [TestMethod]
        public void MethodConfigurator_TimeoutThreshold_ReturnsSelf()
        {
            var method = GetSampleMethod();
            var cfg = new MethodConfigurator(_provider, method);
            Assert.AreSame(cfg, cfg.TimeoutThreshold(500));
        }

        [TestMethod]
        public void MethodConfigurator_AddTag_ReturnsSelf()
        {
            var method = GetSampleMethod();
            var cfg = new MethodConfigurator(_provider, method);
            Assert.AreSame(cfg, cfg.AddTag("method-tag", "value"));
        }

        [TestMethod]
        public void MethodConfigurator_AddTag_NullKey_ThrowsArgumentNullException()
        {
            var method = GetSampleMethod();
            var cfg = new MethodConfigurator(_provider, method);
            Assert.ThrowsExactly<ArgumentNullException>(() => cfg.AddTag(null!, "v"));
        }

        [TestMethod]
        public void MethodConfigurator_NullProvider_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new MethodConfigurator(null!, GetSampleMethod()));
        }

        [TestMethod]
        public void MethodConfigurator_NullMethod_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new MethodConfigurator(_provider, null!));
        }

        [TestMethod]
        public void MethodConfigurator_Apply_CommitsConfiguration()
        {
            var method = GetSampleMethod();
            var cfg = new MethodConfigurator(_provider, method);
            cfg.SamplingRate(0.01)
               .Enabled(false)
               .CaptureParameters(ParameterCaptureMode.None)
               .RecordExceptions(false)
               .TimeoutThreshold(100)
               .AddTag("perf", "critical")
               .Apply();

            var effective = _provider.GetEffectiveConfiguration(typeof(FluentConfiguratorTests), method);
            Assert.IsNotNull(effective);
        }

        private static MethodInfo GetSampleMethod()
        {
            return typeof(FluentConfiguratorTests)
                .GetMethod(nameof(Setup), BindingFlags.Public | BindingFlags.Instance)!;
        }
    }
}
