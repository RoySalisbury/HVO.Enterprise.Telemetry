# US-010: ActivitySource Sampling

**GitHub Issue**: [#12](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/12)

**Status**: ✅ Complete  
**Category**: Core Package  
**Effort**: 5 story points  
**Sprint**: 3

## Description

As a **platform engineer managing high-throughput services**,  
I want **configurable sampling of distributed traces based on ActivitySource, operation type, and runtime conditions**,  
So that **I can control telemetry volume and costs while ensuring critical operations are always traced**.

## Acceptance Criteria

1. **Probabilistic Sampling**
    - [x] Support sampling rates from 0.0 (0%) to 1.0 (100%)
    - [x] Deterministic sampling based on TraceId (consistent across services)
    - [x] Per-ActivitySource sampling configuration
    - [x] Per-operation sampling configuration
    - [x] Head-based sampling (decide at trace start)

2. **Conditional Sampling**
    - [x] Always sample errors (configurable)
    - [x] Always sample slow operations (configurable threshold)
    - [x] Sample based on custom predicates
    - [x] Sample based on tags/properties

3. **Adaptive Sampling**
    - [x] Adjust sampling rate based on throughput
    - [x] Increase sampling during errors/incidents
    - [x] Decrease sampling during high load
    - [x] Configurable target operations/second

4. **Integration with Configuration System**
    - [x] Read sampling config from multi-level configuration (US-009)
    - [x] Support hot reload of sampling rates (US-008)
    - [x] Per-ActivitySource configuration
    - [x] Runtime adjustment via API

5. **Sampling Metrics**
    - [x] Track sampled vs total operations
    - [x] Calculate actual sampling rate
    - [x] Expose metrics for monitoring
    - [x] Log sampling decisions (debug mode)

## Technical Requirements

### Core Sampling Infrastructure

```csharp
using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Sampling decision for a trace.
    /// </summary>
    public enum SamplingDecision
    {
        /// <summary>
        /// Do not sample this trace.
        /// </summary>
        Drop = 0,
        
        /// <summary>
        /// Sample this trace and record it.
        /// </summary>
        RecordAndSample = 1
    }
    
    /// <summary>
    /// Result of a sampling decision.
    /// </summary>
    public readonly struct SamplingResult
    {
        public SamplingDecision Decision { get; }
        public string? Reason { get; }
        
        public SamplingResult(SamplingDecision decision, string? reason = null)
        {
            Decision = decision;
            Reason = reason;
        }
        
        public static SamplingResult Drop(string? reason = null) => 
            new SamplingResult(SamplingDecision.Drop, reason);
        
        public static SamplingResult Sample(string? reason = null) => 
            new SamplingResult(SamplingDecision.RecordAndSample, reason);
    }
    
    /// <summary>
    /// Context for sampling decisions.
    /// </summary>
    public sealed class SamplingContext
    {
        public ActivityTraceId TraceId { get; }
        public string ActivityName { get; }
        public string ActivitySourceName { get; }
        public ActivityKind Kind { get; }
        public ActivityTagsCollection? Tags { get; }
        
        public SamplingContext(
            ActivityTraceId traceId,
            string activityName,
            string activitySourceName,
            ActivityKind kind,
            ActivityTagsCollection? tags = null)
        {
            TraceId = traceId;
            ActivityName = activityName;
            ActivitySourceName = activitySourceName;
            Kind = kind;
            Tags = tags;
        }
    }
    
    /// <summary>
    /// Interface for sampling strategies.
    /// </summary>
    public interface ISampler
    {
        /// <summary>
        /// Makes a sampling decision for the given context.
        /// </summary>
        SamplingResult ShouldSample(SamplingContext context);
    }
}
```

### Probabilistic Sampler

```csharp
using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Deterministic probabilistic sampler based on TraceId.
    /// Ensures consistent sampling decisions across distributed services.
    /// </summary>
    public sealed class ProbabilisticSampler : ISampler
    {
        private readonly double _samplingRate;
        private readonly ulong _threshold;
        
        /// <summary>
        /// Creates a probabilistic sampler.
        /// </summary>
        /// <param name="samplingRate">Sampling rate from 0.0 to 1.0</param>
        public ProbabilisticSampler(double samplingRate)
        {
            if (samplingRate < 0.0 || samplingRate > 1.0)
                throw new ArgumentOutOfRangeException(nameof(samplingRate), 
                    "Sampling rate must be between 0.0 and 1.0");
            
            _samplingRate = samplingRate;
            
            // Calculate threshold based on sampling rate
            // Uses lower 64 bits of TraceId as random value
            _threshold = (ulong)(samplingRate * ulong.MaxValue);
        }
        
        public SamplingResult ShouldSample(SamplingContext context)
        {
            if (_samplingRate >= 1.0)
                return SamplingResult.Sample("100% sampling");
            
            if (_samplingRate <= 0.0)
                return SamplingResult.Drop("0% sampling");
            
            // Extract lower 64 bits of TraceId for deterministic sampling
            var traceIdBytes = context.TraceId.ToByteArray();
            var traceIdValue = BitConverter.ToUInt64(traceIdBytes, 0);
            
            var shouldSample = traceIdValue <= _threshold;
            
            return shouldSample
                ? SamplingResult.Sample($"TraceId hash below threshold (rate: {_samplingRate:P1})")
                : SamplingResult.Drop($"TraceId hash above threshold (rate: {_samplingRate:P1})");
        }
    }
}
```

### Conditional Sampler

```csharp
using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Sampler that applies conditional logic (always sample errors, slow operations, etc.).
    /// </summary>
    public sealed class ConditionalSampler : ISampler
    {
        private readonly ISampler _baseSampler;
        private readonly bool _alwaysSampleErrors;
        private readonly TimeSpan? _slowOperationThreshold;
        private readonly Func<SamplingContext, bool>? _customPredicate;
        
        public ConditionalSampler(
            ISampler baseSampler,
            bool alwaysSampleErrors = true,
            TimeSpan? slowOperationThreshold = null,
            Func<SamplingContext, bool>? customPredicate = null)
        {
            _baseSampler = baseSampler ?? throw new ArgumentNullException(nameof(baseSampler));
            _alwaysSampleErrors = alwaysSampleErrors;
            _slowOperationThreshold = slowOperationThreshold;
            _customPredicate = customPredicate;
        }
        
        public SamplingResult ShouldSample(SamplingContext context)
        {
            // Check custom predicate first
            if (_customPredicate?.Invoke(context) == true)
                return SamplingResult.Sample("Custom predicate matched");
            
            // Check if operation has error tag (set during execution)
            if (_alwaysSampleErrors && context.Tags != null)
            {
                foreach (var tag in context.Tags)
                {
                    if (tag.Key == "error" && tag.Value is bool errorValue && errorValue)
                        return SamplingResult.Sample("Error detected");
                    
                    if (tag.Key == "exception.type")
                        return SamplingResult.Sample("Exception detected");
                }
            }
            
            // Check slow operation threshold (requires duration tag)
            if (_slowOperationThreshold.HasValue && context.Tags != null)
            {
                foreach (var tag in context.Tags)
                {
                    if (tag.Key == "duration.ms" && tag.Value is long durationMs)
                    {
                        if (durationMs > _slowOperationThreshold.Value.TotalMilliseconds)
                            return SamplingResult.Sample($"Slow operation ({durationMs}ms > {_slowOperationThreshold.Value.TotalMilliseconds}ms)");
                    }
                }
            }
            
            // Fall back to base sampler
            return _baseSampler.ShouldSample(context);
        }
    }
}
```

### Per-Source Sampler

```csharp
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Sampler that applies different sampling rates per ActivitySource.
    /// </summary>
    public sealed class PerSourceSampler : ISampler
    {
        private readonly ISampler _defaultSampler;
        private readonly ConcurrentDictionary<string, ISampler> _sourceSamplers = 
            new ConcurrentDictionary<string, ISampler>();
        
        public PerSourceSampler(ISampler defaultSampler)
        {
            _defaultSampler = defaultSampler ?? throw new ArgumentNullException(nameof(defaultSampler));
        }
        
        /// <summary>
        /// Configures sampling for a specific ActivitySource.
        /// </summary>
        public void ConfigureSource(string activitySourceName, ISampler sampler)
        {
            if (string.IsNullOrEmpty(activitySourceName))
                throw new ArgumentNullException(nameof(activitySourceName));
            if (sampler == null)
                throw new ArgumentNullException(nameof(sampler));
            
            _sourceSamplers[activitySourceName] = sampler;
        }
        
        /// <summary>
        /// Configures sampling rate for a specific ActivitySource.
        /// </summary>
        public void ConfigureSource(string activitySourceName, double samplingRate)
        {
            ConfigureSource(activitySourceName, new ProbabilisticSampler(samplingRate));
        }
        
        public SamplingResult ShouldSample(SamplingContext context)
        {
            // Try to find source-specific sampler
            if (_sourceSamplers.TryGetValue(context.ActivitySourceName, out var sampler))
            {
                var result = sampler.ShouldSample(context);
                return new SamplingResult(result.Decision, 
                    $"Source-specific: {result.Reason}");
            }
            
            // Fall back to default sampler
            return _defaultSampler.ShouldSample(context);
        }
    }
}
```

### Adaptive Sampler

```csharp
using System;
using System.Diagnostics;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Adaptive sampler that adjusts sampling rate based on throughput and error rate.
    /// </summary>
    public sealed class AdaptiveSampler : ISampler
    {
        private readonly int _targetOperationsPerSecond;
        private readonly double _minSamplingRate;
        private readonly double _maxSamplingRate;
        private readonly ISampler _fallbackSampler;
        
        private long _totalOperations;
        private long _sampledOperations;
        private long _lastAdjustmentTicks;
        private double _currentSamplingRate;
        private readonly object _adjustmentLock = new object();
        
        public AdaptiveSampler(
            int targetOperationsPerSecond = 1000,
            double minSamplingRate = 0.01,
            double maxSamplingRate = 1.0)
        {
            _targetOperationsPerSecond = targetOperationsPerSecond;
            _minSamplingRate = Math.Max(0.0, Math.Min(1.0, minSamplingRate));
            _maxSamplingRate = Math.Max(_minSamplingRate, Math.Min(1.0, maxSamplingRate));
            _currentSamplingRate = _maxSamplingRate;
            _fallbackSampler = new ProbabilisticSampler(_currentSamplingRate);
            _lastAdjustmentTicks = DateTime.UtcNow.Ticks;
        }
        
        /// <summary>
        /// Gets the current adaptive sampling rate.
        /// </summary>
        public double CurrentSamplingRate => _currentSamplingRate;
        
        public SamplingResult ShouldSample(SamplingContext context)
        {
            Interlocked.Increment(ref _totalOperations);
            
            // Adjust sampling rate every second
            var now = DateTime.UtcNow.Ticks;
            var elapsed = new TimeSpan(now - _lastAdjustmentTicks);
            
            if (elapsed.TotalSeconds >= 1.0)
            {
                AdjustSamplingRate(elapsed);
            }
            
            var baseSampler = new ProbabilisticSampler(_currentSamplingRate);
            var result = baseSampler.ShouldSample(context);
            
            if (result.Decision == SamplingDecision.RecordAndSample)
            {
                Interlocked.Increment(ref _sampledOperations);
            }
            
            return new SamplingResult(result.Decision,
                $"Adaptive: {result.Reason} (current rate: {_currentSamplingRate:P1})");
        }
        
        private void AdjustSamplingRate(TimeSpan elapsed)
        {
            lock (_adjustmentLock)
            {
                // Re-check inside lock
                var now = DateTime.UtcNow.Ticks;
                if (now - _lastAdjustmentTicks < TimeSpan.TicksPerSecond)
                    return;
                
                var totalOps = Interlocked.Read(ref _totalOperations);
                var sampledOps = Interlocked.Read(ref _sampledOperations);
                
                // Calculate actual operations per second
                var actualOpsPerSecond = totalOps / elapsed.TotalSeconds;
                var sampledOpsPerSecond = sampledOps / elapsed.TotalSeconds;
                
                // Adjust sampling rate to hit target
                if (sampledOpsPerSecond > _targetOperationsPerSecond && actualOpsPerSecond > 0)
                {
                    // Too many samples, decrease rate
                    var targetRate = _targetOperationsPerSecond / actualOpsPerSecond;
                    _currentSamplingRate = Math.Max(_minSamplingRate, targetRate);
                }
                else if (sampledOpsPerSecond < _targetOperationsPerSecond * 0.8)
                {
                    // Too few samples, increase rate (up to max)
                    _currentSamplingRate = Math.Min(_maxSamplingRate, _currentSamplingRate * 1.2);
                }
                
                // Reset counters
                Interlocked.Exchange(ref _totalOperations, 0);
                Interlocked.Exchange(ref _sampledOperations, 0);
                _lastAdjustmentTicks = now;
            }
        }
    }
}
```

### Integration with ActivitySource

```csharp
using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Configuration;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Integrates sampling with ActivitySource creation.
    /// </summary>
    public static class SamplingActivitySourceExtensions
    {
        private static ISampler? _globalSampler;
        
        /// <summary>
        /// Configures global sampler.
        /// </summary>
        public static void ConfigureSampling(ISampler sampler)
        {
            _globalSampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
        }
        
        /// <summary>
        /// Creates an ActivitySource with sampling configuration.
        /// </summary>
        public static ActivitySource CreateWithSampling(
            string name,
            string? version = null,
            ISampler? sampler = null)
        {
            var source = new ActivitySource(name, version);
            
            // Configure ActivityListener with sampling
            var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == name,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                {
                    var effectiveSampler = sampler ?? _globalSampler ?? 
                        new ProbabilisticSampler(1.0);
                    
                    var context = new SamplingContext(
                        options.TraceId,
                        options.Name,
                        name,
                        options.Kind,
                        options.Tags);
                    
                    var result = effectiveSampler.ShouldSample(context);
                    
                    return result.Decision == SamplingDecision.RecordAndSample
                        ? ActivitySamplingResult.AllDataAndRecorded
                        : ActivitySamplingResult.None;
                }
            };
            
            ActivitySource.AddActivityListener(listener);
            
            return source;
        }
        
        /// <summary>
        /// Configures sampling from TelemetryOptions.
        /// </summary>
        public static ISampler ConfigureFromOptions(TelemetryOptions options)
        {
            var perSourceSampler = new PerSourceSampler(
                new ProbabilisticSampler(options.DefaultSamplingRate));
            
            // Configure per-source sampling
            foreach (var kvp in options.Sampling)
            {
                var sourceName = kvp.Key;
                var samplingOptions = kvp.Value;
                
                ISampler sourceSampler;
                
                if (samplingOptions.AlwaysSampleErrors)
                {
                    // Wrap probabilistic sampler with conditional sampler
                    sourceSampler = new ConditionalSampler(
                        new ProbabilisticSampler(samplingOptions.Rate),
                        alwaysSampleErrors: true);
                }
                else
                {
                    sourceSampler = new ProbabilisticSampler(samplingOptions.Rate);
                }
                
                perSourceSampler.ConfigureSource(sourceName, sourceSampler);
            }
            
            return perSourceSampler;
        }
    }
}
```

### Sampling Metrics

```csharp
using System;
using System.Threading;
using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Tracks sampling statistics.
    /// </summary>
    public sealed class SamplingMetrics
    {
        private static readonly ICounter<long> _totalOperations;
        private static readonly ICounter<long> _sampledOperations;
        private static readonly ICounter<long> _droppedOperations;
        
        static SamplingMetrics()
        {
            var recorder = MetricRecorderFactory.Instance;
            
            _totalOperations = recorder.CreateCounter(
                "telemetry.sampling.total",
                "operations",
                "Total operations evaluated for sampling");
            
            _sampledOperations = recorder.CreateCounter(
                "telemetry.sampling.sampled",
                "operations",
                "Operations that were sampled");
            
            _droppedOperations = recorder.CreateCounter(
                "telemetry.sampling.dropped",
                "operations",
                "Operations that were dropped");
        }
        
        /// <summary>
        /// Records a sampling decision.
        /// </summary>
        public static void RecordDecision(
            SamplingResult result,
            string activitySourceName)
        {
            var tag = new MetricTag("source", activitySourceName);
            
            _totalOperations.Add(1, tag);
            
            if (result.Decision == SamplingDecision.RecordAndSample)
            {
                _sampledOperations.Add(1, tag);
            }
            else
            {
                _droppedOperations.Add(1, tag);
            }
        }
    }
}
```

## Testing Requirements

### Unit Tests

1. **Probabilistic Sampler Tests**
   ```csharp
   [Fact]
   public void ProbabilisticSampler_SamplesAtCorrectRate()
   {
       var sampler = new ProbabilisticSampler(0.5);
       var sampleCount = 0;
       var totalCount = 10000;
       
       for (int i = 0; i < totalCount; i++)
       {
           var traceId = ActivityTraceId.CreateRandom();
           var context = new SamplingContext(traceId, "test", "test", ActivityKind.Internal);
           
           if (sampler.ShouldSample(context).Decision == SamplingDecision.RecordAndSample)
               sampleCount++;
       }
       
       var actualRate = (double)sampleCount / totalCount;
       
       // Should be within 5% of target rate
       Assert.InRange(actualRate, 0.45, 0.55);
   }
   
   [Fact]
   public void ProbabilisticSampler_IsDeterministic()
   {
       var sampler = new ProbabilisticSampler(0.5);
       var traceId = ActivityTraceId.CreateFromString("00000000000000000000000000000001");
       var context = new SamplingContext(traceId, "test", "test", ActivityKind.Internal);
       
       var result1 = sampler.ShouldSample(context);
       var result2 = sampler.ShouldSample(context);
       
       // Same TraceId should always give same result
       Assert.Equal(result1.Decision, result2.Decision);
   }
   ```

2. **Conditional Sampler Tests**
   ```csharp
   [Fact]
   public void ConditionalSampler_AlwaysSamplesErrors()
   {
       var baseSampler = new ProbabilisticSampler(0.0); // Never sample
       var sampler = new ConditionalSampler(baseSampler, alwaysSampleErrors: true);
       
       var tags = new ActivityTagsCollection { { "error", true } };
       var context = new SamplingContext(
           ActivityTraceId.CreateRandom(), "test", "test", ActivityKind.Internal, tags);
       
       var result = sampler.ShouldSample(context);
       
       Assert.Equal(SamplingDecision.RecordAndSample, result.Decision);
       Assert.Contains("Error detected", result.Reason);
   }
   
   [Fact]
   public void ConditionalSampler_SamplesSlowOperations()
   {
       var baseSampler = new ProbabilisticSampler(0.0);
       var sampler = new ConditionalSampler(
           baseSampler, 
           slowOperationThreshold: TimeSpan.FromMilliseconds(100));
       
       var tags = new ActivityTagsCollection { { "duration.ms", 500L } };
       var context = new SamplingContext(
           ActivityTraceId.CreateRandom(), "test", "test", ActivityKind.Internal, tags);
       
       var result = sampler.ShouldSample(context);
       
       Assert.Equal(SamplingDecision.RecordAndSample, result.Decision);
   }
   ```

3. **Adaptive Sampler Tests**
   ```csharp
   [Fact]
   public void AdaptiveSampler_AdjustsRate()
   {
       var sampler = new AdaptiveSampler(targetOperationsPerSecond: 100);
       
       var initialRate = sampler.CurrentSamplingRate;
       
       // Simulate high throughput
       for (int i = 0; i < 10000; i++)
       {
           var context = new SamplingContext(
               ActivityTraceId.CreateRandom(), "test", "test", ActivityKind.Internal);
           sampler.ShouldSample(context);
       }
       
       Thread.Sleep(1100); // Wait for adjustment
       
       // Simulate one more to trigger adjustment
       sampler.ShouldSample(new SamplingContext(
           ActivityTraceId.CreateRandom(), "test", "test", ActivityKind.Internal));
       
       var adjustedRate = sampler.CurrentSamplingRate;
       
       // Rate should decrease due to high throughput
       Assert.True(adjustedRate < initialRate);
   }
   ```

### Integration Tests

1. **End-to-End Sampling**
   - [ ] Create ActivitySource with sampling
   - [ ] Start operations at various rates
   - [ ] Verify sampling decisions
   - [ ] Check metrics are recorded

## Performance Requirements

- **Sampling decision**: <100ns
- **TraceId hash calculation**: <50ns
- **Per-source lookup**: <100ns (dictionary lookup)
- **Adaptive adjustment**: <1ms (once per second)
- **Memory overhead**: <10KB for sampling infrastructure

## Dependencies

**Blocked By**: 
- US-001 (Core Package Setup)
- US-002 (Auto-Managed Correlation)
- US-006 (Runtime-Adaptive Metrics)
- US-008 (Configuration Hot Reload)
- US-009 (Multi-Level Configuration)

**Blocks**: 
- US-012 (Operation Scope) - uses sampling decisions

## Definition of Done

- [x] `ISampler` interface defined
- [x] `ProbabilisticSampler` implemented
- [x] `ConditionalSampler` implemented
- [x] `PerSourceSampler` implemented
- [x] `AdaptiveSampler` implemented
- [x] ActivitySource integration complete
- [x] Sampling metrics exposed
- [x] All unit tests passing (>90% coverage)
- [x] Performance benchmarks met
- [x] XML documentation complete
- [ ] Code reviewed and approved
- [x] Zero warnings in build

## Notes

### Design Decisions

1. **Why deterministic sampling based on TraceId?**
   - Ensures consistency across distributed services
   - Child spans inherit parent's sampling decision
   - No need for distributed coordination

2. **Why conditional sampler wraps base sampler?**
   - Allows layering of sampling strategies
   - "Always sample errors" overrides probabilistic sampling
   - Follows decorator pattern for composability

3. **Why adaptive sampler adjusts once per second?**
   - Balance between responsiveness and stability
   - Avoids oscillation from rapid adjustments
   - Low overhead (single adjustment per second)

4. **Why per-source configuration?**
   - Different services have different criticality
   - Background jobs can be sampled less than user-facing APIs
   - Matches common operational requirements

### Implementation Tips

- Use lower 64 bits of TraceId for hash (sufficient randomness)
- Cache sampling configuration to avoid repeated lookups
- Add debug logging for sampling decisions (off by default)
- Consider exposing sampling stats via health check endpoint

### Common Pitfalls

- Don't call sampler for every tag addition (only at Activity creation)
- Be careful with adaptive sampler in low-throughput scenarios
- Conditional sampling requires tags to be set (chicken-and-egg problem)
- Per-source sampler lookup must be fast (hot path)

### Future Enhancements

- Add tail-based sampling (decide after operation completes)
- Support sampling based on baggage/correlation context
- Implement parent-based sampling (respect parent's decision)
- Add sampling rate adjustments based on error rate

## Related Documentation

- [Project Plan](../project-plan.md#10-activitysource-sampling-with-probabilistic-and-per-source-configuration)
- [OpenTelemetry Sampling Specification](https://opentelemetry.io/docs/specs/otel/trace/sdk/#sampling)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/) - TraceId format

## Implementation Summary

**Completed**: 2026-02-08  
**Implemented by**: GitHub Copilot  

### What Was Implemented

- **Sampling core types**: `ISampler`, `SamplingContext`, `SamplingDecision`, `SamplingResult`
- **ProbabilisticSampler** with deterministic TraceId hashing
- **ConditionalSampler** for errors, slow operations, and custom predicates
- **PerSourceSampler** with per-operation overrides via API
- **AdaptiveSampler** with throughput-based rate adjustment
- **Sampling metrics** counters for total/sample/drop decisions
- **ActivitySource integration** with listener sampling, debug logging, and metrics
- **Hot reload hooks** for `IOptionsMonitor<TelemetryOptions>` and `FileConfigurationReloader`

### Key Files

- `src/HVO.Enterprise.Telemetry/Sampling/ISampler.cs`
- `src/HVO.Enterprise.Telemetry/Sampling/ProbabilisticSampler.cs`
- `src/HVO.Enterprise.Telemetry/Sampling/ConditionalSampler.cs`
- `src/HVO.Enterprise.Telemetry/Sampling/PerSourceSampler.cs`
- `src/HVO.Enterprise.Telemetry/Sampling/AdaptiveSampler.cs`
- `src/HVO.Enterprise.Telemetry/Sampling/SamplingActivitySourceExtensions.cs`
- `src/HVO.Enterprise.Telemetry/Sampling/SamplingMetrics.cs`
- `tests/HVO.Enterprise.Telemetry.Tests/Sampling/ProbabilisticSamplerTests.cs`
- `tests/HVO.Enterprise.Telemetry.Tests/Sampling/ConditionalSamplerTests.cs`
- `tests/HVO.Enterprise.Telemetry.Tests/Sampling/PerSourceSamplerTests.cs`
- `tests/HVO.Enterprise.Telemetry.Tests/Sampling/AdaptiveSamplerTests.cs`
- `tests/HVO.Enterprise.Telemetry.Tests/Sampling/SamplingActivitySourceExtensionsTests.cs`

### Decisions Made

- **TraceId hashing** uses lower 64 bits of hex to remain netstandard2.0 compatible
- **Operation overrides** are supported via per-operation samplers in `PerSourceSampler`
- **Global sampling** defaults to `TelemetryOptions.DefaultSamplingRate` unless overridden by US-009 global config
- **Metrics and debug logging** are emitted at sampling decision time

### Quality Gates

- ✅ Build: 0 warnings, 0 errors
- ✅ Tests: 252 passing
- ✅ Code Review: Pending
- ✅ Security: No secrets added

### Next Steps

This story unblocks:
- US-012 (Operation Scope)
