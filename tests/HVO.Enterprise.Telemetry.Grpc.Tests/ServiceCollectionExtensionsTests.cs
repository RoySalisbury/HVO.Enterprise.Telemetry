using System;
using System.Linq;
using HVO.Enterprise.Telemetry.Grpc;
using HVO.Enterprise.Telemetry.Grpc.Client;
using HVO.Enterprise.Telemetry.Grpc.Extensions;
using HVO.Enterprise.Telemetry.Grpc.Server;
using Microsoft.Extensions.DependencyInjection;

namespace HVO.Enterprise.Telemetry.Grpc.Tests
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddGrpcTelemetry_NullServices_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                ((IServiceCollection)null!).AddGrpcTelemetry());
        }

        [TestMethod]
        public void AddGrpcTelemetry_RegistersServerInterceptor()
        {
            var services = new ServiceCollection();

            services.AddGrpcTelemetry();

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(TelemetryServerInterceptor)));
        }

        [TestMethod]
        public void AddGrpcTelemetry_RegistersClientInterceptor()
        {
            var services = new ServiceCollection();

            services.AddGrpcTelemetry();

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(TelemetryClientInterceptor)));
        }

        [TestMethod]
        public void AddGrpcTelemetry_RegistersBothInterceptors()
        {
            var services = new ServiceCollection();

            services.AddGrpcTelemetry();

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(TelemetryServerInterceptor)));
            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(TelemetryClientInterceptor)));
        }

        [TestMethod]
        public void AddGrpcTelemetry_Idempotent_DoesNotRegisterDuplicate()
        {
            var services = new ServiceCollection();

            services.AddGrpcTelemetry();
            services.AddGrpcTelemetry();

            var serverCount = services.Count(s => s.ServiceType == typeof(TelemetryServerInterceptor));
            Assert.AreEqual(1, serverCount);

            var clientCount = services.Count(s => s.ServiceType == typeof(TelemetryClientInterceptor));
            Assert.AreEqual(1, clientCount);
        }

        [TestMethod]
        public void AddGrpcTelemetry_WithConfigure_AppliesOptions()
        {
            var services = new ServiceCollection();

            services.AddGrpcTelemetry(options =>
            {
                options.EnableServerInterceptor = false;
                options.SuppressHealthChecks = false;
                options.CorrelationHeaderName = "x-request-id";
            });

            var provider = services.BuildServiceProvider();
            var serverInterceptor = provider.GetRequiredService<TelemetryServerInterceptor>();
            Assert.IsNotNull(serverInterceptor);
        }

        [TestMethod]
        public void AddGrpcTelemetry_ResolvesServerInterceptor()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddGrpcTelemetry();

            var provider = services.BuildServiceProvider();
            var interceptor = provider.GetRequiredService<TelemetryServerInterceptor>();

            Assert.IsNotNull(interceptor);
        }

        [TestMethod]
        public void AddGrpcTelemetry_ResolvesClientInterceptor()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddGrpcTelemetry();

            var provider = services.BuildServiceProvider();
            var interceptor = provider.GetRequiredService<TelemetryClientInterceptor>();

            Assert.IsNotNull(interceptor);
        }

        [TestMethod]
        public void AddGrpcTelemetry_ReturnsServiceCollection()
        {
            var services = new ServiceCollection();

            var result = services.AddGrpcTelemetry();

            Assert.AreSame(services, result);
        }
    }
}
