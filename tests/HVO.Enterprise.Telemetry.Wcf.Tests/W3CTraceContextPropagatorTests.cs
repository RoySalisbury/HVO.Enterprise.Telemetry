using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Wcf.Propagation;

namespace HVO.Enterprise.Telemetry.Wcf.Tests
{
    [TestClass]
    public class W3CTraceContextPropagatorTests
    {
        [TestMethod]
        public void TryParseTraceParent_ValidTraceParent_ReturnsTrue()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out var traceId,
                out var spanId,
                out var traceFlags);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("0af7651916cd43dd8448eb211c80319c", traceId);
            Assert.AreEqual("b7ad6b7169203331", spanId);
            Assert.AreEqual(ActivityTraceFlags.Recorded, traceFlags);
        }

        [TestMethod]
        public void TryParseTraceParent_ValidWithNoRecordFlag_ReturnsCorrectFlags()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out var traceFlags);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(ActivityTraceFlags.None, traceFlags);
        }

        [TestMethod]
        public void TryParseTraceParent_NullInput_ReturnsFalse()
        {
            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                null,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_EmptyInput_ReturnsFalse()
        {
            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                string.Empty,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_WhitespaceInput_ReturnsFalse()
        {
            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                "   ",
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_WrongVersion_ReturnsFalse()
        {
            // Arrange
            var traceparent = "01-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_TooFewParts_ReturnsFalse()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_TooManyParts_ReturnsFalse()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01-extra";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_InvalidHexInTraceId_ReturnsFalse()
        {
            // Arrange
            var traceparent = "00-ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ-b7ad6b7169203331-01";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_AllZeroTraceId_ReturnsFalse()
        {
            // Arrange
            var traceparent = "00-00000000000000000000000000000000-b7ad6b7169203331-01";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_AllZeroSpanId_ReturnsFalse()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-0000000000000000-01";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_ShortTraceId_ReturnsFalse()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd-b7ad6b7169203331-01";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_ShortSpanId_ReturnsFalse()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b71-01";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseTraceParent_UpperCaseHex_Succeeds()
        {
            // Arrange - W3C spec says lowercase, but uppercase should also parse
            var traceparent = "00-0AF7651916CD43DD8448EB211C80319C-B7AD6B7169203331-01";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out var traceId,
                out var spanId,
                out _);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("0AF7651916CD43DD8448EB211C80319C", traceId);
            Assert.AreEqual("B7AD6B7169203331", spanId);
        }

        [TestMethod]
        public void CreateTraceParent_FromActivity_CreatesValidFormat()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            using var source = new ActivitySource("test.wcf.propagator");
            using var activity = source.StartActivity("TestOp")!;

            // Act
            var traceparent = W3CTraceContextPropagator.CreateTraceParent(activity);

            // Assert
            Assert.IsNotNull(traceparent);
            Assert.IsTrue(traceparent.StartsWith("00-"), "Should start with version 00");
            Assert.IsTrue(traceparent.Contains(activity.TraceId.ToHexString()));
            Assert.IsTrue(traceparent.Contains(activity.SpanId.ToHexString()));

            // Verify it can be parsed back
            var parseResult = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out var parsedTraceId,
                out var parsedSpanId,
                out _);

            Assert.IsTrue(parseResult);
            Assert.AreEqual(activity.TraceId.ToHexString(), parsedTraceId);
            Assert.AreEqual(activity.SpanId.ToHexString(), parsedSpanId);
        }

        [TestMethod]
        public void CreateTraceParent_FromComponents_CreatesValidFormat()
        {
            // Arrange
            var traceId = "0af7651916cd43dd8448eb211c80319c";
            var spanId = "b7ad6b7169203331";
            var flags = ActivityTraceFlags.Recorded;

            // Act
            var traceparent = W3CTraceContextPropagator.CreateTraceParent(traceId, spanId, flags);

            // Assert
            Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", traceparent);
        }

        [TestMethod]
        public void CreateTraceParent_WithNoneFlags_EndsWithZero()
        {
            // Arrange
            var traceId = "0af7651916cd43dd8448eb211c80319c";
            var spanId = "b7ad6b7169203331";

            // Act
            var traceparent = W3CTraceContextPropagator.CreateTraceParent(
                traceId, spanId, ActivityTraceFlags.None);

            // Assert
            Assert.IsTrue(traceparent.EndsWith("-00"));
        }

        [TestMethod]
        public void CreateTraceParent_NullActivity_ThrowsArgumentNullException()
        {
            // Act
            Assert.ThrowsExactly<ArgumentNullException>(() => W3CTraceContextPropagator.CreateTraceParent((Activity)null!));
        }

        [TestMethod]
        public void CreateTraceParent_NullTraceId_ThrowsArgumentException()
        {
            // Act
            Assert.ThrowsExactly<ArgumentException>(() => W3CTraceContextPropagator.CreateTraceParent(null!, "b7ad6b7169203331", ActivityTraceFlags.None));
        }

        [TestMethod]
        public void CreateTraceParent_NullSpanId_ThrowsArgumentException()
        {
            // Act
            Assert.ThrowsExactly<ArgumentException>(() => W3CTraceContextPropagator.CreateTraceParent("0af7651916cd43dd8448eb211c80319c", null!, ActivityTraceFlags.None));
        }

        [TestMethod]
        public void GetTraceState_WithTraceState_ReturnsValue()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            using var source = new ActivitySource("test.wcf.tracestate");
            using var activity = source.StartActivity("TestOp")!;
            activity.TraceStateString = "congo=t61rcWkgMzE";

            // Act
            var traceState = W3CTraceContextPropagator.GetTraceState(activity);

            // Assert
            Assert.AreEqual("congo=t61rcWkgMzE", traceState);
        }

        [TestMethod]
        public void GetTraceState_WithoutTraceState_ReturnsNull()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            using var source = new ActivitySource("test.wcf.tracestate2");
            using var activity = source.StartActivity("TestOp")!;

            // Act
            var traceState = W3CTraceContextPropagator.GetTraceState(activity);

            // Assert
            Assert.IsNull(traceState);
        }

        [TestMethod]
        public void GetTraceState_NullActivity_ThrowsArgumentNullException()
        {
            // Act
            Assert.ThrowsExactly<ArgumentNullException>(() => W3CTraceContextPropagator.GetTraceState(null!));
        }

        [TestMethod]
        public void TryParseTraceParent_InvalidFlagsHex_ReturnsFalse()
        {
            // Arrange
            var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-ZZ";

            // Act
            var result = W3CTraceContextPropagator.TryParseTraceParent(
                traceparent,
                out _,
                out _,
                out _);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
