using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.AppInsights;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace HVO.Enterprise.Telemetry.AppInsights.Tests
{
    [TestClass]
    public class CorrelationTelemetryInitializerTests
    {
        private Activity? _activity;

        [TestInitialize]
        public void Initialize()
        {
            // Clear any leaked correlation state
            Telemetry.Shutdown();
            CorrelationContext.Current = null!;
            Activity.Current = null;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _activity?.Dispose();
            _activity = null;
            Telemetry.Shutdown();
            CorrelationContext.Current = null!;
            Activity.Current = null;
        }

        [TestMethod]
        public void Constructor_NullPropertyName_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new CorrelationTelemetryInitializer(propertyName: null!));
        }

        [TestMethod]
        public void Constructor_EmptyPropertyName_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new CorrelationTelemetryInitializer(propertyName: ""));
        }

        [TestMethod]
        public void Constructor_DefaultPropertyName_IsCorrelationId()
        {
            // Default property name constant should be "CorrelationId"
            Assert.AreEqual("CorrelationId", CorrelationTelemetryInitializer.DefaultPropertyName);
        }

        [TestMethod]
        public void FallbackToActivity_DefaultTrue()
        {
            var initializer = new CorrelationTelemetryInitializer();
            Assert.IsTrue(initializer.FallbackToActivity);
        }

        [TestMethod]
        public void FallbackToActivity_CanBeDisabled()
        {
            var initializer = new CorrelationTelemetryInitializer(fallbackToActivity: false);
            Assert.IsFalse(initializer.FallbackToActivity);
        }

        [TestMethod]
        public void Initialize_NullTelemetry_ThrowsArgumentNullException()
        {
            var initializer = new CorrelationTelemetryInitializer();
            Assert.ThrowsExactly<ArgumentNullException>(() => initializer.Initialize(null!));
        }

        [TestMethod]
        public void Initialize_WithExplicitCorrelation_AddsCorrelationId()
        {
            using (CorrelationContext.BeginScope("corr-123"))
            {
                var initializer = new CorrelationTelemetryInitializer();
                var telemetry = new RequestTelemetry();

                initializer.Initialize(telemetry);

                Assert.AreEqual("corr-123", telemetry.Properties["CorrelationId"]);
            }
        }

        [TestMethod]
        public void Initialize_WithExplicitCorrelation_UsesCustomPropertyName()
        {
            using (CorrelationContext.BeginScope("corr-456"))
            {
                var initializer = new CorrelationTelemetryInitializer(propertyName: "CustomCorrelation");
                var telemetry = new RequestTelemetry();

                initializer.Initialize(telemetry);

                Assert.IsTrue(telemetry.Properties.ContainsKey("CustomCorrelation"));
                Assert.AreEqual("corr-456", telemetry.Properties["CustomCorrelation"]);
            }
        }

        [TestMethod]
        public void Initialize_DoesNotOverwriteExistingCorrelationId()
        {
            using (CorrelationContext.BeginScope("new-id"))
            {
                var initializer = new CorrelationTelemetryInitializer();
                var telemetry = new RequestTelemetry();
                telemetry.Properties["CorrelationId"] = "existing-id";

                initializer.Initialize(telemetry);

                Assert.AreEqual("existing-id", telemetry.Properties["CorrelationId"]);
            }
        }

        [TestMethod]
        public void Initialize_NoCorrelation_FallsBackToW3CActivity()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var initializer = new CorrelationTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual(_activity.TraceId.ToString(), telemetry.Properties["CorrelationId"]);
        }

        [TestMethod]
        public void Initialize_NoCorrelation_FallsBackToHierarchicalActivity()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.Hierarchical)
                .Start();

            var initializer = new CorrelationTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            var expected = _activity.RootId ?? _activity.Id;
            Assert.AreEqual(expected, telemetry.Properties["CorrelationId"]);
        }

        [TestMethod]
        public void Initialize_NoCorrelationFallbackDisabled_DoesNotAddProperty()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var initializer = new CorrelationTelemetryInitializer(fallbackToActivity: false);
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.IsFalse(telemetry.Properties.ContainsKey("CorrelationId"));
        }

        [TestMethod]
        public void Initialize_NoCorrelationNoActivity_DoesNotAddProperty()
        {
            var initializer = new CorrelationTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.IsFalse(telemetry.Properties.ContainsKey("CorrelationId"));
        }

        [TestMethod]
        public void Initialize_ExplicitCorrelation_TakesPrecedenceOverActivity()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            using (CorrelationContext.BeginScope("explicit-id"))
            {
                var initializer = new CorrelationTelemetryInitializer();
                var telemetry = new RequestTelemetry();

                initializer.Initialize(telemetry);

                Assert.AreEqual("explicit-id", telemetry.Properties["CorrelationId"]);
            }
        }

        [TestMethod]
        public void Initialize_ImplementsITelemetryInitializer()
        {
            var initializer = new CorrelationTelemetryInitializer();
            Assert.IsInstanceOfType(initializer, typeof(ITelemetryInitializer));
        }

        [TestMethod]
        public void Initialize_ExceptionTelemetry_AddsCorrelationId()
        {
            using (CorrelationContext.BeginScope("exception-corr"))
            {
                var initializer = new CorrelationTelemetryInitializer();
                var telemetry = new ExceptionTelemetry(new InvalidOperationException("test"));

                initializer.Initialize(telemetry);

                Assert.AreEqual("exception-corr", telemetry.Properties["CorrelationId"]);
            }
        }

        [TestMethod]
        public void Initialize_DependencyTelemetry_AddsCorrelationId()
        {
            using (CorrelationContext.BeginScope("dep-corr"))
            {
                var initializer = new CorrelationTelemetryInitializer();
                var telemetry = new DependencyTelemetry();

                initializer.Initialize(telemetry);

                Assert.AreEqual("dep-corr", telemetry.Properties["CorrelationId"]);
            }
        }

        [TestMethod]
        public void Initialize_TraceTelemetry_AddsCorrelationId()
        {
            using (CorrelationContext.BeginScope("trace-corr"))
            {
                var initializer = new CorrelationTelemetryInitializer();
                var telemetry = new TraceTelemetry("test trace");

                initializer.Initialize(telemetry);

                Assert.AreEqual("trace-corr", telemetry.Properties["CorrelationId"]);
            }
        }
    }
}
