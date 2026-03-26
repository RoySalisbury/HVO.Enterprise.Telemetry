using System;
using System.Linq;
using System.Threading;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Initialization
{
    [TestClass]
    public class TelemetryBuilderTests
    {
        private sealed class MockHostApplicationLifetime : IHostApplicationLifetime
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            public CancellationToken ApplicationStarted => _cts.Token;
            public CancellationToken ApplicationStopping => _cts.Token;
            public CancellationToken ApplicationStopped => _cts.Token;
            public void StopApplication() { }
        }

        [TestCleanup]
        public void Cleanup()
        {
            Telemetry.ClearInstance();
        }

        private static ServiceCollection CreateBaseServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHostApplicationLifetime>(new MockHostApplicationLifetime());
            return services;
        }

        [TestMethod]
        public void Builder_Configure_SetsOptions()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(builder => builder
                .Configure(options =>
                {
                    options.ServiceName = "BuilderTest";
                    options.ServiceVersion = "3.0.0";
                }));

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            Assert.AreEqual("BuilderTest", options.Value.ServiceName);
            Assert.AreEqual("3.0.0", options.Value.ServiceVersion);
        }

        [TestMethod]
        public void Builder_AddActivitySource_AddsToOptions()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(builder => builder
                .Configure(options => options.ServiceName = "Test")
                .AddActivitySource("CustomSource")
                .AddActivitySource("AnotherSource"));

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            CollectionAssert.Contains(options.Value.ActivitySources, "CustomSource");
            CollectionAssert.Contains(options.Value.ActivitySources, "AnotherSource");
        }

        [TestMethod]
        public void Builder_AddActivitySource_DoesNotAddDuplicates()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(builder => builder
                .Configure(options => options.ServiceName = "Test")
                .AddActivitySource("CustomSource")
                .AddActivitySource("CustomSource"));

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            var count = options.Value.ActivitySources.Where(s => s == "CustomSource").Count();

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void Builder_AddActivitySource_ThrowsOnEmpty()
        {
            var services = CreateBaseServices();
            Assert.ThrowsExactly<ArgumentException>(() =>
                services.AddTelemetry(builder => builder.AddActivitySource("")));
        }

        [TestMethod]
        public void Builder_AddActivitySource_ThrowsOnNull()
        {
            var services = CreateBaseServices();
            Assert.ThrowsExactly<ArgumentException>(() =>
                services.AddTelemetry(builder => builder.AddActivitySource(null!)));
        }

        [TestMethod]
        public void Builder_AddHttpInstrumentation_EnablesFeature()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(builder => builder
                .Configure(options => options.ServiceName = "Test")
                .AddHttpInstrumentation());

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            Assert.IsTrue(options.Value.Features.EnableHttpInstrumentation);
        }

        [TestMethod]
        public void Builder_Configure_ThrowsOnNull()
        {
            var services = CreateBaseServices();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                services.AddTelemetry(builder => builder.Configure(null!)));
        }

        [TestMethod]
        public void Builder_Fluent_AllMethodsChain()
        {
            var services = CreateBaseServices();
            services.AddTelemetry(builder =>
            {
                var result = builder
                    .Configure(o => o.ServiceName = "Fluent")
                    .AddActivitySource("Source1")
                    .AddHttpInstrumentation();

                Assert.IsNotNull(result);
            });
        }

        [TestMethod]
        public void AddTelemetry_BuilderOverload_ThrowsOnNullServices()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryServiceCollectionExtensions.AddTelemetry(
                    null!, (Action<TelemetryBuilder>)(_ => { })));
        }

        [TestMethod]
        public void AddTelemetry_BuilderOverload_ThrowsOnNullConfigure()
        {
            var services = CreateBaseServices();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => services.AddTelemetry((Action<TelemetryBuilder>)null!));
        }
    }
}
