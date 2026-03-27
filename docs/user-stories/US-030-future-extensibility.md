# US-030: Future Extensibility

**GitHub Issue**: [#32](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/32)  
**Status**: ❌ Not Started  
**Category**: Documentation  
**Effort**: 3 story points  
**Sprint**: 10

## Description

As a **library maintainer and advanced user**,  
I want **well-defined extension points and preparation for future enhancement capabilities**,  
So that **the library can evolve without breaking changes and users can customize behavior without forking the codebase**.

## Acceptance Criteria

1. **Extension Interfaces Defined**
   - [ ] `IMethodInstrumentationStrategy` for custom instrumentation logic
   - [ ] `IMetricExporter` for custom metric destinations
   - [ ] `IActivityEnricher` for custom activity enrichment
   - [ ] `ICorrelationProvider` for custom correlation strategies
   - [ ] `ISamplingStrategy` for custom sampling algorithms

2. **Source Generator Preparation**
   - [ ] Attribute-based API designed for future source generator support
   - [ ] Compilation metadata structure documented
   - [ ] Source generator integration points identified
   - [ ] Example stub source generator created

3. **Plugin Architecture**
   - [ ] `ITelemetryPlugin` interface for modular extensions
   - [ ] Plugin discovery and loading mechanism
   - [ ] Plugin lifecycle management (Initialize, Shutdown)
   - [ ] Plugin dependency injection integration

4. **Extensibility Documentation**
   - [ ] Extension points documented with examples
   - [ ] Guide for creating custom instrumentors
   - [ ] Guide for creating custom exporters
   - [ ] Breaking change policy documented

5. **Compatibility Guarantees**
   - [ ] Public API surface documented
   - [ ] Versioning strategy defined
   - [ ] Deprecation process outlined
   - [ ] Backward compatibility test suite

## Technical Requirements

### IMethodInstrumentationStrategy Interface

```csharp
using System;
using System.Diagnostics;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.Abstractions
{
    /// <summary>
    /// Defines a strategy for instrumenting method calls with telemetry.
    /// Implement this interface to customize how methods are instrumented.
    /// </summary>
    /// <remarks>
    /// This interface enables advanced scenarios such as:
    /// - Custom parameter capture logic
    /// - Specialized timing measurements
    /// - Domain-specific enrichment
    /// - Conditional instrumentation based on runtime state
    /// </remarks>
    public interface IMethodInstrumentationStrategy
    {
        /// <summary>
        /// Determines whether a method should be instrumented.
        /// </summary>
        /// <param name="method">The method to evaluate.</param>
        /// <returns>True if the method should be instrumented; otherwise, false.</returns>
        bool ShouldInstrument(MethodInfo method);
        
        /// <summary>
        /// Called before method execution to set up instrumentation.
        /// </summary>
        /// <param name="context">Context information about the method call.</param>
        /// <returns>
        /// An activity to track the operation, or null if instrumentation should be skipped.
        /// </returns>
        Activity? OnMethodEntry(MethodInstrumentationContext context);
        
        /// <summary>
        /// Called after successful method execution.
        /// </summary>
        /// <param name="context">Context information about the method call.</param>
        /// <param name="activity">The activity created in OnMethodEntry.</param>
        /// <param name="result">The method's return value, if any.</param>
        void OnMethodExit(
            MethodInstrumentationContext context, 
            Activity? activity, 
            object? result);
        
        /// <summary>
        /// Called when method execution throws an exception.
        /// </summary>
        /// <param name="context">Context information about the method call.</param>
        /// <param name="activity">The activity created in OnMethodEntry.</param>
        /// <param name="exception">The exception that was thrown.</param>
        void OnMethodException(
            MethodInstrumentationContext context, 
            Activity? activity, 
            Exception exception);
    }
    
    /// <summary>
    /// Provides context information for method instrumentation.
    /// </summary>
    public sealed class MethodInstrumentationContext
    {
        /// <summary>
        /// Gets the method being instrumented.
        /// </summary>
        public MethodInfo Method { get; }
        
        /// <summary>
        /// Gets the target object instance (null for static methods).
        /// </summary>
        public object? Target { get; }
        
        /// <summary>
        /// Gets the method arguments.
        /// </summary>
        public object?[] Arguments { get; }
        
        /// <summary>
        /// Gets or sets custom state that flows through the instrumentation lifecycle.
        /// </summary>
        public object? State { get; set; }
        
        /// <summary>
        /// Gets the timestamp when instrumentation started.
        /// </summary>
        public DateTimeOffset StartTime { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodInstrumentationContext"/> class.
        /// </summary>
        public MethodInstrumentationContext(
            MethodInfo method,
            object? target,
            object?[] arguments)
        {
            if (method == null || arguments == null)
            {
                throw new ArgumentNullException(
                    method == null ? nameof(method) : nameof(arguments));
            }
            
            Method = method;
            Target = target;
            Arguments = arguments;
            StartTime = DateTimeOffset.UtcNow;
        }
    }
}
```

### Example Custom Strategy Implementation

```csharp
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HVO.Enterprise.Telemetry.Abstractions;

namespace MyApp.Telemetry
{
    /// <summary>
    /// Custom instrumentation strategy that captures parameter values
    /// only for methods marked with a specific attribute.
    /// </summary>
    public class AttributeBasedInstrumentationStrategy : IMethodInstrumentationStrategy
    {
        private static readonly ActivitySource ActivitySource = 
            new ActivitySource("MyApp.CustomInstrumentation");
        
        public bool ShouldInstrument(MethodInfo method)
        {
            // Only instrument methods marked with [TrackTelemetry]
            return method.GetCustomAttribute<TrackTelemetryAttribute>() != null;
        }
        
        public Activity? OnMethodEntry(MethodInstrumentationContext context)
        {
            var attr = context.Method.GetCustomAttribute<TrackTelemetryAttribute>();
            if (attr == null)
            {
                return null;
            }
            
            var activity = ActivitySource.StartActivity(
                context.Method.Name,
                ActivityKind.Internal);
            
            if (activity == null)
            {
                return null;
            }
            
            // Add method signature
            activity.SetTag("method.name", context.Method.Name);
            activity.SetTag("method.class", context.Method.DeclaringType?.FullName);
            
            // Capture parameters if requested
            if (attr.CaptureParameters)
            {
                var parameters = context.Method.GetParameters();
                for (int i = 0; i < parameters.Length && i < context.Arguments.Length; i++)
                {
                    var param = parameters[i];
                    var value = context.Arguments[i];
                    
                    // Skip sensitive parameters
                    if (param.GetCustomAttribute<SensitiveAttribute>() != null)
                    {
                        activity.SetTag($"param.{param.Name}", "[REDACTED]");
                    }
                    else if (value != null)
                    {
                        activity.SetTag($"param.{param.Name}", value.ToString());
                    }
                }
            }
            
            return activity;
        }
        
        public void OnMethodExit(
            MethodInstrumentationContext context,
            Activity? activity,
            object? result)
        {
            if (activity == null)
            {
                return;
            }
            
            var attr = context.Method.GetCustomAttribute<TrackTelemetryAttribute>();
            if (attr?.CaptureReturnValue == true && result != null)
            {
                activity.SetTag("return.type", result.GetType().Name);
                activity.SetTag("return.value", result.ToString());
            }
            
            activity.SetStatus(ActivityStatusCode.Ok);
            activity.Dispose();
        }
        
        public void OnMethodException(
            MethodInstrumentationContext context,
            Activity? activity,
            Exception exception)
        {
            if (activity == null)
            {
                return;
            }
            
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.RecordException(exception);
            activity.Dispose();
        }
    }
    
    /// <summary>
    /// Marks a method for telemetry tracking.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class TrackTelemetryAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to capture method parameters.
        /// </summary>
        public bool CaptureParameters { get; set; }
        
        /// <summary>
        /// Gets or sets whether to capture the return value.
        /// </summary>
        public bool CaptureReturnValue { get; set; }
    }
    
    /// <summary>
    /// Marks a parameter as sensitive (value will be redacted).
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class SensitiveAttribute : Attribute
    {
    }
}
```

### Source Generator Preparation

#### Attribute-Based API for Future Generators

```csharp
using System;

namespace HVO.Enterprise.Telemetry.Annotations
{
    /// <summary>
    /// Marks a class for automatic telemetry instrumentation.
    /// In future versions, a source generator will create instrumentation code at compile time.
    /// </summary>
    /// <remarks>
    /// Current implementation uses DispatchProxy (runtime).
    /// Future implementation will use source generators (compile-time) for better performance.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, 
        Inherited = true, AllowMultiple = false)]
    public sealed class InstrumentedAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the activity source name for this instrumented type.
        /// If not specified, uses the type's full name.
        /// </summary>
        public string? ActivitySourceName { get; set; }
        
        /// <summary>
        /// Gets or sets the default instrumentation strategy.
        /// </summary>
        public InstrumentationMode Mode { get; set; } = InstrumentationMode.AllPublicMethods;
    }
    
    /// <summary>
    /// Specifies instrumentation behavior for a method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class InstrumentAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the operation name (defaults to method name).
        /// </summary>
        public string? OperationName { get; set; }
        
        /// <summary>
        /// Gets or sets whether to capture method parameters.
        /// </summary>
        public bool CaptureParameters { get; set; }
        
        /// <summary>
        /// Gets or sets whether to capture the return value.
        /// </summary>
        public bool CaptureReturnValue { get; set; }
        
        /// <summary>
        /// Gets or sets the activity kind.
        /// </summary>
        public ActivityKind ActivityKind { get; set; } = ActivityKind.Internal;
    }
    
    /// <summary>
    /// Excludes a method from automatic instrumentation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NoInstrumentationAttribute : Attribute
    {
    }
    
    /// <summary>
    /// Defines instrumentation modes.
    /// </summary>
    public enum InstrumentationMode
    {
        /// <summary>No automatic instrumentation.</summary>
        None = 0,
        
        /// <summary>Instrument all public methods.</summary>
        AllPublicMethods = 1,
        
        /// <summary>Instrument only methods marked with [Instrument].</summary>
        ExplicitOnly = 2,
        
        /// <summary>Instrument all methods (public and private).</summary>
        AllMethods = 3
    }
}
```

#### Source Generator Integration Contract

```csharp
namespace HVO.Enterprise.Telemetry.SourceGeneration
{
    /// <summary>
    /// Metadata for source generator to consume.
    /// This enables compile-time instrumentation in future versions.
    /// </summary>
    internal static class GeneratorContract
    {
        /// <summary>
        /// Marker interface that source generators will detect.
        /// Types implementing this interface can be source-generated.
        /// </summary>
        public interface ISourceGeneratable
        {
            // Source generators will look for this interface
            // and generate optimized instrumentation code
        }
        
        /// <summary>
        /// Defines metadata that source generators can consume.
        /// </summary>
        public struct InstrumentationMetadata
        {
            /// <summary>Type to instrument.</summary>
            public Type Type;
            
            /// <summary>Activity source name.</summary>
            public string ActivitySourceName;
            
            /// <summary>Methods to instrument.</summary>
            public InstrumentedMethod[] Methods;
        }
        
        /// <summary>
        /// Method-level instrumentation metadata.
        /// </summary>
        public struct InstrumentedMethod
        {
            /// <summary>Method name.</summary>
            public string Name;
            
            /// <summary>Operation name for telemetry.</summary>
            public string OperationName;
            
            /// <summary>Whether to capture parameters.</summary>
            public bool CaptureParameters;
            
            /// <summary>Whether to capture return value.</summary>
            public bool CaptureReturnValue;
            
            /// <summary>Activity kind.</summary>
            public ActivityKind Kind;
        }
    }
}
```

### Plugin Architecture

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace HVO.Enterprise.Telemetry.Plugins
{
    /// <summary>
    /// Defines a plugin that extends HVO.Enterprise.Telemetry functionality.
    /// </summary>
    public interface ITelemetryPlugin
    {
        /// <summary>
        /// Gets the unique name of this plugin.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the plugin version.
        /// </summary>
        Version Version { get; }
        
        /// <summary>
        /// Gets the names of plugins this plugin depends on.
        /// </summary>
        IEnumerable<string> Dependencies { get; }
        
        /// <summary>
        /// Called when the plugin is loaded.
        /// Use this to register services, configure telemetry, etc.
        /// </summary>
        /// <param name="services">Service collection for dependency injection.</param>
        /// <param name="configuration">Telemetry configuration.</param>
        void Initialize(IServiceCollection services, TelemetryConfiguration configuration);
        
        /// <summary>
        /// Called when telemetry is shutting down.
        /// Use this to flush buffers, close connections, etc.
        /// </summary>
        void Shutdown();
    }
    
    /// <summary>
    /// Manages plugin loading and lifecycle.
    /// </summary>
    public interface IPluginManager
    {
        /// <summary>
        /// Registers a plugin.
        /// </summary>
        void RegisterPlugin(ITelemetryPlugin plugin);
        
        /// <summary>
        /// Discovers and loads plugins from specified directories.
        /// </summary>
        void DiscoverPlugins(params string[] searchPaths);
        
        /// <summary>
        /// Gets all registered plugins.
        /// </summary>
        IReadOnlyCollection<ITelemetryPlugin> GetPlugins();
        
        /// <summary>
        /// Initializes all registered plugins in dependency order.
        /// </summary>
        void InitializeAll(IServiceCollection services, TelemetryConfiguration configuration);
        
        /// <summary>
        /// Shuts down all plugins in reverse dependency order.
        /// </summary>
        void ShutdownAll();
    }
    
    /// <summary>
    /// Example plugin implementation.
    /// </summary>
    public class CustomMetricsPlugin : ITelemetryPlugin
    {
        public string Name => "CustomMetrics";
        
        public Version Version => new Version(1, 0, 0);
        
        public IEnumerable<string> Dependencies => Array.Empty<string>();
        
        public void Initialize(
            IServiceCollection services, 
            TelemetryConfiguration configuration)
        {
            // Register custom metric exporters
            services.AddSingleton<IMetricExporter, PrometheusExporter>();
            services.AddSingleton<IMetricExporter, InfluxDbExporter>();
            
            // Configure metric collection
            configuration.MetricsEnabled = true;
            configuration.MetricsInterval = TimeSpan.FromSeconds(10);
        }
        
        public void Shutdown()
        {
            // Cleanup resources
        }
    }
}
```

### Custom Exporter Interface

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HVO.Enterprise.Telemetry.Abstractions
{
    /// <summary>
    /// Defines an exporter for telemetry data.
    /// Implement this to send telemetry to custom destinations.
    /// </summary>
    public interface ITelemetryExporter : IDisposable
    {
        /// <summary>
        /// Gets the name of this exporter.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Exports a batch of activities (traces).
        /// </summary>
        Task<ExportResult> ExportAsync(
            IEnumerable<Activity> activities,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Forces export of any buffered data.
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Result of an export operation.
    /// </summary>
    public enum ExportResult
    {
        /// <summary>Export succeeded.</summary>
        Success,
        
        /// <summary>Export failed but can be retried.</summary>
        Failure,
        
        /// <summary>Export failed and should not be retried.</summary>
        FatalError
    }
    
    /// <summary>
    /// Example custom exporter implementation.
    /// </summary>
    public class FileExporter : ITelemetryExporter
    {
        private readonly string _filePath;
        private readonly object _lock = new object();
        
        public string Name => "FileExporter";
        
        public FileExporter(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            _filePath = filePath;
        }
        
        public Task<ExportResult> ExportAsync(
            IEnumerable<Activity> activities,
            CancellationToken cancellationToken = default)
        {
            try
            {
                lock (_lock)
                {
                    using var writer = System.IO.File.AppendText(_filePath);
                    
                    foreach (var activity in activities)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return Task.FromResult(ExportResult.Failure);
                        }
                        
                        writer.WriteLine($"{activity.StartTimeUtc:O}|{activity.OperationName}|{activity.Duration.TotalMilliseconds}ms");
                    }
                }
                
                return Task.FromResult(ExportResult.Success);
            }
            catch (Exception)
            {
                return Task.FromResult(ExportResult.Failure);
            }
        }
        
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            // File writes are synchronous, nothing to flush
            return Task.CompletedTask;
        }
        
        public void Dispose()
        {
            // No resources to dispose
        }
    }
}
```

### Activity Enricher Interface

```csharp
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Abstractions
{
    /// <summary>
    /// Enriches activities with additional data.
    /// </summary>
    public interface IActivityEnricher
    {
        /// <summary>
        /// Enriches an activity with additional tags or context.
        /// </summary>
        /// <param name="activity">The activity to enrich.</param>
        void Enrich(Activity activity);
    }
    
    /// <summary>
    /// Example: Enricher that adds environment information.
    /// </summary>
    public class EnvironmentEnricher : IActivityEnricher
    {
        public void Enrich(Activity activity)
        {
            activity.SetTag("env.machineName", Environment.MachineName);
            activity.SetTag("env.osVersion", Environment.OSVersion.ToString());
            activity.SetTag("env.processorCount", Environment.ProcessorCount);
            activity.SetTag("env.is64BitProcess", Environment.Is64BitProcess);
        }
    }
}
```

## Testing Requirements

### Unit Tests

1. **IMethodInstrumentationStrategy Tests**
   ```csharp
   [Fact]
   public void CustomStrategy_ShouldInstrument_ReturnsTrue_WhenAttributePresent()
   {
       // Arrange
       var strategy = new AttributeBasedInstrumentationStrategy();
       var method = typeof(TestClass).GetMethod(nameof(TestClass.TrackedMethod));
       
       // Act
       var result = strategy.ShouldInstrument(method);
       
       // Assert
       Assert.True(result);
   }
   
   [Fact]
   public void CustomStrategy_OnMethodEntry_CreatesActivity_WithCorrectTags()
   {
       // Arrange
       var strategy = new AttributeBasedInstrumentationStrategy();
       var method = typeof(TestClass).GetMethod(nameof(TestClass.TrackedMethod));
       var context = new MethodInstrumentationContext(
           method, 
           new TestClass(), 
           new object[] { "param1", 123 });
       
       // Act
       var activity = strategy.OnMethodEntry(context);
       
       // Assert
       Assert.NotNull(activity);
       Assert.Equal(method.Name, activity.OperationName);
       Assert.Equal("param1", activity.GetTagItem("param.arg1"));
       Assert.Equal("123", activity.GetTagItem("param.arg2"));
   }
   ```

2. **Plugin System Tests**
   ```csharp
   [Fact]
   public void PluginManager_RegisterPlugin_AddsPlugin()
   {
       // Arrange
       var manager = new PluginManager();
       var plugin = new CustomMetricsPlugin();
       
       // Act
       manager.RegisterPlugin(plugin);
       
       // Assert
       Assert.Contains(plugin, manager.GetPlugins());
   }
   
   [Fact]
   public void PluginManager_InitializeAll_CallsPluginsInDependencyOrder()
   {
       // Arrange
       var manager = new PluginManager();
       var initOrder = new List<string>();
       
       var plugin1 = new MockPlugin("Plugin1", Array.Empty<string>(), 
           () => initOrder.Add("Plugin1"));
       var plugin2 = new MockPlugin("Plugin2", new[] { "Plugin1" }, 
           () => initOrder.Add("Plugin2"));
       
       manager.RegisterPlugin(plugin1);
       manager.RegisterPlugin(plugin2);
       
       var services = new ServiceCollection();
       var config = new TelemetryConfiguration();
       
       // Act
       manager.InitializeAll(services, config);
       
       // Assert
       Assert.Equal(new[] { "Plugin1", "Plugin2" }, initOrder);
   }
   ```

3. **Custom Exporter Tests**
   ```csharp
   [Fact]
   public async Task FileExporter_ExportAsync_WritesActivitiesToFile()
   {
       // Arrange
       var tempFile = Path.GetTempFileName();
       var exporter = new FileExporter(tempFile);
       
       var activity = new Activity("TestOperation");
       activity.Start();
       await Task.Delay(10);
       activity.Stop();
       
       // Act
       var result = await exporter.ExportAsync(new[] { activity });
       
       // Assert
       Assert.Equal(ExportResult.Success, result);
       
       var content = File.ReadAllText(tempFile);
       Assert.Contains("TestOperation", content);
       
       // Cleanup
       File.Delete(tempFile);
   }
   ```

4. **Source Generator Attribute Tests**
   ```csharp
   [Fact]
   public void InstrumentedAttribute_CanBeAppliedToClass()
   {
       // Arrange & Act
       var attr = typeof(TestInstrumentedClass)
           .GetCustomAttribute<InstrumentedAttribute>();
       
       // Assert
       Assert.NotNull(attr);
       Assert.Equal(InstrumentationMode.AllPublicMethods, attr.Mode);
   }
   
   [Instrumented(Mode = InstrumentationMode.AllPublicMethods)]
   public class TestInstrumentedClass
   {
       [Instrument(CaptureParameters = true)]
       public void TrackedMethod(string arg1, int arg2) { }
       
       [NoInstrumentation]
       public void UntrackedMethod() { }
   }
   ```

### Integration Tests

1. **End-to-End Plugin Test**
   ```csharp
   [Fact]
   public void EndToEnd_CustomPlugin_IntegratesWithTelemetrySystem()
   {
       // Arrange
       var services = new ServiceCollection();
       services.AddTelemetry(options =>
       {
           options.ServiceName = "TestApp";
           options.RegisterPlugin(new CustomMetricsPlugin());
       });
       
       var provider = services.BuildServiceProvider();
       var telemetry = provider.GetRequiredService<ITelemetryService>();
       
       // Act
       using (var scope = telemetry.StartOperation("TestOperation"))
       {
           // Operation code
       }
       
       // Assert
       // Verify custom metrics were recorded
       var metrics = provider.GetService<IMetricExporter>();
       Assert.NotNull(metrics);
   }
   ```

2. **Custom Strategy Integration Test**
   ```csharp
   [Fact]
   public async Task EndToEnd_CustomStrategy_InstrumentsMethodCalls()
   {
       // Arrange
       var services = new ServiceCollection();
       services.AddTelemetry(options =>
       {
           options.InstrumentationStrategy = new AttributeBasedInstrumentationStrategy();
       });
       
       services.AddSingleton<IOrderService, OrderService>();
       
       var provider = services.BuildServiceProvider();
       var proxy = provider.CreateInstrumentedProxy<IOrderService>();
       
       // Act
       await proxy.ProcessOrderAsync(new Order { Id = 123 });
       
       // Assert
       // Verify activity was created with correct tags
   }
   ```

## Performance Requirements

- **Strategy Overhead**: <50ns per method call determination
- **Plugin Initialization**: <100ms total for all plugins
- **Exporter Throughput**: >5K activities/second per exporter
- **Memory Overhead**: <100KB per registered plugin

## Dependencies

**Blocked By**: 
- US-001: Core Package Setup
- US-014: DispatchProxy Instrumentation

**Blocks / Enables**:
- US-033: OpenTelemetry/OTLP Extension (implements `ITelemetryExporter` and `ITelemetryPlugin`)
- US-034: Seq Extension (implements `ITelemetryExporter` for CLEF push)
- US-035: Grafana Extension (implements `ITelemetryExporter` for Loki push)
- US-036: gRPC Interceptor Extension (implements `IActivityEnricher` pattern)

> US-033 serves as the **reference implementation** for the `ITelemetryPlugin` and `ITelemetryExporter` interfaces defined in this story.

## Definition of Done

- [ ] All extension interfaces defined and documented
- [ ] Attribute-based API complete for future source generators
- [ ] Plugin architecture implemented and tested
- [ ] Example implementations for all interfaces
- [ ] Extensibility documentation complete
- [ ] All unit tests passing
- [ ] Integration tests demonstrate extensibility
- [ ] Code reviewed and approved
- [ ] Breaking change policy documented
- [ ] Versioning strategy documented

## Notes

### Design Decisions

1. **Why IMethodInstrumentationStrategy?**
   - Enables custom instrumentation logic without forking
   - Supports domain-specific telemetry patterns
   - Allows gradual migration of existing instrumentation

2. **Why prepare for source generators now?**
   - Attribute-based API is easier to consume
   - Source generators provide zero-overhead instrumentation
   - Smooth migration path from DispatchProxy to source generators
   - .NET 8+ applications can benefit from compile-time optimization

3. **Why plugin architecture?**
   - Modular extensions without coupling
   - Community can create plugins without PRs
   - Enables commercial plugins (e.g., custom exporters)
   - Clear dependency management

4. **Why ITelemetryExporter interface?**
   - Users often need custom destinations
   - Standardized export contract
   - Easy testing with mock exporters
   - Enables multi-exporter scenarios

### Implementation Tips

1. **Start with interfaces** - Define contracts before implementation
2. **Document extensively** - Extension points need excellent docs
3. **Provide examples** - Each interface should have working example
4. **Version carefully** - Extension interfaces are part of public API
5. **Test thoroughly** - Extensions are hard to test after release

### Source Generator Timeline

**Phase 1 (Current)**: Attribute-based API, DispatchProxy runtime implementation
**Phase 2 (v1.1)**: Basic source generator for simple cases
**Phase 3 (v1.2)**: Advanced source generator with full feature parity
**Phase 4 (v2.0)**: Source generator recommended default, DispatchProxy fallback

### Common Pitfalls

1. **Breaking interface changes** - Extension interfaces must remain stable
2. **Forgetting backward compatibility** - Old plugins must continue working
3. **Insufficient examples** - Users need clear guidance
4. **Poor error messages** - Extension errors hard to diagnose
5. **No versioning** - Extension version compatibility must be clear

### Known Consumers (Extension Packages)

The following extension packages will implement or consume the extension interfaces defined here:

| Interface | Consumer Package | Story |
|---|---|---|
| `ITelemetryExporter` | `HVO.Enterprise.Telemetry.OpenTelemetry` | US-033 |
| `ITelemetryExporter` | `HVO.Enterprise.Telemetry.Seq` | US-034 |
| `ITelemetryExporter` | `HVO.Enterprise.Telemetry.Grafana` | US-035 |
| `ITelemetryPlugin` | `HVO.Enterprise.Telemetry.OpenTelemetry` | US-033 |
| `IActivityEnricher` | `HVO.Enterprise.Telemetry.Grpc` | US-036 |

### Future Enhancements

- **Dynamic plugin loading** - Load plugins at runtime from assemblies
- **Plugin marketplace** - Central registry of available plugins
- **Visual Studio extension** - Code generation tooling
- **Performance profiling** - Built-in profiler for custom strategies
- **Plugin templates** - dotnet new templates for plugin projects

### Breaking Change Policy

**Major Version (X.0.0)**: Breaking changes to extension interfaces allowed
**Minor Version (1.X.0)**: Only additive changes (new interfaces, new methods)
**Patch Version (1.0.X)**: No interface changes, only bug fixes

### Deprecation Process

1. **Announce**: Document deprecation in release notes
2. **Mark**: Add `[Obsolete]` attribute with migration guidance
3. **Wait**: Minimum 6 months before removal
4. **Remove**: Only in major version release

## Related Documentation

- [Project Plan](../project-plan.md#30-document-future-extension-points)
- [Architecture Document](./ARCHITECTURE.md#extension-points)
- [Source Generator Design](./source-generator-design.md)
- [Plugin Development Guide](./plugin-development.md)
- [API Versioning Policy](./versioning-policy.md)
