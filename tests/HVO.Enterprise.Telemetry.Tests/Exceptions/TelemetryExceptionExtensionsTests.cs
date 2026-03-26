using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HVO.Enterprise.Telemetry.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Exceptions
{
    [TestClass]
    public class TelemetryExceptionExtensionsTests
    {
        [TestInitialize]
        public void Setup()
        {
            TelemetryExceptionExtensions.Configure(new ExceptionTrackingOptions());
        }

        [TestMethod]
        public void RecordException_NullException_ThrowsArgumentNullException()
        {
            Exception ex = null!;
            Assert.ThrowsExactly<ArgumentNullException>(() => ex.RecordException());
        }

        [TestMethod]
        public void RecordException_ValidException_DoesNotThrow()
        {
            Activity.Current = null;
            var ex = new InvalidOperationException("test error");
            ex.RecordException();
        }

        [TestMethod]
        public void RecordException_AggregatorTracksException()
        {
            Activity.Current = null;
            var before = TelemetryExceptionExtensions.GetAggregator().TotalExceptions;
            var ex = new InvalidOperationException("aggregator-test");
            ex.RecordException();
            Assert.IsTrue(TelemetryExceptionExtensions.GetAggregator().TotalExceptions > before);
        }

        [TestMethod]
        public void RecordException_AddsToActivity()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            using var activitySource = new ActivitySource("Test");
            using var activity = activitySource.StartActivity("TestOp", ActivityKind.Internal);
            Assert.IsNotNull(activity);

            var exception = new InvalidOperationException("Test error");
            exception.RecordException();

            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
            Assert.IsTrue(activity.Tags.Any(tag => tag.Key == "exception.type"));
            Assert.IsTrue(activity.Tags.Any(tag => tag.Key == "exception.fingerprint"));
        }

        [TestMethod]
        public void RecordException_WithCaptureMessage_AddsMessageEvent()
        {
            TelemetryExceptionExtensions.Configure(
                new ExceptionTrackingOptions(captureMessage: true, captureStackTrace: false));

            using var activitySource = new ActivitySource("test-ext-msg");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-ext-msg",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("op");
            Assert.IsNotNull(activity);

            new InvalidOperationException("message-capture").RecordException();

            var events = activity.Events.ToList();
            Assert.IsTrue(events.Count > 0, "Should have at least one event");
            var exEvent = events.Find(e => e.Name == "exception");
            Assert.IsNotNull(exEvent);
            Assert.IsTrue(exEvent.Tags.Any(t => t.Key == "exception.message"));
        }

        [TestMethod]
        public void RecordException_WithCaptureStackTrace_AddsStackTraceEvent()
        {
            TelemetryExceptionExtensions.Configure(
                new ExceptionTrackingOptions(captureMessage: false, captureStackTrace: true));

            using var activitySource = new ActivitySource("test-ext-st");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-ext-st",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("op");
            Assert.IsNotNull(activity);

            Exception thrown;
            try { throw new InvalidOperationException("with-stack"); }
            catch (Exception ex) { thrown = ex; }

            thrown.RecordException();

            var events = activity.Events.ToList();
            var exEvent = events.Find(e => e.Name == "exception");
            Assert.IsNotNull(exEvent);
            Assert.IsTrue(exEvent.Tags.Any(t => t.Key == "exception.stacktrace"));
        }

        [TestMethod]
        public void RecordException_IncludeMessageInActivityStatus_SetsMessageAsDescription()
        {
            TelemetryExceptionExtensions.Configure(
                new ExceptionTrackingOptions(includeMessageInActivityStatus: true));

            using var activitySource = new ActivitySource("test-ext-status-msg");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-ext-status-msg",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("op");
            Assert.IsNotNull(activity);

            new InvalidOperationException("detailed error").RecordException();
            Assert.AreEqual("detailed error", activity.StatusDescription);
        }

        [TestMethod]
        public void RecordException_WithoutIncludeMessage_SetsTypeNameAsDescription()
        {
            TelemetryExceptionExtensions.Configure(
                new ExceptionTrackingOptions(includeMessageInActivityStatus: false));

            using var activitySource = new ActivitySource("test-ext-status-type");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-ext-status-type",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("op");
            Assert.IsNotNull(activity);

            new ArgumentNullException("p").RecordException();
            Assert.AreEqual("ArgumentNullException", activity.StatusDescription);
        }

        [TestMethod]
        public void RecordException_NoCaptureFlags_StillAddsEvent()
        {
            TelemetryExceptionExtensions.Configure(
                new ExceptionTrackingOptions(captureMessage: false, captureStackTrace: false));

            using var activitySource = new ActivitySource("test-ext-noflags");
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "test-ext-noflags",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity("op");
            Assert.IsNotNull(activity);

            new Exception("min").RecordException();
            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
            Assert.IsTrue(activity.Events.Any(e => e.Name == "exception"));
        }

        [TestMethod]
        public void RecordException_NoActiveActivity_DoesNotThrow()
        {
            Activity.Current = null;
            new Exception("orphan").RecordException();
        }

        [TestMethod]
        public void Configure_NullOptions_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => TelemetryExceptionExtensions.Configure(null!));
        }

        [TestMethod]
        public void Configure_ValidOptions_UpdatesOptions()
        {
            var opts = new ExceptionTrackingOptions(
                captureMessage: true,
                captureStackTrace: true,
                includeMessageInActivityStatus: true);
            TelemetryExceptionExtensions.Configure(opts);

            var current = TelemetryExceptionExtensions.Options;
            Assert.IsTrue(current.CaptureMessage);
            Assert.IsTrue(current.CaptureStackTrace);
            Assert.IsTrue(current.IncludeMessageInActivityStatus);
        }

        [TestMethod]
        public void Options_ReturnsNonNull()
        {
            Assert.IsNotNull(TelemetryExceptionExtensions.Options);
        }

        [TestMethod]
        public void GetAggregator_ReturnsSameInstance()
        {
            Assert.AreSame(
                TelemetryExceptionExtensions.GetAggregator(),
                TelemetryExceptionExtensions.GetAggregator());
        }
    }
}
