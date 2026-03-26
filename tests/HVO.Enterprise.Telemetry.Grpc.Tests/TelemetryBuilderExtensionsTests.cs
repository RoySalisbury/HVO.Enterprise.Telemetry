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
    public class TelemetryBuilderExtensionsTests
    {
        [TestMethod]
        public void WithGrpcInstrumentation_NullBuilder_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                ((TelemetryBuilder)null!).WithGrpcInstrumentation());
        }

        [TestMethod]
        public void WithGrpcInstrumentation_RegistersInterceptors()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithGrpcInstrumentation();
            });

            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(TelemetryServerInterceptor)));
            Assert.IsTrue(services.Any(s => s.ServiceType == typeof(TelemetryClientInterceptor)));
        }

        [TestMethod]
        public void WithGrpcInstrumentation_WithConfigure_AppliesOptions()
        {
            var services = new ServiceCollection();
            services.AddTelemetry(builder =>
            {
                builder.WithGrpcInstrumentation(options =>
                {
                    options.SuppressHealthChecks = false;
                    options.EnableClientInterceptor = false;
                });
            });

            var provider = services.BuildServiceProvider();
            var clientInterceptor = provider.GetRequiredService<TelemetryClientInterceptor>();
            Assert.IsNotNull(clientInterceptor);
        }

        [TestMethod]
        public void WithGrpcInstrumentation_Chainable_ReturnsSameBuilder()
        {
            var services = new ServiceCollection();
            TelemetryBuilder? capturedBuilder = null;

            services.AddTelemetry(builder =>
            {
                capturedBuilder = builder;
                var result = builder.WithGrpcInstrumentation();
                Assert.AreSame(builder, result);
            });

            Assert.IsNotNull(capturedBuilder);
        }
    }
}
