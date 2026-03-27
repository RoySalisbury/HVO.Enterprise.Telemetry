using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class ConfigurationHttpEndpointTests
    {
        [TestMethod]
        public void Constructor_WithNullPrefix_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ConfigurationHttpEndpoint(" ", new TelemetryOptions()));
        }

        [TestMethod]
        public void Constructor_WithNullOptions_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ConfigurationHttpEndpoint("http://localhost:5055/", null!));
        }

        [TestMethod]
        public void Dispose_IsIdempotent()
        {
            using var endpoint = new ConfigurationHttpEndpoint("http://localhost:5056/", new TelemetryOptions());

            endpoint.Dispose();
            endpoint.Dispose();
        }

        [TestMethod]
        public async Task ConfigurationHttpEndpoint_UpdatesConfigurationAsync()
        {
            var port = GetAvailablePort();
            var prefix = "http://localhost:" + port + "/";

            var endpoint = new ConfigurationHttpEndpoint(prefix, new TelemetryOptions());
            var changedEvent = new ManualResetEventSlim(false);
            endpoint.ConfigurationChanged += (_, __) => changedEvent.Set();

            endpoint.Start();

            using var client = new HttpClient();
            var getResponse = await client.GetAsync(prefix + "telemetry/config");
            getResponse.EnsureSuccessStatusCode();

            var updatedOptions = new TelemetryOptions { DefaultSamplingRate = 0.25 };
            var json = JsonSerializer.Serialize(updatedOptions);
            var postResponse = await client.PostAsync(
                prefix + "telemetry/config",
                new StringContent(json, Encoding.UTF8, "application/json"));

            postResponse.EnsureSuccessStatusCode();
            Assert.IsTrue(changedEvent.Wait(TimeSpan.FromSeconds(2)));

            var updatedResponse = await client.GetAsync(prefix + "telemetry/config");
            updatedResponse.EnsureSuccessStatusCode();

            var updatedJson = await updatedResponse.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<TelemetryOptions>(updatedJson);

            Assert.IsNotNull(parsed);
            Assert.AreEqual(0.25, parsed!.DefaultSamplingRate, 0.0001);

            endpoint.Dispose();
        }

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
