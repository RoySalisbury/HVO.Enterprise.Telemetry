using System;
using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Context.Providers;
using HVO.Enterprise.Telemetry.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    /// <summary>
    /// Tests for <see cref="ContextEnricher"/> and <see cref="EnvironmentContextProvider"/>.
    /// </summary>
    [TestClass]
    public class ContextEnricherComprehensiveTests
    {
        // --- ContextEnricher ---

        [TestMethod]
        public void ContextEnricher_DefaultConstructor_CreatesWithDefaults()
        {
            var enricher = new ContextEnricher();
            Assert.IsNotNull(enricher);
        }

        [TestMethod]
        public void ContextEnricher_WithOptions_UsesOptions()
        {
            var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Minimal };
            var enricher = new ContextEnricher(options);
            Assert.IsNotNull(enricher);
        }

        [TestMethod]
        public void ContextEnricher_WithLogger_UsesLogger()
        {
            var logger = new FakeLogger<ContextEnricher>();
            var enricher = new ContextEnricher(null, logger);
            Assert.IsNotNull(enricher);
        }

        [TestMethod]
        public void EnrichActivity_NullActivity_ThrowsArgumentNullException()
        {
            var enricher = new ContextEnricher();
            Assert.ThrowsExactly<ArgumentNullException>(() => enricher.EnrichActivity(null!));
        }

        [TestMethod]
        public void EnrichActivity_ValidActivity_AddsBasicTags()
        {
            using var activitySource = new ActivitySource("test-context-enricher");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-context-enricher",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("test-op");
            Assert.IsNotNull(activity);

            var enricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Minimal });
            enricher.EnrichActivity(activity);

            // Minimal level should add service.name, service.version, host.name
            Assert.IsNotNull(activity.GetTagItem("service.name"));
            Assert.IsNotNull(activity.GetTagItem("host.name"));
        }

        [TestMethod]
        public void EnrichActivity_StandardLevel_AddsOsAndRuntimeTags()
        {
            using var activitySource = new ActivitySource("test-context-enricher-std");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-context-enricher-std",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("test-op");
            Assert.IsNotNull(activity);

            var enricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Standard });
            enricher.EnrichActivity(activity);

            Assert.IsNotNull(activity.GetTagItem("os.type"));
            Assert.IsNotNull(activity.GetTagItem("runtime.version"));
        }

        [TestMethod]
        public void EnrichActivity_VerboseLevel_AddsPidAndThreadTags()
        {
            using var activitySource = new ActivitySource("test-context-enricher-verbose");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-context-enricher-verbose",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("test-op");
            Assert.IsNotNull(activity);

            var enricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose });
            enricher.EnrichActivity(activity);

            Assert.IsNotNull(activity.GetTagItem("process.pid"));
            Assert.IsNotNull(activity.GetTagItem("thread.id"));
        }

        [TestMethod]
        public void EnrichProperties_NullProperties_ThrowsArgumentNullException()
        {
            var enricher = new ContextEnricher();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => enricher.EnrichProperties(null!));
        }

        [TestMethod]
        public void EnrichProperties_ValidDictionary_AddsBasicProperties()
        {
            var enricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Minimal });
            var props = new Dictionary<string, object>();
            enricher.EnrichProperties(props);

            Assert.IsTrue(props.ContainsKey("service.name"));
            Assert.IsTrue(props.ContainsKey("host.name"));
        }

        [TestMethod]
        public void EnrichProperties_StandardLevel_AddsOsAndRuntime()
        {
            var enricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Standard });
            var props = new Dictionary<string, object>();
            enricher.EnrichProperties(props);

            Assert.IsTrue(props.ContainsKey("os.type"));
            Assert.IsTrue(props.ContainsKey("runtime.version"));
        }

        [TestMethod]
        public void EnrichProperties_VerboseLevel_AddsPidAndThread()
        {
            var enricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose });
            var props = new Dictionary<string, object>();
            enricher.EnrichProperties(props);

            Assert.IsTrue(props.ContainsKey("process.pid"));
            Assert.IsTrue(props.ContainsKey("thread.id"));
        }

        [TestMethod]
        public void RegisterProvider_NullProvider_ThrowsArgumentNullException()
        {
            var enricher = new ContextEnricher();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => enricher.RegisterProvider(null!));
        }

        [TestMethod]
        public void RegisterProvider_CustomProvider_IsUsedDuringEnrichment()
        {
            var enricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose });
            var customProvider = new TestContextProvider("custom", EnrichmentLevel.Minimal);
            enricher.RegisterProvider(customProvider);

            var props = new Dictionary<string, object>();
            enricher.EnrichProperties(props);

            Assert.IsTrue(customProvider.PropertiesEnriched, "Custom provider should have been called");
        }

        [TestMethod]
        public void EnrichActivity_ProviderThrows_LogsWarningAndContinues()
        {
            var logger = new FakeLogger<ContextEnricher>();
            var enricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose }, logger);
            enricher.RegisterProvider(new ThrowingContextProvider());

            using var activitySource = new ActivitySource("test-context-enricher-throw");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-context-enricher-throw",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("test-op");
            Assert.IsNotNull(activity);

            // Should not throw despite the bad provider
            enricher.EnrichActivity(activity);
            Assert.IsTrue(logger.HasLoggedAtLevel(Microsoft.Extensions.Logging.LogLevel.Warning));
        }

        [TestMethod]
        public void EnrichProperties_ProviderThrows_LogsWarningAndContinues()
        {
            var logger = new FakeLogger<ContextEnricher>();
            var enricher = new ContextEnricher(new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose }, logger);
            enricher.RegisterProvider(new ThrowingContextProvider());

            var props = new Dictionary<string, object>();
            enricher.EnrichProperties(props);

            Assert.IsTrue(logger.HasLoggedAtLevel(Microsoft.Extensions.Logging.LogLevel.Warning));
        }

        // --- EnvironmentContextProvider ---

        [TestMethod]
        public void EnvironmentContextProvider_Name_IsEnvironment()
        {
            var provider = new EnvironmentContextProvider();
            Assert.AreEqual("Environment", provider.Name);
        }

        [TestMethod]
        public void EnvironmentContextProvider_Level_IsMinimal()
        {
            var provider = new EnvironmentContextProvider();
            Assert.AreEqual(EnrichmentLevel.Minimal, provider.Level);
        }

        [TestMethod]
        public void EnvironmentContextProvider_EnrichActivity_NullActivity_Throws()
        {
            var provider = new EnvironmentContextProvider();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => provider.EnrichActivity(null!, new EnrichmentOptions()));
        }

        [TestMethod]
        public void EnvironmentContextProvider_EnrichActivity_NullOptions_Throws()
        {
            var provider = new EnvironmentContextProvider();
            using var activitySource = new ActivitySource("test-env-ctx");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-env-ctx",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("test");
            Assert.IsNotNull(activity);

            Assert.ThrowsExactly<ArgumentNullException>(
                () => provider.EnrichActivity(activity, null!));
        }

        [TestMethod]
        public void EnvironmentContextProvider_EnrichProperties_NullProperties_Throws()
        {
            var provider = new EnvironmentContextProvider();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => provider.EnrichProperties(null!, new EnrichmentOptions()));
        }

        [TestMethod]
        public void EnvironmentContextProvider_EnrichProperties_NullOptions_Throws()
        {
            var provider = new EnvironmentContextProvider();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => provider.EnrichProperties(new Dictionary<string, object>(), null!));
        }

        [TestMethod]
        public void EnvironmentContextProvider_EnrichProperties_Minimal_AddsBasicKeys()
        {
            var provider = new EnvironmentContextProvider();
            var props = new Dictionary<string, object>();
            provider.EnrichProperties(props, new EnrichmentOptions { MaxLevel = EnrichmentLevel.Minimal });

            Assert.IsTrue(props.ContainsKey("service.name"));
            Assert.IsTrue(props.ContainsKey("service.version"));
            Assert.IsTrue(props.ContainsKey("host.name"));
            Assert.IsFalse(props.ContainsKey("os.type"), "Minimal should not include os.type");
        }

        [TestMethod]
        public void EnvironmentContextProvider_EnrichProperties_Standard_AddsOsTags()
        {
            var provider = new EnvironmentContextProvider();
            var props = new Dictionary<string, object>();
            provider.EnrichProperties(props, new EnrichmentOptions { MaxLevel = EnrichmentLevel.Standard });

            Assert.IsTrue(props.ContainsKey("os.type"));
            Assert.IsTrue(props.ContainsKey("os.version"));
            Assert.IsTrue(props.ContainsKey("runtime.name"));
            Assert.IsTrue(props.ContainsKey("runtime.version"));
            Assert.IsTrue(props.ContainsKey("deployment.environment"));
        }

        [TestMethod]
        public void EnvironmentContextProvider_EnrichProperties_Verbose_AddsPidAndCpuCount()
        {
            var provider = new EnvironmentContextProvider();
            var props = new Dictionary<string, object>();
            provider.EnrichProperties(props, new EnrichmentOptions { MaxLevel = EnrichmentLevel.Verbose });

            Assert.IsTrue(props.ContainsKey("process.pid"));
            Assert.IsTrue(props.ContainsKey("thread.id"));
            Assert.IsTrue(props.ContainsKey("async.context_id"));
            Assert.IsTrue(props.ContainsKey("host.cpu_count"));
        }

        [TestMethod]
        public void EnvironmentContextProvider_CustomEnvironmentTags_AddedAtStandardLevel()
        {
            var provider = new EnvironmentContextProvider();
            var props = new Dictionary<string, object>();
            var options = new EnrichmentOptions
            {
                MaxLevel = EnrichmentLevel.Standard,
                CustomEnvironmentTags = new Dictionary<string, string>
                {
                    ["region"] = "us-east-1",
                    ["cluster"] = "prod"
                }
            };
            provider.EnrichProperties(props, options);

            Assert.IsTrue(props.ContainsKey("env.region"));
            Assert.AreEqual("us-east-1", props["env.region"]);
            Assert.IsTrue(props.ContainsKey("env.cluster"));
        }

        [TestMethod]
        public void EnvironmentContextProvider_CustomEnvironmentTags_SkipsEmptyKeys()
        {
            var provider = new EnvironmentContextProvider();
            var props = new Dictionary<string, object>();
            var options = new EnrichmentOptions
            {
                MaxLevel = EnrichmentLevel.Standard,
                CustomEnvironmentTags = new Dictionary<string, string>
                {
                    [""] = "should-skip",
                    ["valid"] = "included"
                }
            };
            provider.EnrichProperties(props, options);

            Assert.IsFalse(props.ContainsKey("env."));
            Assert.IsTrue(props.ContainsKey("env.valid"));
        }

        [TestMethod]
        public void EnvironmentContextProvider_CustomEnvironmentTags_SkipsEmptyValues()
        {
            var provider = new EnvironmentContextProvider();
            var props = new Dictionary<string, object>();
            var options = new EnrichmentOptions
            {
                MaxLevel = EnrichmentLevel.Standard,
                CustomEnvironmentTags = new Dictionary<string, string>
                {
                    ["empty-val"] = "",
                    ["valid"] = "ok"
                }
            };
            provider.EnrichProperties(props, options);

            Assert.IsFalse(props.ContainsKey("env.empty-val"));
            Assert.IsTrue(props.ContainsKey("env.valid"));
        }

        [TestMethod]
        public void EnvironmentContextProvider_EnrichActivity_Standard_AddsCustomEnvTags()
        {
            var provider = new EnvironmentContextProvider();
            using var activitySource = new ActivitySource("test-env-ctx-custom-tags");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-env-ctx-custom-tags",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("test");
            Assert.IsNotNull(activity);

            var options = new EnrichmentOptions
            {
                MaxLevel = EnrichmentLevel.Standard,
                CustomEnvironmentTags = new Dictionary<string, string>
                {
                    ["az"] = "us-west-2"
                }
            };
            provider.EnrichActivity(activity, options);

            Assert.AreEqual("us-west-2", activity.GetTagItem("env.az"));
        }

        // --- Test helpers ---

        private class TestContextProvider : IContextProvider
        {
            public string Name { get; }
            public EnrichmentLevel Level { get; }
            public bool ActivityEnriched { get; private set; }
            public bool PropertiesEnriched { get; private set; }

            public TestContextProvider(string name, EnrichmentLevel level)
            {
                Name = name;
                Level = level;
            }

            public void EnrichActivity(Activity activity, EnrichmentOptions options)
            {
                ActivityEnriched = true;
                activity.SetTag("custom.test", "yes");
            }

            public void EnrichProperties(IDictionary<string, object> properties, EnrichmentOptions options)
            {
                PropertiesEnriched = true;
                properties["custom.test"] = "yes";
            }
        }

        private class ThrowingContextProvider : IContextProvider
        {
            public string Name => "Throwing";
            public EnrichmentLevel Level => EnrichmentLevel.Minimal;

            public void EnrichActivity(Activity activity, EnrichmentOptions options)
            {
                throw new InvalidOperationException("Provider failure");
            }

            public void EnrichProperties(IDictionary<string, object> properties, EnrichmentOptions options)
            {
                throw new InvalidOperationException("Provider failure");
            }
        }
    }
}
