using System;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Proxies
{
    [TestClass]
    public class TelemetryInstrumentationExtensionsTests
    {
        // ─── AddTelemetryProxyFactory ───────────────────────────────────

        [TestMethod]
        public void AddTelemetryProxyFactory_RegistersSingleton()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(new FakeOperationScopeFactory());

            services.AddTelemetryProxyFactory();

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<ITelemetryProxyFactory>();

            Assert.IsNotNull(factory);
            Assert.IsInstanceOfType(factory, typeof(TelemetryProxyFactory));
        }

        [TestMethod]
        public void AddTelemetryProxyFactory_NullServices_Throws()
        {
            IServiceCollection? services = null;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => services!.AddTelemetryProxyFactory());
        }

        // ─── AddInstrumentedTransient ───────────────────────────────────

        [TestMethod]
        public void AddInstrumentedTransient_ResolvesProxy()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(new FakeOperationScopeFactory());
            services.AddTelemetryProxyFactory();
            services.AddInstrumentedTransient<ISimpleService, SimpleService>();

            var sp = services.BuildServiceProvider();
            var svc = sp.GetRequiredService<ISimpleService>();

            Assert.IsNotNull(svc);
            // Should be a proxy, not the direct implementation.
            Assert.IsNotInstanceOfType(svc, typeof(SimpleService));
        }

        [TestMethod]
        public void AddInstrumentedTransient_ProxyDelegatesToImplementation()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(new FakeOperationScopeFactory());
            services.AddTelemetryProxyFactory();
            services.AddInstrumentedTransient<ISimpleService, SimpleService>();

            var sp = services.BuildServiceProvider();
            var svc = sp.GetRequiredService<ISimpleService>();
            var result = svc.NotInstrumented(5);

            Assert.AreEqual("plain-5", result);
        }

        [TestMethod]
        public void AddInstrumentedTransient_EachResolveNewInstance()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(new FakeOperationScopeFactory());
            services.AddTelemetryProxyFactory();
            services.AddInstrumentedTransient<ISimpleService, SimpleService>();

            var sp = services.BuildServiceProvider();
            var svc1 = sp.GetRequiredService<ISimpleService>();
            var svc2 = sp.GetRequiredService<ISimpleService>();

            Assert.AreNotSame(svc1, svc2);
        }

        [TestMethod]
        public void AddInstrumentedTransient_NullServices_Throws()
        {
            IServiceCollection? services = null;
            Assert.ThrowsExactly<ArgumentNullException>(
                () => services!.AddInstrumentedTransient<ISimpleService, SimpleService>());
        }

        // ─── AddInstrumentedScoped ──────────────────────────────────────

        [TestMethod]
        public void AddInstrumentedScoped_ResolvesProxy()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(new FakeOperationScopeFactory());
            services.AddTelemetryProxyFactory();
            services.AddInstrumentedScoped<IOrderService, OrderService>();

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var svc = scope.ServiceProvider.GetRequiredService<IOrderService>();
                Assert.IsNotNull(svc);
                Assert.IsNotInstanceOfType(svc, typeof(OrderService));
            }
        }

        [TestMethod]
        public void AddInstrumentedScoped_SameWithinScope()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(new FakeOperationScopeFactory());
            services.AddTelemetryProxyFactory();
            services.AddInstrumentedScoped<IOrderService, OrderService>();

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var svc1 = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var svc2 = scope.ServiceProvider.GetRequiredService<IOrderService>();
                Assert.AreSame(svc1, svc2);
            }
        }

        [TestMethod]
        public void AddInstrumentedScoped_DifferentAcrossScopes()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(new FakeOperationScopeFactory());
            services.AddTelemetryProxyFactory();
            services.AddInstrumentedScoped<IOrderService, OrderService>();

            var sp = services.BuildServiceProvider();
            IOrderService svc1, svc2;
            using (var scope = sp.CreateScope())
            {
                svc1 = scope.ServiceProvider.GetRequiredService<IOrderService>();
            }
            using (var scope = sp.CreateScope())
            {
                svc2 = scope.ServiceProvider.GetRequiredService<IOrderService>();
            }
            Assert.AreNotSame(svc1, svc2);
        }

        [TestMethod]
        public void AddInstrumentedScoped_NullServices_Throws()
        {
            IServiceCollection? services = null;
            Assert.ThrowsExactly<ArgumentNullException>(
                () => services!.AddInstrumentedScoped<IOrderService, OrderService>());
        }

        // ─── AddInstrumentedSingleton ───────────────────────────────────

        [TestMethod]
        public void AddInstrumentedSingleton_ResolvesProxy()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(new FakeOperationScopeFactory());
            services.AddTelemetryProxyFactory();
            services.AddInstrumentedSingleton<ISimpleService, SimpleService>();

            var sp = services.BuildServiceProvider();
            var svc = sp.GetRequiredService<ISimpleService>();

            Assert.IsNotNull(svc);
        }

        [TestMethod]
        public void AddInstrumentedSingleton_SameInstance()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(new FakeOperationScopeFactory());
            services.AddTelemetryProxyFactory();
            services.AddInstrumentedSingleton<ISimpleService, SimpleService>();

            var sp = services.BuildServiceProvider();
            var svc1 = sp.GetRequiredService<ISimpleService>();
            var svc2 = sp.GetRequiredService<ISimpleService>();

            Assert.AreSame(svc1, svc2);
        }

        [TestMethod]
        public void AddInstrumentedSingleton_NullServices_Throws()
        {
            IServiceCollection? services = null;
            Assert.ThrowsExactly<ArgumentNullException>(
                () => services!.AddInstrumentedSingleton<ISimpleService, SimpleService>());
        }

        // ─── WithOptions ────────────────────────────────────────────────

        [TestMethod]
        public void AddInstrumentedTransient_WithOptions_UsesOptions()
        {
            var scopeFactory = new FakeOperationScopeFactory();
            var services = new ServiceCollection();
            services.AddSingleton<IOperationScopeFactory>(scopeFactory);
            services.AddTelemetryProxyFactory();
            services.AddInstrumentedTransient<IPiiAutoDetectService, PiiAutoDetectService>(
                new InstrumentationOptions { AutoDetectPii = false });

            var sp = services.BuildServiceProvider();
            var svc = sp.GetRequiredService<IPiiAutoDetectService>();

            svc.Login("admin", "secret123", "tok_abc");

            // AutoDetectPii=false → password captured as-is.
            var tags = scopeFactory.LastScope!.Tags;
            Assert.AreEqual("secret123", tags["param.password"]);
        }
    }
}
