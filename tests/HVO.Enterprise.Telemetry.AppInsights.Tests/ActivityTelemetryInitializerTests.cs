using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using HVO.Enterprise.Telemetry.AppInsights;

namespace HVO.Enterprise.Telemetry.AppInsights.Tests
{
    [TestClass]
    public class ActivityTelemetryInitializerTests
    {
        private Activity? _activity;

        [TestCleanup]
        public void Cleanup()
        {
            _activity?.Dispose();
            _activity = null;
        }

        [TestMethod]
        public void Initialize_NullTelemetry_ThrowsArgumentNullException()
        {
            var initializer = new ActivityTelemetryInitializer();
            Assert.ThrowsExactly<ArgumentNullException>(() => initializer.Initialize(null!));
        }

        [TestMethod]
        public void Initialize_NoActivity_DoesNotThrow()
        {
            // Ensure no Activity is current
            Activity.Current = null;

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            // Should not throw
            initializer.Initialize(telemetry);
        }

        [TestMethod]
        public void Initialize_NoActivity_DoesNotModifyTelemetry()
        {
            Activity.Current = null;

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual(0, telemetry.Properties.Count);
        }

        [TestMethod]
        public void Initialize_W3CActivity_SetsOperationId()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual(_activity.TraceId.ToString(), telemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void Initialize_W3CActivity_SetsSpanId()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual(_activity.SpanId.ToString(), telemetry.Id);
        }

        [TestMethod]
        public void Initialize_W3CActivityWithParent_SetsParentId()
        {
            var parent = new Activity("parent")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            _activity = new Activity("child")
                .Start();

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual(parent.SpanId.ToString(), telemetry.Context.Operation.ParentId);

            parent.Dispose();
        }

        [TestMethod]
        public void Initialize_W3CActivity_CopiesTagsToProperties()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            _activity.SetTag("user.id", "user-123");
            _activity.SetTag("tenant.id", "tenant-456");

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual("user-123", telemetry.Properties["user.id"]);
            Assert.AreEqual("tenant-456", telemetry.Properties["tenant.id"]);
        }

        [TestMethod]
        public void Initialize_W3CActivity_DoesNotOverwriteExistingProperties()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            _activity.SetTag("existing-key", "from-activity");

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();
            telemetry.Properties["existing-key"] = "original-value";

            initializer.Initialize(telemetry);

            Assert.AreEqual("original-value", telemetry.Properties["existing-key"]);
        }

        [TestMethod]
        public void Initialize_W3CActivity_CopiesBaggageWithPrefix()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            _activity.AddBaggage("correlation-context", "ctx-789");
            _activity.AddBaggage("session-id", "sess-101");

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual("ctx-789", telemetry.Properties["baggage.correlation-context"]);
            Assert.AreEqual("sess-101", telemetry.Properties["baggage.session-id"]);
        }

        [TestMethod]
        public void Initialize_W3CActivityWithTraceState_AddsTracestateProperty()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            _activity.TraceStateString = "congo=lZWRzIHRoNhcm5teleABhcm5hbA";

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual("congo=lZWRzIHRoNhcm5teleABhcm5hbA", telemetry.Properties["tracestate"]);
        }

        [TestMethod]
        public void Initialize_HierarchicalActivity_SetsOperationId()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.Hierarchical)
                .Start();

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            var expectedId = _activity.RootId ?? _activity.Id;
            Assert.AreEqual(expectedId, telemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void Initialize_HierarchicalActivity_SetsActivityId()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.Hierarchical)
                .Start();

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual(_activity.Id, telemetry.Id);
        }

        [TestMethod]
        public void Initialize_NonOperationTelemetry_SetsOperationIdOnly()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new TraceTelemetry("test trace");

            initializer.Initialize(telemetry);

            // TraceTelemetry is not OperationTelemetry, so only Operation.Id should be set
            Assert.AreEqual(_activity.TraceId.ToString(), telemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void Initialize_NonISupportPropertiesTelemetry_StillSetsOperationContext()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new MetricTelemetry("test-metric", 42.0);

            initializer.Initialize(telemetry);

            Assert.AreEqual(_activity.TraceId.ToString(), telemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void Initialize_ActivityWithNullTagValue_DoesNotCopyTag()
        {
            _activity = new Activity("test-operation")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            // Activity.SetTag with null value removes the tag in modern .NET
            _activity.SetTag("null-tag", null);

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            initializer.Initialize(telemetry);

            // The tag with null value is removed from Activity.Tags, so it should not appear
            Assert.IsFalse(telemetry.Properties.ContainsKey("null-tag"));
        }

        [TestMethod]
        public void Initialize_DependencyTelemetry_SetsOperationContext()
        {
            _activity = new Activity("test-dependency")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new DependencyTelemetry();

            initializer.Initialize(telemetry);

            Assert.AreEqual(_activity.TraceId.ToString(), telemetry.Context.Operation.Id);
            Assert.AreEqual(_activity.SpanId.ToString(), telemetry.Id);
        }

        [TestMethod]
        public void Initialize_ImplementsITelemetryInitializer()
        {
            var initializer = new ActivityTelemetryInitializer();
            Assert.IsInstanceOfType(initializer, typeof(ITelemetryInitializer));
        }
    }
}
