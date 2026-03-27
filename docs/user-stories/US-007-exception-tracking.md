# US-007: Exception Tracking

**GitHub Issue**: [#9](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/9)

**Status**: ✅ Complete  
**Category**: Core Package  
**Effort**: 3 story points  
**Sprint**: 5

## Description

As a **developer monitoring application reliability**,  
I want **automatic exception tracking with fingerprinting, aggregation, and error rate calculation**,  
So that **I can quickly identify, group, and prioritize exceptions across distributed services**.

## Acceptance Criteria

1. **Exception Fingerprinting**
   - [x] Generate stable fingerprint from exception type, message pattern, and stack trace
   - [x] Strip dynamic values (IDs, timestamps, URLs) for accurate grouping
   - [x] Handle inner exceptions and aggregate exceptions
   - [x] Consistent fingerprints across processes and machines

2. **Exception Aggregation**
   - [x] Group exceptions by fingerprint
   - [x] Track first occurrence, last occurrence, and total count
   - [x] Store representative exception details
   - [x] Configurable time window for aggregation

3. **Error Rate Calculation**
   - [x] Track exceptions per minute/hour
   - [x] Calculate error rate as percentage of total operations
   - [x] Support per-operation and global error rates
   - [x] Expose metrics for monitoring

4. **Integration with Activities**
   - [x] Automatically record exceptions on current Activity
   - [x] Add exception to Activity tags
   - [x] Mark Activity as failed
   - [x] Include exception in distributed trace

5. **Public API**
   - [x] `Telemetry.RecordException(Exception)` method
   - [x] `IOperationScope.RecordException(Exception)` method
   - [x] `ExceptionFingerprinter` for custom fingerprinting
   - [x] Query API for exception statistics

## Technical Requirements

### Exception Fingerprinting

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Generates stable fingerprints for exceptions to enable grouping and aggregation.
    /// </summary>
    public static class ExceptionFingerprinter
    {
        private static readonly Regex GuidPattern = new Regex(
            @"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        private static readonly Regex NumberPattern = new Regex(
            @"\b\d{2,}\b",
            RegexOptions.Compiled);
        
        private static readonly Regex UrlPattern = new Regex(
            @"https?://[^\s]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        /// <summary>
        /// Generates a stable fingerprint for the exception.
        /// </summary>
        public static string GenerateFingerprint(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            
            var components = new List<string>
            {
                exception.GetType().FullName ?? exception.GetType().Name,
                NormalizeMessage(exception.Message),
                NormalizeStackTrace(exception.StackTrace)
            };
            
            // Include inner exception fingerprint
            if (exception.InnerException != null)
            {
                components.Add(GenerateFingerprint(exception.InnerException));
            }
            
            // Handle AggregateException specially
            if (exception is AggregateException aggEx)
            {
                foreach (var inner in aggEx.InnerExceptions.Take(3))
                {
                    components.Add(GenerateFingerprint(inner));
                }
            }
            
            var combined = string.Join("|", components);
            return ComputeHash(combined);
        }
        
        /// <summary>
        /// Normalizes exception message by removing dynamic values.
        /// </summary>
        private static string NormalizeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;
            
            // Remove GUIDs
            message = GuidPattern.Replace(message, "{guid}");
            
            // Remove numbers (likely IDs, timestamps, etc.)
            message = NumberPattern.Replace(message, "{number}");
            
            // Remove URLs
            message = UrlPattern.Replace(message, "{url}");
            
            // Normalize whitespace
            message = Regex.Replace(message, @"\s+", " ").Trim();
            
            return message;
        }
        
        /// <summary>
        /// Normalizes stack trace by keeping method signatures but removing line numbers.
        /// </summary>
        private static string NormalizeStackTrace(string? stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return string.Empty;
            
            // Take first 3 frames (most relevant)
            var frames = stackTrace
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(3);
            
            var normalized = new List<string>();
            foreach (var frame in frames)
            {
                // Remove line numbers: "at Namespace.Class.Method() in File.cs:line 42"
                var cleaned = Regex.Replace(frame, @" in .+:line \d+", string.Empty);
                
                // Remove assembly info
                cleaned = Regex.Replace(cleaned, @"\[.+?\]", string.Empty);
                
                normalized.Add(cleaned.Trim());
            }
            
            return string.Join("|", normalized);
        }
        
        /// <summary>
        /// Computes SHA256 hash of the input string.
        /// </summary>
        private static string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
```

### Exception Aggregation

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Aggregates exceptions by fingerprint for efficient storage and analysis.
    /// </summary>
    public sealed class ExceptionAggregator
    {
        private readonly ConcurrentDictionary<string, ExceptionGroup> _groups = 
            new ConcurrentDictionary<string, ExceptionGroup>();
        
        private readonly TimeSpan _expirationWindow;
        
        public ExceptionAggregator(TimeSpan? expirationWindow = null)
        {
            _expirationWindow = expirationWindow ?? TimeSpan.FromHours(24);
        }
        
        /// <summary>
        /// Records an exception and returns its group.
        /// </summary>
        public ExceptionGroup RecordException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            
            var fingerprint = ExceptionFingerprinter.GenerateFingerprint(exception);
            
            var group = _groups.AddOrUpdate(
                fingerprint,
                key => new ExceptionGroup(fingerprint, exception),
                (key, existing) => 
                {
                    existing.RecordOccurrence(exception);
                    return existing;
                });
            
            return group;
        }
        
        /// <summary>
        /// Gets all active exception groups.
        /// </summary>
        public IReadOnlyCollection<ExceptionGroup> GetGroups()
        {
            CleanupExpiredGroups();
            return _groups.Values.ToList();
        }
        
        /// <summary>
        /// Gets exception group by fingerprint.
        /// </summary>
        public ExceptionGroup? GetGroup(string fingerprint)
        {
            _groups.TryGetValue(fingerprint, out var group);
            return group;
        }
        
        /// <summary>
        /// Removes expired exception groups.
        /// </summary>
        private void CleanupExpiredGroups()
        {
            var now = DateTimeOffset.UtcNow;
            var expired = _groups
                .Where(kvp => now - kvp.Value.LastOccurrence > _expirationWindow)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in expired)
            {
                _groups.TryRemove(key, out _);
            }
        }
    }
    
    /// <summary>
    /// Represents a group of exceptions with the same fingerprint.
    /// </summary>
    public sealed class ExceptionGroup
    {
        private long _count;
        
        public string Fingerprint { get; }
        public string ExceptionType { get; }
        public string Message { get; }
        public string? StackTrace { get; }
        public DateTimeOffset FirstOccurrence { get; }
        public DateTimeOffset LastOccurrence { get; private set; }
        public long Count => _count;
        
        internal ExceptionGroup(string fingerprint, Exception exception)
        {
            Fingerprint = fingerprint;
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name;
            Message = exception.Message;
            StackTrace = exception.StackTrace;
            FirstOccurrence = DateTimeOffset.UtcNow;
            LastOccurrence = FirstOccurrence;
            _count = 1;
        }
        
        internal void RecordOccurrence(Exception exception)
        {
            System.Threading.Interlocked.Increment(ref _count);
            LastOccurrence = DateTimeOffset.UtcNow;
        }
        
        /// <summary>
        /// Gets the error rate (occurrences per minute).
        /// </summary>
        public double GetErrorRate()
        {
            var duration = LastOccurrence - FirstOccurrence;
            if (duration.TotalMinutes < 0.01)
                return Count;
            
            return Count / duration.TotalMinutes;
        }
    }
}
```

### Integration with Telemetry

```csharp
using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Extension methods for exception tracking.
    /// </summary>
    public static class TelemetryExceptionExtensions
    {
        private static readonly ExceptionAggregator _aggregator = new ExceptionAggregator();
        
        /// <summary>
        /// Records an exception with the telemetry system.
        /// </summary>
        public static void RecordException(this Exception exception)
        {
            if (exception == null)
                return;
            
            // Aggregate exception
            var group = _aggregator.RecordException(exception);
            
            // Add to current Activity if available
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.AddTag("exception.type", exception.GetType().FullName);
                activity.AddTag("exception.message", exception.Message);
                activity.AddTag("exception.fingerprint", group.Fingerprint);
                
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    activity.AddTag("exception.stacktrace", exception.StackTrace);
                }
                
                // Record as Activity event
                var tags = new ActivityTagsCollection
                {
                    { "exception.type", exception.GetType().FullName },
                    { "exception.message", exception.Message },
                    { "exception.fingerprint", group.Fingerprint }
                };
                activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));
            }
        }
        
        /// <summary>
        /// Gets the exception aggregator for querying statistics.
        /// </summary>
        public static ExceptionAggregator GetAggregator() => _aggregator;
    }
}
```

### Operation Scope Integration

```csharp
public interface IOperationScope : IDisposable
{
    /// <summary>
    /// Records an exception that occurred during this operation.
    /// </summary>
    void RecordException(Exception exception);
    
    // ... other members
}

internal class OperationScope : IOperationScope
{
    public void RecordException(Exception exception)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        
        // Record exception
        exception.RecordException();
        
        // Mark operation as failed
        _succeeded = false;
    }
}
```

### Error Rate Metrics

```csharp
using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Exposes exception metrics for monitoring.
    /// </summary>
    public static class ExceptionMetrics
    {
        private static readonly ICounter<long> _exceptionCounter;
        private static readonly IHistogram<long> _exceptionRateHistogram;
        
        static ExceptionMetrics()
        {
            var recorder = MetricRecorderFactory.Instance;
            
            _exceptionCounter = recorder.CreateCounter(
                "exceptions.total",
                "exceptions",
                "Total number of exceptions");
            
            _exceptionRateHistogram = recorder.CreateHistogram(
                "exceptions.rate",
                "exceptions/min",
                "Exception rate per minute");
        }
        
        /// <summary>
        /// Records an exception occurrence.
        /// </summary>
        public static void RecordException(string exceptionType, string fingerprint)
        {
            _exceptionCounter.Add(1,
                new MetricTag("type", exceptionType),
                new MetricTag("fingerprint", fingerprint));
        }
        
        /// <summary>
        /// Records error rate for monitoring.
        /// </summary>
        public static void RecordErrorRate(double exceptionsPerMinute)
        {
            _exceptionRateHistogram.Record((long)exceptionsPerMinute);
        }
    }
}
```

## Testing Requirements

### Unit Tests

1. **Fingerprinting Tests**
   ```csharp
   [Fact]
   public void ExceptionFingerprinter_GeneratesConsistentFingerprints()
   {
       var ex1 = new InvalidOperationException("Failed to process user 12345");
       var ex2 = new InvalidOperationException("Failed to process user 67890");
       
       var fp1 = ExceptionFingerprinter.GenerateFingerprint(ex1);
       var fp2 = ExceptionFingerprinter.GenerateFingerprint(ex2);
       
       // Should be same after normalization (numbers removed)
       Assert.Equal(fp1, fp2);
   }
   
   [Fact]
   public void ExceptionFingerprinter_HandlesDifferentExceptionTypes()
   {
       var ex1 = new InvalidOperationException("Error");
       var ex2 = new ArgumentException("Error");
       
       var fp1 = ExceptionFingerprinter.GenerateFingerprint(ex1);
       var fp2 = ExceptionFingerprinter.GenerateFingerprint(ex2);
       
       // Different types should have different fingerprints
       Assert.NotEqual(fp1, fp2);
   }
   
   [Fact]
   public void ExceptionFingerprinter_HandlesInnerExceptions()
   {
       var inner = new InvalidOperationException("Inner error");
       var outer = new Exception("Outer error", inner);
       
       var fingerprint = ExceptionFingerprinter.GenerateFingerprint(outer);
       
       Assert.NotNull(fingerprint);
       Assert.NotEmpty(fingerprint);
   }
   
   [Fact]
   public void ExceptionFingerprinter_HandlesAggregateException()
   {
       var ex1 = new InvalidOperationException("Error 1");
       var ex2 = new ArgumentException("Error 2");
       var aggEx = new AggregateException(ex1, ex2);
       
       var fingerprint = ExceptionFingerprinter.GenerateFingerprint(aggEx);
       
       Assert.NotNull(fingerprint);
       Assert.NotEmpty(fingerprint);
   }
   ```

2. **Aggregation Tests**
   ```csharp
   [Fact]
   public void ExceptionAggregator_GroupsSimilarExceptions()
   {
       var aggregator = new ExceptionAggregator();
       
       var ex1 = new InvalidOperationException("User 123 not found");
       var ex2 = new InvalidOperationException("User 456 not found");
       
       var group1 = aggregator.RecordException(ex1);
       var group2 = aggregator.RecordException(ex2);
       
       // Should be same group (numbers normalized)
       Assert.Equal(group1.Fingerprint, group2.Fingerprint);
       Assert.Equal(2, group1.Count);
   }
   
   [Fact]
   public void ExceptionGroup_TracksOccurrences()
   {
       var aggregator = new ExceptionAggregator();
       var exception = new InvalidOperationException("Test error");
       
       var group = aggregator.RecordException(exception);
       Assert.Equal(1, group.Count);
       
       aggregator.RecordException(exception);
       Assert.Equal(2, group.Count);
       
       aggregator.RecordException(exception);
       Assert.Equal(3, group.Count);
   }
   
   [Fact]
   public void ExceptionGroup_CalculatesErrorRate()
   {
       var aggregator = new ExceptionAggregator();
       var exception = new InvalidOperationException("Test");
       
       var group = aggregator.RecordException(exception);
       Thread.Sleep(1000); // Wait 1 second
       
       aggregator.RecordException(exception);
       aggregator.RecordException(exception);
       
       var errorRate = group.GetErrorRate();
       
       // Should be approximately 120 errors/min (2 errors in 1 second)
       Assert.InRange(errorRate, 60, 180);
   }
   ```

3. **Activity Integration Tests**
   ```csharp
   [Fact]
   public void RecordException_AddsToActivity()
   {
       var activitySource = new ActivitySource("Test");
       using var activity = activitySource.StartActivity("TestOp");
       
       var exception = new InvalidOperationException("Test error");
       exception.RecordException();
       
       Assert.Contains(activity!.Tags, t => t.Key == "exception.type");
       Assert.Contains(activity!.Tags, t => t.Key == "exception.message");
       Assert.Contains(activity!.Tags, t => t.Key == "exception.fingerprint");
       Assert.Equal(ActivityStatusCode.Error, activity.Status);
   }
   ```

### Integration Tests

1. **End-to-End Exception Tracking**
   - [ ] Exception recorded in operation scope
   - [ ] Exception fingerprint generated correctly
   - [ ] Exception aggregated with similar exceptions
   - [ ] Activity marked as failed
   - [ ] Metrics updated

2. **Performance Tests**
   ```csharp
   [Benchmark]
   public void GenerateFingerprint()
   {
       ExceptionFingerprinter.GenerateFingerprint(_testException);
   }
   
   [Benchmark]
   public void RecordException()
   {
       _aggregator.RecordException(_testException);
   }
   ```

## Performance Requirements

- **Fingerprint generation**: <10μs per exception
- **Exception recording**: <50μs
- **Aggregation lookup**: <100ns (dictionary lookup)
- **Memory per exception group**: <2KB
- **Maximum concurrent exception groups**: 10,000 (configurable)

## Dependencies

**Blocked By**: 
- US-001 (Core Package Setup)
- US-002 (Auto-Managed Correlation)
- US-006 (Runtime-Adaptive Metrics)

**Blocks**: 
- US-012 (Operation Scope) - uses exception recording
- US-016 (Statistics & Health Checks) - exposes exception statistics

## Definition of Done

- [x] `ExceptionFingerprinter` class implemented and tested
- [x] `ExceptionAggregator` class implemented
- [x] `ExceptionGroup` class tracks statistics
- [x] Integration with Activity API complete
- [x] Extension methods on Exception class
- [x] All unit tests passing (>95% coverage)
- [x] Performance benchmarks meet requirements
- [x] Tested on .NET Framework 4.8 and .NET 8
- [x] XML documentation complete
- [x] Code reviewed and approved
- [x] Zero warnings in build

## Notes

### Design Decisions

1. **Why SHA256 for fingerprinting?**
   - Cryptographically strong hash minimizes collisions
   - Fixed length output (64 hex chars)
   - Available in .NET Standard 2.0

2. **Why normalize exception messages?**
   - Dynamic values (IDs, timestamps) prevent proper grouping
   - Similar errors should be grouped together
   - Reduces cardinality for better aggregation

3. **Why limit to first 3 stack frames?**
   - Most relevant information is at the top
   - Reduces fingerprint size
   - Prevents deep stack variations from creating different fingerprints

4. **Why 24-hour expiration window?**
   - Balances memory usage with historical data
   - Long enough for daily patterns
   - Prevents unbounded memory growth

### Implementation Tips

- Use `ConcurrentDictionary` for thread-safe aggregation
- Consider LRU eviction for memory-constrained environments
- Add diagnostic logging for fingerprint generation (debug builds)
- Pre-compile regex patterns for performance

### Common Pitfalls

- Don't include PII in exception fingerprints
- Be careful with inner exception chains (can be deep)
- AggregateException can contain many inner exceptions (limit to first few)
- Stack traces may vary across platforms (normalize carefully)

### Future Enhancements

- Add support for custom fingerprinting strategies
- Implement rate limiting for high-frequency exceptions
- Add exception sampling for high-volume scenarios
- Support exception screenshots/context capture

## Related Documentation

- [Project Plan](../project-plan.md#7-exception-tracking-with-fingerprinting-and-aggregation)
- [Activity Status Code](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitystatuscode)
- [Exception Handling Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)

## Implementation Summary

**Completed**: 2026-02-08  
**Implemented by**: GitHub Copilot (Cloud Agent)  
**PR**: [#44](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/pull/44)

### What Was Implemented

- **ExceptionFingerprinter**: Generates stable SHA256 fingerprints from exception type, normalized message, and top 3 stack frames
  - Strips dynamic values (GUIDs, numbers, URLs) for accurate grouping across instances
  - Handles inner exceptions and AggregateException (first 3 inner exceptions)
  - Consistent fingerprints across processes and machines

- **ExceptionAggregator**: Thread-safe exception grouping by fingerprint
  - Tracks first occurrence, last occurrence, and total count per fingerprint
  - Configurable expiration window (default 24 hours)
  - Automatic cleanup of expired groups
  - Query API for exception statistics

- **ExceptionGroup**: Statistics tracking for aggregated exceptions
  - Stores representative exception details (type, message, stack trace)
  - Calculates error rate (occurrences per minute)
  - Thread-safe counter using Interlocked operations

- **TelemetryExceptionExtensions**: Public API for exception recording
  - `Exception.RecordException()` extension method
  - Automatic Activity integration (tags, events, status)
  - Global aggregator access via `GetAggregator()`

- **ExceptionMetrics**: Metrics integration
  - Counter: `exceptions.total` with type/fingerprint tags
  - Histogram: `exceptions.rate` for error rate tracking
  - Uses runtime-adaptive MetricRecorder from US-006

- **ExceptionTrackingOptions**: Configuration
  - Configurable detail capture (Full, MessageOnly, MinimalWithFingerprint)
  - Options for PII reduction and payload size optimization

### Key Files

- `src/HVO.Enterprise.Telemetry/Exceptions/ExceptionFingerprinter.cs` (117 lines)
- `src/HVO.Enterprise.Telemetry/Exceptions/ExceptionAggregator.cs` (91 lines)
- `src/HVO.Enterprise.Telemetry/Exceptions/ExceptionGroup.cs` (83 lines)
- `src/HVO.Enterprise.Telemetry/Exceptions/TelemetryExceptionExtensions.cs` (129 lines)
- `src/HVO.Enterprise.Telemetry/Exceptions/ExceptionMetrics.cs` (77 lines)
- `src/HVO.Enterprise.Telemetry/Exceptions/ExceptionTrackingOptions.cs` (40 lines)
- `tests/HVO.Enterprise.Telemetry.Tests/Exceptions/ExceptionFingerprinterTests.cs`
- `tests/HVO.Enterprise.Telemetry.Tests/Exceptions/ExceptionAggregatorTests.cs`
- `tests/HVO.Enterprise.Telemetry.Tests/Exceptions/ExceptionMetricsTests.cs`
- `tests/HVO.Enterprise.Telemetry.Tests/Exceptions/TelemetryExceptionExtensionsTests.cs`

Total implementation: ~701 lines across 6 source files + 4 test files

### Decisions Made

1. **SHA256 for fingerprinting**: Cryptographically strong, fixed-length output, minimal collisions
2. **Regex-based normalization**: Removes GUIDs, numbers (2+ digits), URLs for consistent grouping
3. **Top 3 stack frames only**: Most relevant info at top, reduces fingerprint variability
4. **24-hour expiration**: Balances memory usage with historical data, prevents unbounded growth
5. **Concurrent dictionary**: Thread-safe aggregation without locks for high-throughput scenarios
6. **Configurable detail capture**: Allows PII reduction and payload size optimization in production

### Quality Gates

- ✅ Build: 0 warnings, 0 errors
- ✅ Tests: All exception tracking tests passing
- ✅ Code Review: PR #44 reviewed and merged
- ✅ Activity Integration: Exception events and tags properly set
- ✅ Metrics Integration: Counters and histograms exposed via runtime-adaptive MetricRecorder

### Example Usage

```csharp
using HVO.Enterprise.Telemetry.Exceptions;

try
{
    DoWork();
}
catch (Exception ex)
{
    // Record exception with automatic Activity tagging
    ex.RecordException();
    
    // Query aggregated statistics
    var fingerprint = ExceptionFingerprinter.GenerateFingerprint(ex);
    var group = TelemetryExceptionExtensions.GetAggregator().GetGroup(fingerprint);
    
    if (group != null)
    {
        Console.WriteLine($"This error has occurred {group.Count} times");
        Console.WriteLine($"Error rate: {group.GetErrorRate():F2} per minute");
    }
}
```

### Next Steps

This story unblocks:
- US-012 (Operation Scope) - can now use RecordException() in IOperationScope
- US-016 (Statistics & Health Checks) - can expose exception statistics via health check endpoints
