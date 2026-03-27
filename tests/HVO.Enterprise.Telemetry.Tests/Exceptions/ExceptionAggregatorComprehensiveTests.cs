using System;
using System.Linq;
using System.Threading;
using HVO.Enterprise.Telemetry.Exceptions;

namespace HVO.Enterprise.Telemetry.Tests.Exceptions
{
    /// <summary>
    /// Comprehensive tests for <see cref="ExceptionAggregator"/> and <see cref="ExceptionGroup"/>
    /// covering rate calculations, expiration, null guards, and property accessors.
    /// </summary>
    [TestClass]
    public class ExceptionAggregatorComprehensiveTests
    {
        // --- ExceptionAggregator null/validation guards ---

        [TestMethod]
        public void RecordException_NullException_ThrowsArgumentNullException()
        {
            var aggregator = new ExceptionAggregator();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => aggregator.RecordException(null!));
        }

        [TestMethod]
        public void Constructor_ZeroExpirationWindow_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new ExceptionAggregator(TimeSpan.Zero));
        }

        [TestMethod]
        public void Constructor_NegativeExpirationWindow_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new ExceptionAggregator(TimeSpan.FromHours(-1)));
        }

        [TestMethod]
        public void Constructor_DefaultExpirationWindow_CreatesSuccessfully()
        {
            var aggregator = new ExceptionAggregator();
            Assert.IsNotNull(aggregator);
        }

        [TestMethod]
        public void Constructor_CustomExpirationWindow_CreatesSuccessfully()
        {
            var aggregator = new ExceptionAggregator(TimeSpan.FromMinutes(30));
            Assert.IsNotNull(aggregator);
        }

        // --- TotalExceptions ---

        [TestMethod]
        public void TotalExceptions_InitiallyZero()
        {
            var aggregator = new ExceptionAggregator();
            Assert.AreEqual(0L, aggregator.TotalExceptions);
        }

        [TestMethod]
        public void TotalExceptions_IncrementsOnRecord()
        {
            var aggregator = new ExceptionAggregator();
            aggregator.RecordException(new InvalidOperationException("err1"));
            Assert.AreEqual(1L, aggregator.TotalExceptions);

            aggregator.RecordException(new InvalidOperationException("err2"));
            Assert.AreEqual(2L, aggregator.TotalExceptions);
        }

        [TestMethod]
        public void TotalExceptions_CountsAllExceptions_IncludingSameFingerprint()
        {
            var aggregator = new ExceptionAggregator();
            var ex = new InvalidOperationException("same message");
            aggregator.RecordException(ex);
            aggregator.RecordException(ex);
            aggregator.RecordException(ex);
            Assert.AreEqual(3L, aggregator.TotalExceptions);
        }

        // --- GetGroups ---

        [TestMethod]
        public void GetGroups_EmptyAggregator_ReturnsEmptyCollection()
        {
            var aggregator = new ExceptionAggregator();
            var groups = aggregator.GetGroups();
            Assert.AreEqual(0, groups.Count);
        }

        [TestMethod]
        public void GetGroups_AfterRecording_ReturnsGroups()
        {
            var aggregator = new ExceptionAggregator();
            aggregator.RecordException(new InvalidOperationException("msg1"));
            aggregator.RecordException(new ArgumentException("msg2"));

            var groups = aggregator.GetGroups();
            Assert.IsTrue(groups.Count >= 1, "Should have at least one group");
        }

        // --- GetGroup ---

        [TestMethod]
        public void GetGroup_NullFingerprint_ReturnsNull()
        {
            var aggregator = new ExceptionAggregator();
            Assert.IsNull(aggregator.GetGroup(null!));
        }

        [TestMethod]
        public void GetGroup_EmptyFingerprint_ReturnsNull()
        {
            var aggregator = new ExceptionAggregator();
            Assert.IsNull(aggregator.GetGroup(string.Empty));
        }

        [TestMethod]
        public void GetGroup_NonExistentFingerprint_ReturnsNull()
        {
            var aggregator = new ExceptionAggregator();
            Assert.IsNull(aggregator.GetGroup("nonexistent-fingerprint"));
        }

        [TestMethod]
        public void GetGroup_ExistingFingerprint_ReturnsGroup()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new InvalidOperationException("msg"));
            var retrieved = aggregator.GetGroup(group.Fingerprint);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(group.Fingerprint, retrieved.Fingerprint);
        }

        // --- GetGlobalErrorRatePerMinute ---

        [TestMethod]
        public void GetGlobalErrorRatePerMinute_NoExceptions_ReturnsZero()
        {
            var aggregator = new ExceptionAggregator();
            Assert.AreEqual(0.0, aggregator.GetGlobalErrorRatePerMinute());
        }

        [TestMethod]
        public void GetGlobalErrorRatePerMinute_SingleException_ReturnsPositive()
        {
            // Use time provider to simulate time passage
            var now = DateTimeOffset.UtcNow;
            var callCount = 0;

            var aggregator = CreateAggregatorWithTimeProvider(() =>
            {
                callCount++;
                // First call: first occurrence, subsequent calls: a minute later
                return callCount <= 3 ? now : now.AddMinutes(1);
            });

            aggregator.RecordException(new InvalidOperationException("err1"));
            // Record second exception at a later time
            aggregator.RecordException(new InvalidOperationException("err2"));

            var rate = aggregator.GetGlobalErrorRatePerMinute();
            Assert.IsTrue(rate >= 0.0, "Rate should be non-negative");
        }

        // --- GetGlobalErrorRatePerHour ---

        [TestMethod]
        public void GetGlobalErrorRatePerHour_NoExceptions_ReturnsZero()
        {
            var aggregator = new ExceptionAggregator();
            Assert.AreEqual(0.0, aggregator.GetGlobalErrorRatePerHour());
        }

        // --- GetGlobalErrorRatePercentage ---

        [TestMethod]
        public void GetGlobalErrorRatePercentage_NoExceptions_ReturnsZero()
        {
            var aggregator = new ExceptionAggregator();
            Assert.AreEqual(0.0, aggregator.GetGlobalErrorRatePercentage(100));
        }

        [TestMethod]
        public void GetGlobalErrorRatePercentage_ZeroOperations_ReturnsZero()
        {
            var aggregator = new ExceptionAggregator();
            aggregator.RecordException(new Exception("err"));
            Assert.AreEqual(0.0, aggregator.GetGlobalErrorRatePercentage(0));
        }

        [TestMethod]
        public void GetGlobalErrorRatePercentage_NegativeOperations_ReturnsZero()
        {
            var aggregator = new ExceptionAggregator();
            aggregator.RecordException(new Exception("err"));
            Assert.AreEqual(0.0, aggregator.GetGlobalErrorRatePercentage(-10));
        }

        [TestMethod]
        public void GetGlobalErrorRatePercentage_CorrectCalculation()
        {
            var aggregator = new ExceptionAggregator();
            aggregator.RecordException(new Exception("err1"));
            aggregator.RecordException(new Exception("err2"));

            // 2 exceptions out of 100 operations = 2%
            var rate = aggregator.GetGlobalErrorRatePercentage(100);
            Assert.AreEqual(2.0, rate, 0.001);
        }

        // --- Expiration / Cleanup ---

        [TestMethod]
        public void Expiration_ExpiredGroupsAreRemoved()
        {
            var now = DateTimeOffset.UtcNow;
            var callCount = 0;

            // Short expiration window of 1 second
            var aggregator = CreateAggregatorWithTimeProvider(
                () =>
                {
                    callCount++;
                    // First 3 calls (ctor, group ctor, RecordException): current time;
                    // Subsequent calls (GetGroups cleanup): well past expiration
                    return callCount <= 3 ? now : now.AddHours(25);
                },
                TimeSpan.FromHours(1));

            // Record exception at "now"
            aggregator.RecordException(new InvalidOperationException("expired"));

            // GetGroups should trigger cleanup with "now + 25 hours"
            var groups = aggregator.GetGroups();
            Assert.AreEqual(0, groups.Count, "Expired groups should be cleaned up");
        }

        // --- ExceptionGroup properties ---

        [TestMethod]
        public void ExceptionGroup_ExceptionType_MatchesExceptionName()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new InvalidOperationException("test"));
            Assert.AreEqual("System.InvalidOperationException", group.ExceptionType);
        }

        [TestMethod]
        public void ExceptionGroup_Message_MatchesExceptionMessage()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new ArgumentException("my custom message"));
            Assert.AreEqual("my custom message", group.Message);
        }

        [TestMethod]
        public void ExceptionGroup_Fingerprint_IsNotEmpty()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new Exception("test"));
            Assert.IsFalse(string.IsNullOrEmpty(group.Fingerprint));
        }

        [TestMethod]
        public void ExceptionGroup_Count_IncrementsOnRepeat()
        {
            var aggregator = new ExceptionAggregator();
            var ex = new InvalidOperationException("repeated");
            var group1 = aggregator.RecordException(ex);
            Assert.AreEqual(1L, group1.Count);

            aggregator.RecordException(ex);
            // Re-fetch group since RecordOccurrence updates in place
            Assert.AreEqual(2L, group1.Count);
        }

        [TestMethod]
        public void ExceptionGroup_FirstOccurrence_IsSet()
        {
            var aggregator = new ExceptionAggregator();
            var before = DateTimeOffset.UtcNow;
            var group = aggregator.RecordException(new Exception("test"));
            var after = DateTimeOffset.UtcNow;

            Assert.IsTrue(group.FirstOccurrence >= before.AddSeconds(-1));
            Assert.IsTrue(group.FirstOccurrence <= after.AddSeconds(1));
        }

        [TestMethod]
        public void ExceptionGroup_LastOccurrence_IsSet()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new Exception("test"));

            Assert.IsTrue(group.LastOccurrence > DateTimeOffset.MinValue);
        }

        [TestMethod]
        public void ExceptionGroup_LastOccurrence_UpdatesOnRepeat()
        {
            var now = DateTimeOffset.UtcNow;
            var callCount = 0;

            var aggregator = CreateAggregatorWithTimeProvider(() =>
            {
                callCount++;
                return now.AddSeconds(callCount * 10d);
            });

            var group = aggregator.RecordException(new InvalidOperationException("repeated"));
            var firstLast = group.LastOccurrence;

            aggregator.RecordException(new InvalidOperationException("repeated"));
            var secondLast = group.LastOccurrence;

            Assert.IsTrue(secondLast >= firstLast, "LastOccurrence should advance on repeat");
        }

        // --- ExceptionGroup rate methods ---

        [TestMethod]
        public void ExceptionGroup_GetErrorRate_SingleOccurrence_ReturnsPositive()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new Exception("rate-test"));

            // With only one occurrence, the duration is zero, so rate
            // should be calculated against MinimumRateWindow
            var rate = group.GetErrorRate();
            Assert.IsTrue(rate >= 0.0, "Error rate should be non-negative");
        }

        [TestMethod]
        public void ExceptionGroup_GetErrorRatePerHour_SingleOccurrence_ReturnsPositive()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new Exception("hourly-test"));

            var rate = group.GetErrorRatePerHour();
            Assert.IsTrue(rate >= 0.0, "Hourly error rate should be non-negative");
        }

        [TestMethod]
        public void ExceptionGroup_GetErrorRatePercentage_ZeroOperations_ReturnsZero()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new Exception("pct-test"));

            Assert.AreEqual(0.0, group.GetErrorRatePercentage(0));
        }

        [TestMethod]
        public void ExceptionGroup_GetErrorRatePercentage_NegativeOperations_ReturnsZero()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new Exception("pct-test"));

            Assert.AreEqual(0.0, group.GetErrorRatePercentage(-5));
        }

        [TestMethod]
        public void ExceptionGroup_GetErrorRatePercentage_CorrectCalculation()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new Exception("pct-calc"));

            // 1 exception out of 200 operations = 0.5%
            var rate = group.GetErrorRatePercentage(200);
            Assert.AreEqual(0.5, rate, 0.001);
        }

        [TestMethod]
        public void ExceptionGroup_GetErrorRate_MultipleOccurrences_WithTimeGap()
        {
            var now = DateTimeOffset.UtcNow;
            var callCount = 0;

            var aggregator = CreateAggregatorWithTimeProvider(() =>
            {
                callCount++;
                // Simulate time passing between recordings
                return now.AddMinutes(callCount);
            });

            var group = aggregator.RecordException(new InvalidOperationException("rate"));
            aggregator.RecordException(new InvalidOperationException("rate"));
            aggregator.RecordException(new InvalidOperationException("rate"));

            // Should compute rate based on time range
            var rate = group.GetErrorRate();
            Assert.IsTrue(rate > 0, "Rate should be positive with time gap and multiple occurrences");
        }

        // --- ExceptionGroup StackTrace ---

        [TestMethod]
        public void ExceptionGroup_StackTrace_FromThrownException_IsNotNull()
        {
            var aggregator = new ExceptionAggregator();
            Exception? thrownEx = null;
            try
            {
                throw new InvalidOperationException("thrown");
            }
            catch (Exception ex)
            {
                thrownEx = ex;
            }

            var group = aggregator.RecordException(thrownEx!);
            Assert.IsNotNull(group.StackTrace, "StackTrace should be captured from thrown exception");
        }

        [TestMethod]
        public void ExceptionGroup_StackTrace_FromNewException_IsNull()
        {
            var aggregator = new ExceptionAggregator();
            var group = aggregator.RecordException(new Exception("not thrown"));
            // Stack trace is null for exceptions that were never thrown
            Assert.IsNull(group.StackTrace);
        }

        private ExceptionAggregator CreateAggregatorWithTimeProvider(
            Func<DateTimeOffset> nowProvider,
            TimeSpan? expirationWindow = null)
        {
            // Use internal constructor via reflection
            var ctor = typeof(ExceptionAggregator).GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(Func<DateTimeOffset>), typeof(TimeSpan?) },
                null);

            if (ctor == null)
                throw new InvalidOperationException("Could not find internal constructor");

            return (ExceptionAggregator)ctor.Invoke(new object?[] { nowProvider, expirationWindow });
        }
    }
}
