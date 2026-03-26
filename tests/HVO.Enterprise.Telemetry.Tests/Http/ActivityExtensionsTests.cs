using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HVO.Enterprise.Telemetry.Http;

namespace HVO.Enterprise.Telemetry.Tests.Http
{
    [TestClass]
    public class ActivityExtensionsTests
    {
        [TestMethod]
        public void RecordException_SetsExceptionType()
        {
            using var listener = CreateListener();
            using var source = new ActivitySource("Test.ActivityExtensions." + Guid.NewGuid().ToString("N"));
            using var activity = source.StartActivity("test-op")!;

            activity.RecordException(new InvalidOperationException("test error"));

            var exceptionEvent = activity.Events.First(e => e.Name == "exception");
            var tags = exceptionEvent.Tags.ToDictionary(t => t.Key, t => t.Value);
            Assert.AreEqual("System.InvalidOperationException", tags["exception.type"]);
        }

        [TestMethod]
        public void RecordException_SetsExceptionMessage()
        {
            using var listener = CreateListener();
            using var source = new ActivitySource("Test.ActivityExtensions." + Guid.NewGuid().ToString("N"));
            using var activity = source.StartActivity("test-op")!;

            activity.RecordException(new ArgumentException("bad argument"));

            var exceptionEvent = activity.Events.First(e => e.Name == "exception");
            var tags = exceptionEvent.Tags.ToDictionary(t => t.Key, t => t.Value);
            Assert.AreEqual("bad argument", tags["exception.message"]);
        }

        [TestMethod]
        public void RecordException_SetsStackTrace_WhenAvailable()
        {
            using var listener = CreateListener();
            using var source = new ActivitySource("Test.ActivityExtensions." + Guid.NewGuid().ToString("N"));
            using var activity = source.StartActivity("test-op")!;

            Exception captured;
            try
            {
                throw new InvalidOperationException("thrown exception");
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            activity.RecordException(captured);

            var exceptionEvent = activity.Events.First(e => e.Name == "exception");
            var tags = exceptionEvent.Tags.ToDictionary(t => t.Key, t => t.Value);
            Assert.IsTrue(tags.ContainsKey("exception.stacktrace"),
                "Stack trace should be recorded when available.");
            Assert.IsTrue(((string)tags["exception.stacktrace"]!).Contains("RecordException_SetsStackTrace_WhenAvailable"),
                "Stack trace should contain the throwing method name.");
        }

        [TestMethod]
        public void RecordException_OmitsStackTrace_WhenNull()
        {
            using var listener = CreateListener();
            using var source = new ActivitySource("Test.ActivityExtensions." + Guid.NewGuid().ToString("N"));
            using var activity = source.StartActivity("test-op")!;

            // Exception created but not thrown has null StackTrace
            var exception = new InvalidOperationException("not thrown");

            activity.RecordException(exception);

            var exceptionEvent = activity.Events.First(e => e.Name == "exception");
            var tags = exceptionEvent.Tags.ToDictionary(t => t.Key, t => t.Value);
            Assert.IsFalse(tags.ContainsKey("exception.stacktrace"),
                "Stack trace tag should be omitted when null.");
        }

        [TestMethod]
        public void RecordException_AddsExceptionActivityEvent()
        {
            using var listener = CreateListener();
            using var source = new ActivitySource("Test.ActivityExtensions." + Guid.NewGuid().ToString("N"));
            using var activity = source.StartActivity("test-op")!;

            activity.RecordException(new Exception("test"));

            var eventNames = activity.Events.Select(e => e.Name).ToList();
            CollectionAssert.Contains(eventNames, "exception");
        }

        [TestMethod]
        public void RecordException_ReturnsSameActivity_ForChaining()
        {
            using var listener = CreateListener();
            using var source = new ActivitySource("Test.ActivityExtensions." + Guid.NewGuid().ToString("N"));
            using var activity = source.StartActivity("test-op")!;

            var result = activity.RecordException(new Exception("test"));

            Assert.AreSame(activity, result);
        }

        [TestMethod]
        public void RecordException_NullActivity_ThrowsArgumentNull()
        {
            Activity? nullActivity = null;
            Assert.ThrowsExactly<ArgumentNullException>(
                () => nullActivity!.RecordException(new Exception("test")));
        }

        [TestMethod]
        public void RecordException_NullException_ThrowsArgumentNull()
        {
            using var listener = CreateListener();
            using var source = new ActivitySource("Test.ActivityExtensions." + Guid.NewGuid().ToString("N"));
            using var activity = source.StartActivity("test-op")!;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => activity.RecordException(null!));
        }

        private static ActivityListener CreateListener()
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);
            return listener;
        }
    }
}
