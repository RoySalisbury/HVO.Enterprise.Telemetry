using System.Collections.Generic;
using System.Linq;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.OpenTelemetry;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.OpenTelemetry.Tests
{
    [TestClass]
    public class HvoActivitySourceRegistrarTests
    {
        [TestMethod]
        public void GetSourceNames_IncludesDefaultSources()
        {
            var telemetryOptions = Options.Create(new TelemetryOptions());
            var registrar = new HvoActivitySourceRegistrar(telemetryOptions);

            var names = registrar.GetSourceNames().ToList();

            Assert.IsTrue(names.Contains("HVO.Enterprise.Telemetry"));
            Assert.IsTrue(names.Contains("HVO.Enterprise.Telemetry.Http"));
            Assert.IsTrue(names.Contains("HVO.Enterprise.Telemetry.Data"));
        }

        [TestMethod]
        public void GetSourceNames_IncludesCustomSources()
        {
            var telemetryOptions = Options.Create(new TelemetryOptions
            {
                ActivitySources = new List<string>
                {
                    "HVO.Enterprise.Telemetry",
                    "MyApp.Custom.Source"
                }
            });
            var registrar = new HvoActivitySourceRegistrar(telemetryOptions);

            var names = registrar.GetSourceNames().ToList();

            Assert.IsTrue(names.Contains("MyApp.Custom.Source"));
        }

        [TestMethod]
        public void GetSourceNames_DeduplicatesDefaultSources()
        {
            var telemetryOptions = Options.Create(new TelemetryOptions
            {
                ActivitySources = new List<string>
                {
                    "HVO.Enterprise.Telemetry",
                    "HVO.Enterprise.Telemetry.Http",
                    "HVO.Enterprise.Telemetry.Data"
                }
            });
            var registrar = new HvoActivitySourceRegistrar(telemetryOptions);

            var names = registrar.GetSourceNames().ToList();
            var telemetryCount = names.Count(n => n == "HVO.Enterprise.Telemetry");

            Assert.AreEqual(1, telemetryCount);
        }

        [TestMethod]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<System.ArgumentNullException>(
                () => new HvoActivitySourceRegistrar(null!));
        }
    }
}
