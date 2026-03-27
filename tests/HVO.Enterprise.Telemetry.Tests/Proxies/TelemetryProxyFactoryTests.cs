using System;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Proxies
{
    [TestClass]
    public class TelemetryProxyFactoryTests
    {
        private FakeOperationScopeFactory _scopeFactory = null!;
        private TelemetryProxyFactory _factory = null!;

        [TestInitialize]
        public void Setup()
        {
            _scopeFactory = new FakeOperationScopeFactory();
            _factory = new TelemetryProxyFactory(_scopeFactory);
        }

        // ─── CREATE PROXY ───────────────────────────────────────────────

        [TestMethod]
        public void CreateProxy_ValidInterface_ReturnsProxy()
        {
            var service = new SimpleService();
            var proxy = _factory.CreateProxy<ISimpleService>(service);

            Assert.IsNotNull(proxy);
            Assert.IsInstanceOfType(proxy, typeof(ISimpleService));
        }

        [TestMethod]
        public void CreateProxy_ProxyImplementsInterface()
        {
            var service = new SimpleService();
            var proxy = _factory.CreateProxy<ISimpleService>(service);

            // Proxy should be callable.
            var result = proxy.NotInstrumented(42);
            Assert.AreEqual("plain-42", result);
        }

        [TestMethod]
        public void CreateProxy_NullTarget_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => _factory.CreateProxy<ISimpleService>(null!));
        }

        [TestMethod]
        public void CreateProxy_NonInterface_ThrowsArgumentException()
        {
            var obj = new NotAnInterface { Value = 1 };

            Assert.ThrowsExactly<ArgumentException>(
                () => _factory.CreateProxy(obj));
        }

        [TestMethod]
        public void CreateProxy_NullScopeFactory_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new TelemetryProxyFactory(null!));
        }

        [TestMethod]
        public void CreateProxy_WithOptions_PassesOptionsToProxy()
        {
            var service = new SimpleService();
            var options = new InstrumentationOptions { MaxCaptureDepth = 5 };

            var proxy = _factory.CreateProxy<ISimpleService>(service, options);
            Assert.IsNotNull(proxy);
        }

        [TestMethod]
        public void CreateProxy_WithoutOptions_UsesDefaults()
        {
            var service = new SimpleService();
            var proxy = _factory.CreateProxy<ISimpleService>(service);

            // Should not throw — uses default options.
            proxy.GetValue(1);
        }

        // ─── MULTIPLE PROXIES ───────────────────────────────────────────

        [TestMethod]
        public void CreateProxy_MultipleProxies_AreIndependent()
        {
            var svc1 = new SimpleService();
            var svc2 = new SimpleService();

            var proxy1 = _factory.CreateProxy<ISimpleService>(svc1);
            var proxy2 = _factory.CreateProxy<ISimpleService>(svc2);

            Assert.AreNotSame(proxy1, proxy2);
        }

        // ─── DIFFERENT INTERFACE TYPES ──────────────────────────────────

        [TestMethod]
        public void CreateProxy_DifferentInterfaces_AllWork()
        {
            var proxy1 = _factory.CreateProxy<ISimpleService>(new SimpleService());
            var proxy2 = _factory.CreateProxy<IOrderService>(new OrderService());
            var proxy3 = _factory.CreateProxy<IExceptionService>(new ExceptionService());

            Assert.IsNotNull(proxy1);
            Assert.IsNotNull(proxy2);
            Assert.IsNotNull(proxy3);
        }
    }
}
