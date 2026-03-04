using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using HVO.Enterprise.Telemetry.Data.RabbitMQ;
using HVO.Enterprise.Telemetry.Data.RabbitMQ.Instrumentation;
using RabbitMQ.Client;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify RabbitMQ telemetry instrumentation against a real broker.
    /// These tests are skipped automatically when RabbitMQ is not reachable.
    /// Start RabbitMQ via: <c>docker compose -f docker-compose.test.yml up -d rabbitmq</c>
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class RabbitMqConnectivityTests
    {
        private static readonly string _host =
            Environment.GetEnvironmentVariable("HVO_RABBITMQ_HOST") ?? "127.0.0.1";
        private static readonly int _port =
            int.TryParse(Environment.GetEnvironmentVariable("HVO_RABBITMQ_PORT"), out var p) ? p : 5672;
        private static readonly string _user =
            Environment.GetEnvironmentVariable("HVO_RABBITMQ_USER") ?? "guest";
        private static readonly string _pass =
            Environment.GetEnvironmentVariable("HVO_RABBITMQ_PASS") ?? "guest";

        private static bool IsRabbitMqAvailable()
        {
            try
            {
                using var tcp = new TcpClient();
                tcp.Connect(_host, _port);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SkipIfUnavailable()
        {
            if (!IsRabbitMqAvailable())
            {
                Assert.Inconclusive(
                    $"RabbitMQ is not reachable at {_host}:{_port}. " +
                    "Start it with: docker compose -f docker-compose.test.yml up -d rabbitmq");
            }
        }

        private IConnection CreateConnection()
        {
            var factory = new ConnectionFactory
            {
                HostName = _host,
                Port = _port,
                UserName = _user,
                Password = _pass
            };
            return factory.CreateConnection();
        }

        [TestMethod]
        public void RabbitMq_TcpConnectivity_ReachesPort()
        {
            SkipIfUnavailable();

            using var tcp = new TcpClient();
            tcp.Connect(_host, _port);

            Assert.IsTrue(tcp.Connected, "Should be connected to RabbitMQ AMQP port");
        }

        [TestMethod]
        public void RabbitMq_Connection_EstablishesSuccessfully()
        {
            SkipIfUnavailable();

            using var connection = CreateConnection();

            Assert.IsTrue(connection.IsOpen, "RabbitMQ connection should be open");
        }

        [TestMethod]
        public void RabbitMq_HeaderPropagator_InjectsAndExtractsTraceContext()
        {
            SkipIfUnavailable();

            using var listener = new ActivityListener
            {
                ShouldListenTo = src => src.Name == RabbitMqActivitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var connection = CreateConnection();
            using var channel = connection.CreateModel();

            var queueName = "hvo.integration.test." + Guid.NewGuid().ToString("N");
            channel.QueueDeclare(queueName, durable: false, exclusive: true, autoDelete: true);

            // Start an activity and inject context into message headers
            using var publishActivity = RabbitMqActivitySource.Source.StartActivity(
                "rabbitmq.publish", ActivityKind.Producer);

            var headers = new Dictionary<string, object>();
            RabbitMqHeaderPropagator.Inject(headers, publishActivity);

            var props = channel.CreateBasicProperties();
            props.Headers = headers;

            var body = Encoding.UTF8.GetBytes("integration-test-payload");
            channel.BasicPublish(string.Empty, queueName, props, body);

            // Consume the message and extract context
            var result = channel.BasicGet(queueName, autoAck: true);

            Assert.IsNotNull(result, "Should receive the published message");

            var extractedContext = RabbitMqHeaderPropagator.Extract(result.BasicProperties.Headers);

            if (publishActivity != null)
            {
                Assert.AreEqual(
                    publishActivity.TraceId,
                    extractedContext.TraceId,
                    "Extracted trace ID should match injected trace ID");
            }

            channel.QueueDelete(queueName);
        }

        [TestMethod]
        public void RabbitMq_ActivitySource_HasCorrectName()
        {
            Assert.AreEqual("HVO.Enterprise.Telemetry.Data.RabbitMQ", RabbitMqActivitySource.Name);
        }
    }
}
