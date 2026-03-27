using System;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.Sampling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Sampling
{
    [TestClass]
    public class SamplingActivitySourceExtensionsTests
    {
        [TestMethod]
        public void ConfigureSampling_ThrowsOnNullSampler()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                SamplingActivitySourceExtensions.ConfigureSampling(null!));
        }

        [TestMethod]
        public void SamplingActivitySourceExtensions_BuildSampler_UsesGlobalOverride()
        {
            var provider = new ConfigurationProvider();
            provider.SetGlobalConfiguration(new OperationConfiguration { SamplingRate = 0.0 });

            var options = new TelemetryOptions
            {
                DefaultSamplingRate = 1.0
            };

            var sampler = SamplingActivitySourceExtensions.BuildSampler(options, provider);

            // Use a fixed TraceId to ensure deterministic behavior (32 hex chars)
            var traceId = System.Diagnostics.ActivityTraceId.CreateFromString("00000000000000000000000000000001");
            var context = new SamplingContext(traceId, "op", "source", System.Diagnostics.ActivityKind.Internal);

            var result = sampler.ShouldSample(context);

            // With 0.0 global override, should always drop (regardless of DefaultSamplingRate = 1.0)
            Assert.AreEqual(SamplingDecision.Drop, result.Decision);
        }

        [TestMethod]
        public void SamplingActivitySourceExtensions_BuildSampler_RespectsPerSourceRate()
        {
            var options = new TelemetryOptions
            {
                DefaultSamplingRate = 1.0
            };
            options.Sampling["source"] = new SamplingOptions { Rate = 0.0, AlwaysSampleErrors = false };

            var sampler = SamplingActivitySourceExtensions.BuildSampler(options, new ConfigurationProvider());
            var context = new SamplingContext(System.Diagnostics.ActivityTraceId.CreateRandom(), "op", "source", System.Diagnostics.ActivityKind.Internal);

            var result = sampler.ShouldSample(context);

            Assert.AreEqual(SamplingDecision.Drop, result.Decision);
        }

        [TestMethod]
        public void CreateWithSampling_CachesByNameAndVersion()
        {
            var name = "test.source." + Guid.NewGuid().ToString("N");

            SamplingActivitySourceExtensions.ClearCache();

            var first = SamplingActivitySourceExtensions.CreateWithSampling(name, "1.0.0");
            var second = SamplingActivitySourceExtensions.CreateWithSampling(name, "1.0.0");

            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void CreateWithSampling_DifferentVersion_ReturnsDifferentInstance()
        {
            var name = "test.source." + Guid.NewGuid().ToString("N");

            SamplingActivitySourceExtensions.ClearCache();

            var first = SamplingActivitySourceExtensions.CreateWithSampling(name, "1.0.0");
            var second = SamplingActivitySourceExtensions.CreateWithSampling(name, "2.0.0");

            Assert.AreNotSame(first, second);
        }

        [TestMethod]
        public void ClearCache_AllowsNewInstance()
        {
            var name = "test.source." + Guid.NewGuid().ToString("N");

            SamplingActivitySourceExtensions.ClearCache();
            var first = SamplingActivitySourceExtensions.CreateWithSampling(name, "1.0.0");

            SamplingActivitySourceExtensions.ClearCache();
            var second = SamplingActivitySourceExtensions.CreateWithSampling(name, "1.0.0");

            Assert.AreNotSame(first, second);
        }

        [TestMethod]
        public void ConfigureFromOptionsMonitor_ReturnsSubscription()
        {
            var monitor = new FakeOptionsMonitor(new TelemetryOptions { DefaultSamplingRate = 1.0 });

            var subscription = SamplingActivitySourceExtensions.ConfigureFromOptionsMonitor(monitor);

            Assert.IsNotNull(subscription);
            Assert.IsTrue(monitor.HasCallback);
            subscription.Dispose();
        }

        [TestMethod]
        public void ConfigureFromOptionsMonitor_ReturnsNullDisposableWhenMonitorReturnsNull()
        {
            var monitor = new FakeOptionsMonitor(new TelemetryOptions { DefaultSamplingRate = 1.0 }, returnNullSubscription: true);

            var subscription = SamplingActivitySourceExtensions.ConfigureFromOptionsMonitor(monitor);

            Assert.IsNotNull(subscription);
            Assert.IsFalse(monitor.HasCallback);
            subscription.Dispose();
        }

        [TestMethod]
        public void ConfigureFromFileReloader_ReturnsDisposable()
        {
            var path = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(path, "{}");

            using var reloader = new FileConfigurationReloader(path);
            var subscription = SamplingActivitySourceExtensions.ConfigureFromFileReloader(reloader);

            Assert.IsNotNull(subscription);
            subscription.Dispose();

            System.IO.File.Delete(path);
        }

        private sealed class FakeOptionsMonitor : Microsoft.Extensions.Options.IOptionsMonitor<TelemetryOptions>
        {
            private Action<TelemetryOptions, string?>? _listener;
            private readonly bool _returnNullSubscription;

            public FakeOptionsMonitor(TelemetryOptions currentValue, bool returnNullSubscription = false)
            {
                CurrentValue = currentValue;
                _returnNullSubscription = returnNullSubscription;
            }

            public TelemetryOptions CurrentValue { get; private set; }

            public bool HasCallback => _listener != null;

            public TelemetryOptions Get(string? name) => CurrentValue;

            public IDisposable OnChange(Action<TelemetryOptions, string?> listener)
            {
                if (_returnNullSubscription)
                    return null!;

                _listener = listener;
                return new CallbackDisposable(() => _listener = null);
            }

            private sealed class CallbackDisposable : IDisposable
            {
                private readonly Action _onDispose;

                public CallbackDisposable(Action onDispose)
                {
                    _onDispose = onDispose;
                }

                public void Dispose()
                {
                    _onDispose();
                }
            }
        }
    }
}
