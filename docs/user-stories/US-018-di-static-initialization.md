# US-018: DI and Static Initialization

**GitHub Issue**: [#20](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/20)  
**Status**: ✅ Complete  
**Category**: Core Package  
**Effort**: 5 story points  
**Sprint**: 4

## Description

As a **developer integrating telemetry into my application**,  
I want **both dependency injection (AddTelemetry()) and static initialization (Telemetry.Initialize()) patterns**,  
So that **I can use the library in modern ASP.NET Core apps with DI or legacy applications without DI**.

## Acceptance Criteria

1. **Dependency Injection Support**
   - [x] `AddTelemetry()` extension method for IServiceCollection
   - [x] Registers all telemetry services with appropriate lifetimes
   - [x] Supports fluent configuration API
   - [x] Integrates with IOptions pattern
   - [x] Returns IServiceCollection for chaining

2. **Static Initialization API**
   - [x] `Telemetry.Initialize()` for non-DI scenarios
   - [x] Creates singleton telemetry instance
   - [x] Thread-safe initialization (once-only)
   - [x] `Telemetry.IsInitialized` property
   - [x] Static property accessors for common operations

3. **Dual-Mode Operation**
   - [x] Library works in both DI and static modes
   - [x] Same features available in both modes
   - [x] Clear error messages if used incorrectly
   - [x] Documentation for both patterns

4. **Configuration Options**
   - [x] TelemetryOptions class for all settings
   - [x] IOptions<TelemetryOptions> support for DI mode
   - [x] Direct configuration object for static mode
   - [x] Validation of configuration on startup

5. **Lifecycle Integration**
   - [x] Automatic startup in DI mode (hosted service)
   - [x] Manual Shutdown() for static mode
   - [x] Graceful shutdown on AppDomain.Unload
   - [x] Flush pending telemetry on shutdown

## Technical Requirements

### TelemetryOptions Configuration

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Configuration options for the HVO telemetry system.
    /// </summary>
    public sealed class TelemetryOptions
    {
        /// <summary>
        /// Gets or sets the service name for telemetry.
        /// </summary>
        public string ServiceName { get; set; } = "Unknown";

        /// <summary>
        /// Gets or sets the service version.
        /// </summary>
        public string? ServiceVersion { get; set; }

        /// <summary>
        /// Gets or sets the environment name (e.g., Production, Staging).
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Gets or sets whether telemetry is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum log level for telemetry operations.
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets the background queue capacity.
        /// </summary>
        public int QueueCapacity { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the number of background worker threads.
        /// </summary>
        public int WorkerCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the flush interval for batched operations.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets activity sources to enable.
        /// </summary>
        public List<string> ActivitySources { get; set; } = new()
        {
            "HVO.Enterprise.Telemetry"
        };

        /// <summary>
        /// Gets or sets the activity sampling ratio (0.0 to 1.0).
        /// 1.0 = sample everything, 0.1 = sample 10%, 0.0 = sample nothing.
        /// </summary>
        public double SamplingRatio { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets whether to automatically instrument HttpClient.
        /// </summary>
        public bool InstrumentHttpClient { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to capture exception stack traces.
        /// </summary>
        public bool CaptureExceptionStackTraces { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum exception fingerprint cache size.
        /// </summary>
        public int MaxExceptionFingerprints { get; set; } = 1000;

        /// <summary>
        /// Gets or sets resource attributes (key-value pairs).
        /// </summary>
        public Dictionary<string, object> ResourceAttributes { get; set; } = new();

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
                throw new InvalidOperationException("ServiceName is required.");

            if (QueueCapacity <= 0)
                throw new InvalidOperationException("QueueCapacity must be positive.");

            if (WorkerCount <= 0)
                throw new InvalidOperationException("WorkerCount must be positive.");

            if (SamplingRatio < 0.0 || SamplingRatio > 1.0)
                throw new InvalidOperationException(
                    "SamplingRatio must be between 0.0 and 1.0.");

            if (FlushInterval <= TimeSpan.Zero)
                throw new InvalidOperationException("FlushInterval must be positive.");
        }
    }
}
```

### Dependency Injection Extensions

```csharp
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Extension methods for registering telemetry services with dependency injection.
    /// </summary>
    public static class TelemetryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds HVO telemetry services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration delegate.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTelemetry(
            this IServiceCollection services,
            Action<TelemetryOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Configure options
            var optionsBuilder = services.AddOptions<TelemetryOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // Validate options on startup
            optionsBuilder.ValidateDataAnnotations();
            services.AddSingleton<IValidateOptions<TelemetryOptions>, 
                TelemetryOptionsValidator>();

            // Register core services
            services.TryAddSingleton<ITelemetryStatistics, TelemetryStatistics>();
            services.TryAddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
            services.TryAddSingleton<ITelemetryQueue, BoundedTelemetryQueue>();
            services.TryAddSingleton<IActivitySourceManager, ActivitySourceManager>();
            
            // Register telemetry service as both interface and concrete type
            services.TryAddSingleton<TelemetryService>();
            services.TryAddSingleton<ITelemetryService>(sp => 
                sp.GetRequiredService<TelemetryService>());

            // Register hosted service for lifecycle management
            services.AddHostedService<TelemetryHostedService>();

            return services;
        }

        /// <summary>
        /// Adds HVO telemetry services using configuration from IConfiguration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Configuration section for telemetry options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTelemetry(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return services.AddTelemetry(options => 
                configuration.Bind(options));
        }

        /// <summary>
        /// Adds HVO telemetry services with a configuration builder pattern.
        /// </summary>
        public static IServiceCollection AddTelemetry(
            this IServiceCollection services,
            Action<TelemetryBuilder> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new TelemetryBuilder(services);
            configure(builder);
            
            return services;
        }
    }

    /// <summary>
    /// Builder for configuring telemetry services.
    /// </summary>
    public sealed class TelemetryBuilder
    {
        public IServiceCollection Services { get; }

        internal TelemetryBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            
            // Add core telemetry services
            Services.AddTelemetry();
        }

        /// <summary>
        /// Configures telemetry options.
        /// </summary>
        public TelemetryBuilder Configure(Action<TelemetryOptions> configure)
        {
            Services.Configure(configure);
            return this;
        }

        /// <summary>
        /// Adds HTTP client instrumentation.
        /// </summary>
        public TelemetryBuilder AddHttpInstrumentation(
            Action<HttpInstrumentationOptions>? configure = null)
        {
            // Register HTTP instrumentation services
            if (configure != null)
            {
                Services.Configure(configure);
            }
            
            return this;
        }

        /// <summary>
        /// Adds custom activity source.
        /// </summary>
        public TelemetryBuilder AddActivitySource(string name, string? version = null)
        {
            Services.Configure<TelemetryOptions>(options =>
            {
                if (!options.ActivitySources.Contains(name))
                {
                    options.ActivitySources.Add(name);
                }
            });
            
            return this;
        }
    }

    /// <summary>
    /// Validates telemetry options.
    /// </summary>
    internal sealed class TelemetryOptionsValidator : IValidateOptions<TelemetryOptions>
    {
        public ValidateOptionsResult Validate(string? name, TelemetryOptions options)
        {
            try
            {
                options.Validate();
                return ValidateOptionsResult.Success;
            }
            catch (Exception ex)
            {
                return ValidateOptionsResult.Fail(ex.Message);
            }
        }
    }

    /// <summary>
    /// Hosted service for telemetry lifecycle management.
    /// </summary>
    internal sealed class TelemetryHostedService : IHostedService
    {
        private readonly TelemetryService _telemetryService;
        private readonly ILogger<TelemetryHostedService> _logger;

        public TelemetryHostedService(
            TelemetryService telemetryService,
            ILogger<TelemetryHostedService> logger)
        {
            _telemetryService = telemetryService ?? 
                throw new ArgumentNullException(nameof(telemetryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting telemetry service");
            _telemetryService.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping telemetry service");
            _telemetryService.Shutdown();
            return Task.CompletedTask;
        }
    }
}
```

### Static Initialization API

```csharp
using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Static entry point for telemetry operations. Supports both DI and static initialization.
    /// </summary>
    public static class Telemetry
    {
        private static readonly object _lock = new object();
        private static TelemetryService? _instance;
        private static bool _isInitialized;

        /// <summary>
        /// Gets whether telemetry has been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the current telemetry statistics.
        /// </summary>
        public static ITelemetryStatistics Statistics
        {
            get
            {
                EnsureInitialized();
                return _instance!.Statistics;
            }
        }

        /// <summary>
        /// Gets the current correlation ID for the execution context.
        /// </summary>
        public static string? CurrentCorrelationId => 
            CorrelationContext.Current?.CorrelationId;

        /// <summary>
        /// Gets the current Activity (distributed trace span).
        /// </summary>
        public static Activity? CurrentActivity => Activity.Current;

        /// <summary>
        /// Initializes telemetry with default options.
        /// </summary>
        /// <returns>True if initialized successfully, false if already initialized.</returns>
        public static bool Initialize()
        {
            return Initialize(new TelemetryOptions());
        }

        /// <summary>
        /// Initializes telemetry with specified options.
        /// </summary>
        /// <param name="options">Configuration options.</param>
        /// <returns>True if initialized successfully, false if already initialized.</returns>
        public static bool Initialize(TelemetryOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return Initialize(options, NullLoggerFactory.Instance);
        }

        /// <summary>
        /// Initializes telemetry with options and logger factory.
        /// </summary>
        /// <param name="options">Configuration options.</param>
        /// <param name="loggerFactory">Logger factory for internal logging.</param>
        /// <returns>True if initialized successfully, false if already initialized.</returns>
        public static bool Initialize(
            TelemetryOptions options,
            ILoggerFactory loggerFactory)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            lock (_lock)
            {
                if (_isInitialized)
                {
                    return false;
                }

                options.Validate();

                _instance = new TelemetryService(
                    options,
                    loggerFactory.CreateLogger<TelemetryService>());

                _instance.Start();

                // Register shutdown hook
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;

                _isInitialized = true;
                return true;
            }
        }

        /// <summary>
        /// Shuts down telemetry and flushes pending data.
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                if (!_isInitialized || _instance == null)
                    return;

                _instance.Shutdown();
                _instance = null;
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Starts a new operation scope with automatic timing.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns>Disposable operation scope.</returns>
        public static IOperationScope StartOperation(string operationName)
        {
            EnsureInitialized();
            return _instance!.StartOperation(operationName);
        }

        /// <summary>
        /// Tracks an exception.
        /// </summary>
        public static void TrackException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            EnsureInitialized();
            _instance!.TrackException(exception);
        }

        /// <summary>
        /// Tracks a custom event.
        /// </summary>
        public static void TrackEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("Event name cannot be empty.", nameof(eventName));

            EnsureInitialized();
            _instance!.TrackEvent(eventName);
        }

        /// <summary>
        /// Records a metric value.
        /// </summary>
        public static void RecordMetric(string metricName, double value)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be empty.", nameof(metricName));

            EnsureInitialized();
            _instance!.RecordMetric(metricName, value);
        }

        /// <summary>
        /// Sets correlation ID for the current execution context.
        /// </summary>
        public static IDisposable SetCorrelationId(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException(
                    "Correlation ID cannot be empty.", 
                    nameof(correlationId));

            return CorrelationContext.SetCorrelationId(correlationId);
        }

        /// <summary>
        /// Generates a new correlation ID and sets it for the current context.
        /// </summary>
        public static IDisposable BeginCorrelation()
        {
            var correlationId = Guid.NewGuid().ToString("N");
            return SetCorrelationId(correlationId);
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized || _instance == null)
            {
                throw new InvalidOperationException(
                    "Telemetry has not been initialized. Call Telemetry.Initialize() first.");
            }
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            Shutdown();
        }

        private static void OnDomainUnload(object? sender, EventArgs e)
        {
            Shutdown();
        }
    }
}
```

### Core TelemetryService Implementation

```csharp
using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Core telemetry service implementation.
    /// </summary>
    public sealed class TelemetryService : ITelemetryService, IDisposable
    {
        private readonly TelemetryOptions _options;
        private readonly ILogger<TelemetryService> _logger;
        private readonly ITelemetryStatistics _statistics;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly ITelemetryQueue _queue;
        private readonly IActivitySourceManager _activitySourceManager;
        private bool _isStarted;
        private bool _isDisposed;

        public ITelemetryStatistics Statistics => _statistics;

        /// <summary>
        /// Constructor for DI-based initialization.
        /// </summary>
        public TelemetryService(
            IOptions<TelemetryOptions> options,
            ITelemetryStatistics statistics,
            ICorrelationIdProvider correlationIdProvider,
            ITelemetryQueue queue,
            IActivitySourceManager activitySourceManager,
            ILogger<TelemetryService> logger)
            : this(
                options?.Value ?? throw new ArgumentNullException(nameof(options)),
                statistics,
                correlationIdProvider,
                queue,
                activitySourceManager,
                logger)
        {
        }

        /// <summary>
        /// Constructor for static initialization.
        /// </summary>
        internal TelemetryService(
            TelemetryOptions options,
            ILogger<TelemetryService> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create dependencies directly (not using DI)
            _statistics = new TelemetryStatistics();
            _correlationIdProvider = new CorrelationIdProvider();
            _queue = new BoundedTelemetryQueue(options.QueueCapacity, _statistics);
            _activitySourceManager = new ActivitySourceManager(options.ActivitySources);
        }

        /// <summary>
        /// Full constructor with all dependencies.
        /// </summary>
        internal TelemetryService(
            TelemetryOptions options,
            ITelemetryStatistics statistics,
            ICorrelationIdProvider correlationIdProvider,
            ITelemetryQueue queue,
            IActivitySourceManager activitySourceManager,
            ILogger<TelemetryService> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
            _correlationIdProvider = correlationIdProvider ?? 
                throw new ArgumentNullException(nameof(correlationIdProvider));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _activitySourceManager = activitySourceManager ?? 
                throw new ArgumentNullException(nameof(activitySourceManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            if (_isStarted)
                return;

            _logger.LogInformation(
                "Starting telemetry service: {ServiceName} v{Version} ({Environment})",
                _options.ServiceName,
                _options.ServiceVersion ?? "unknown",
                _options.Environment ?? "unknown");

            // Start background workers
            _queue.Start(_options.WorkerCount);

            _isStarted = true;
        }

        public void Shutdown()
        {
            if (!_isStarted || _isDisposed)
                return;

            _logger.LogInformation("Shutting down telemetry service");

            // Stop accepting new work
            _queue.Stop();

            // Flush remaining items
            _queue.Flush(timeout: TimeSpan.FromSeconds(10));

            _isStarted = false;
        }

        public IOperationScope StartOperation(string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException(
                    "Operation name cannot be empty.", 
                    nameof(operationName));

            var activity = _activitySourceManager.StartActivity(operationName);
            return new OperationScope(operationName, activity, this);
        }

        public void TrackException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (!_options.Enabled)
                return;

            _queue.Enqueue(new ExceptionTelemetryItem(exception));
            _statistics.IncrementExceptionsTracked();
        }

        public void TrackEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("Event name cannot be empty.", nameof(eventName));

            if (!_options.Enabled)
                return;

            _queue.Enqueue(new EventTelemetryItem(eventName));
            _statistics.IncrementEventsRecorded();
        }

        public void RecordMetric(string metricName, double value)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be empty.", nameof(metricName));

            if (!_options.Enabled)
                return;

            _queue.Enqueue(new MetricTelemetryItem(metricName, value));
            _statistics.IncrementMetricsRecorded();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Shutdown();
            _isDisposed = true;
        }
    }

    /// <summary>
    /// Interface for telemetry service operations.
    /// </summary>
    public interface ITelemetryService
    {
        ITelemetryStatistics Statistics { get; }
        void Start();
        void Shutdown();
        IOperationScope StartOperation(string operationName);
        void TrackException(Exception exception);
        void TrackEvent(string eventName);
        void RecordMetric(string metricName, double value);
    }
}
```

## Testing Requirements

### Unit Tests

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using FluentAssertions;

namespace HVO.Enterprise.Telemetry.Tests
{
    public class TelemetryInitializationTests
    {
        [Fact]
        public void AddTelemetry_RegistersAllServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTelemetry(options =>
            {
                options.ServiceName = "TestService";
            });

            var provider = services.BuildServiceProvider();

            // Assert
            provider.GetService<ITelemetryService>().Should().NotBeNull();
            provider.GetService<ITelemetryStatistics>().Should().NotBeNull();
            provider.GetService<ICorrelationIdProvider>().Should().NotBeNull();
        }

        [Fact]
        public void AddTelemetry_ConfiguresOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTelemetry(options =>
            {
                options.ServiceName = "MyService";
                options.ServiceVersion = "1.0.0";
                options.SamplingRatio = 0.5;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            // Assert
            options.Value.ServiceName.Should().Be("MyService");
            options.Value.ServiceVersion.Should().Be("1.0.0");
            options.Value.SamplingRatio.Should().Be(0.5);
        }

        [Fact]
        public void AddTelemetry_FromConfiguration_BindsCorrectly()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                ["Telemetry:ServiceName"] = "ConfigService",
                ["Telemetry:QueueCapacity"] = "5000",
                ["Telemetry:SamplingRatio"] = "0.25"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            var services = new ServiceCollection();

            // Act
            services.AddTelemetry(configuration.GetSection("Telemetry"));

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            // Assert
            options.Value.ServiceName.Should().Be("ConfigService");
            options.Value.QueueCapacity.Should().Be(5000);
            options.Value.SamplingRatio.Should().Be(0.25);
        }

        [Fact]
        public void TelemetryOptions_Validate_ThrowsOnInvalidSettings()
        {
            // Arrange
            var options = new TelemetryOptions
            {
                ServiceName = "",
                QueueCapacity = -1
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void StaticInitialize_FirstCall_ReturnsTrue()
        {
            // Arrange & Act
            var result = Telemetry.Initialize(new TelemetryOptions
            {
                ServiceName = "TestService"
            });

            // Assert
            result.Should().BeTrue();
            Telemetry.IsInitialized.Should().BeTrue();

            // Cleanup
            Telemetry.Shutdown();
        }

        [Fact]
        public void StaticInitialize_SecondCall_ReturnsFalse()
        {
            // Arrange
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "Test" });

            // Act
            var result = Telemetry.Initialize(new TelemetryOptions { ServiceName = "Test2" });

            // Assert
            result.Should().BeFalse();

            // Cleanup
            Telemetry.Shutdown();
        }

        [Fact]
        public void StaticApi_WithoutInitialize_ThrowsException()
        {
            // Arrange - Ensure not initialized
            Telemetry.Shutdown();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                Telemetry.StartOperation("test"));
        }

        [Fact]
        public void StaticApi_AfterInitialize_Works()
        {
            // Arrange
            Telemetry.Initialize(new TelemetryOptions { ServiceName = "Test" });

            // Act
            using var operation = Telemetry.StartOperation("test-operation");
            Telemetry.TrackEvent("test-event");

            // Assert
            Telemetry.Statistics.ActivitiesCreated.Should().BeGreaterThan(0);

            // Cleanup
            Telemetry.Shutdown();
        }

        [Fact]
        public void TelemetryBuilder_Fluent_Configuration()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTelemetry(builder => builder
                .Configure(options =>
                {
                    options.ServiceName = "BuilderTest";
                    options.SamplingRatio = 0.75;
                })
                .AddActivitySource("CustomSource")
                .AddHttpInstrumentation());

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<TelemetryOptions>>();

            // Assert
            options.Value.ServiceName.Should().Be("BuilderTest");
            options.Value.ActivitySources.Should().Contain("CustomSource");
        }

        [Fact]
        public async Task HostedService_StartsAndStopsService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(options =>
            {
                options.ServiceName = "HostedTest";
            });

            var provider = services.BuildServiceProvider();
            var hostedServices = provider.GetServices<IHostedService>();

            // Act - Start
            foreach (var service in hostedServices)
            {
                await service.StartAsync(CancellationToken.None);
            }

            var telemetryService = provider.GetRequiredService<ITelemetryService>();

            // Assert - Service should be started
            telemetryService.Should().NotBeNull();

            // Act - Stop
            foreach (var service in hostedServices)
            {
                await service.StopAsync(CancellationToken.None);
            }
        }

        [Fact]
        public void Telemetry_ThreadSafe_Initialization()
        {
            // Arrange
            var initResults = new System.Collections.Concurrent.ConcurrentBag<bool>();
            var threadCount = 10;

            // Act - Multiple threads try to initialize simultaneously
            Parallel.For(0, threadCount, _ =>
            {
                var result = Telemetry.Initialize(new TelemetryOptions 
                { 
                    ServiceName = "ThreadTest" 
                });
                initResults.Add(result);
            });

            // Assert - Only one thread should have initialized successfully
            initResults.Count(r => r == true).Should().Be(1);
            initResults.Count(r => r == false).Should().Be(threadCount - 1);

            // Cleanup
            Telemetry.Shutdown();
        }
    }
}
```

### Integration Tests

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using FluentAssertions;

namespace HVO.Enterprise.Telemetry.Tests.Integration
{
    public class EndToEndInitializationTests
    {
        [Fact]
        public async Task DI_Mode_FullLifecycle()
        {
            // Arrange
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddTelemetry(options =>
                    {
                        options.ServiceName = "IntegrationTest";
                        options.WorkerCount = 2;
                    });
                })
                .Build();

            // Act - Start host
            await host.StartAsync();

            var telemetry = host.Services.GetRequiredService<ITelemetryService>();
            
            using (var operation = telemetry.StartOperation("test-operation"))
            {
                telemetry.TrackEvent("test-event");
                telemetry.RecordMetric("test-metric", 42.0);
            }

            // Assert
            var stats = telemetry.Statistics.GetSnapshot();
            stats.EventsRecorded.Should().BeGreaterThan(0);
            stats.MetricsRecorded.Should().BeGreaterThan(0);

            // Act - Stop host
            await host.StopAsync();
        }

        [Fact]
        public void Static_Mode_FullLifecycle()
        {
            // Arrange & Act
            Telemetry.Initialize(new TelemetryOptions
            {
                ServiceName = "StaticTest",
                QueueCapacity = 1000
            });

            using (Telemetry.BeginCorrelation())
            {
                using (var operation = Telemetry.StartOperation("test-operation"))
                {
                    Telemetry.TrackEvent("test-event");
                    Telemetry.RecordMetric("test-metric", 100.0);
                }
            }

            // Assert
            var stats = Telemetry.Statistics.GetSnapshot();
            stats.EventsRecorded.Should().BeGreaterThan(0);
            stats.MetricsRecorded.Should().BeGreaterThan(0);

            // Cleanup
            Telemetry.Shutdown();
            Telemetry.IsInitialized.Should().BeFalse();
        }
    }
}
```

## Performance Requirements

- **AddTelemetry() registration**: <5ms for all services
- **Static initialization**: <50ms cold start
- **Shutdown/flush**: Complete within 10 seconds
- **Memory overhead**: <10MB for telemetry infrastructure
- **Service resolution**: No additional overhead vs standard DI

## Dependencies

**Blocked By**:
- US-001: Core Package Setup
- US-002: Auto-Managed Correlation
- US-004: Bounded Queue Worker
- US-016: Statistics and Health Checks

**Blocks**:
- US-027/US-028: Sample applications (need initialization patterns)
- US-029: Project Documentation (setup guides)

## Definition of Done

- [x] `AddTelemetry()` extension methods implemented
- [x] `Telemetry.Initialize()` static API implemented
- [x] Both DI and static modes fully functional
- [x] Configuration validation working
- [x] All unit tests passing (>90% coverage)
- [x] Integration tests for both modes
- [x] Hosted service lifecycle tested
- [x] XML documentation complete
- [ ] Code reviewed and approved
- [x] Zero warnings in build

## Notes

### Design Decisions

1. **Why support both DI and static initialization?**
   - DI mode: Modern ASP.NET Core applications
   - Static mode: Legacy .NET Framework apps, console apps, WCF services
   - Some environments don't have DI containers available

2. **Why use IHostedService for DI mode?**
   - Automatic startup/shutdown integration
   - Works with generic host pattern
   - ASP.NET Core calls Start/Stop automatically

3. **Why singleton lifetime for all services?**
   - Telemetry should be single instance per application
   - State is shared globally (statistics, correlation)
   - Better performance than creating instances per request

### Implementation Tips

- Test both modes thoroughly
- Ensure thread-safe initialization in static mode
- Use AppDomain events for cleanup in static mode
- Validate configuration early (fail fast)
- Provide clear error messages for misconfiguration

### Usage Examples

#### ASP.NET Core (DI Mode)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Option 1: Simple configuration
builder.Services.AddTelemetry(options =>
{
    options.ServiceName = "MyApi";
    options.ServiceVersion = "1.0.0";
    options.Environment = builder.Environment.EnvironmentName;
});

// Option 2: From appsettings.json
builder.Services.AddTelemetry(
    builder.Configuration.GetSection("Telemetry"));

// Option 3: Builder pattern
builder.Services.AddTelemetry(telemetry => telemetry
    .Configure(options =>
    {
        options.ServiceName = "MyApi";
        options.SamplingRatio = 0.1;
    })
    .AddActivitySource("MyCompany.MyApi")
    .AddHttpInstrumentation());

var app = builder.Build();

// Use via DI
app.MapGet("/api/users", (ITelemetryService telemetry) =>
{
    using var operation = telemetry.StartOperation("GetUsers");
    // ... business logic
});

app.Run();
```

#### Console App / WCF Service (Static Mode)

```csharp
// Program.cs or Global.asax
class Program
{
    static void Main(string[] args)
    {
        // Initialize once at startup
        Telemetry.Initialize(new TelemetryOptions
        {
            ServiceName = "MyConsoleApp",
            ServiceVersion = "1.0.0"
        });

        try
        {
            DoWork();
        }
        finally
        {
            // Cleanup
            Telemetry.Shutdown();
        }
    }

    static void DoWork()
    {
        using (Telemetry.BeginCorrelation())
        {
            using (var operation = Telemetry.StartOperation("ProcessJob"))
            {
                Telemetry.TrackEvent("JobStarted");
                
                // ... business logic
                
                Telemetry.RecordMetric("ItemsProcessed", 100);
            }
        }
    }
}
```

## Related Documentation

- [Project Plan](../project-plan.md#18-dependency-injection-and-static-initialization)
- [Options Pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)
- [IHostedService](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice)

## Implementation Summary

**Completed**: 2025-07-17  
**Implemented by**: GitHub Copilot

### What Was Implemented

- **ITelemetryService** - Fleshed out from empty placeholder with `IsEnabled`, `Statistics`, `StartOperation`, `TrackException`, `TrackEvent`, `RecordMetric`, `Start`, `Shutdown`
- **TelemetryService** - Core concrete implementation with 3 constructors (DI, static, internal full). Orchestrates existing subsystems: `IOperationScopeFactory`, `TelemetryStatistics`, `TelemetryExceptionExtensions`. Returns `NoOpOperationScope` when disabled.
- **Static Telemetry API** - Major expansion of `Telemetry.cs` with `Initialize()` (3 overloads), `Shutdown()`, `IsInitialized`, `Statistics`, `StartOperation()`, `TrackException()`, `TrackEvent()`, `RecordMetric()`, `SetCorrelationId()`, `BeginCorrelation()`. Thread-safe with lock. AppDomain ProcessExit/DomainUnload hooks.
- **TelemetryServiceCollectionExtensions** - 3 `AddTelemetry()` overloads: `Action<TelemetryOptions>?`, `IConfiguration`, `Action<TelemetryBuilder>`. Idempotent registration. Composes `AddTelemetryLifetime()` and `AddTelemetryStatistics()`.
- **TelemetryBuilder** - Fluent API with `Configure()`, `AddActivitySource()`, `AddHttpInstrumentation()`
- **TelemetryOptionsValidator** - `IValidateOptions<TelemetryOptions>` delegating to existing `Validate()` method
- **TelemetryHostedService** - Bridges DI lifecycle to telemetry: `StartAsync` → `Start()` + `SetInstance()`, `StopAsync` → `Shutdown()` + `ClearInstance()`
- **CorrelationIdProvider** - Concrete `ICorrelationIdProvider` using `CorrelationContext`
- **NoOpOperationScope** - No-op `IOperationScope` for disabled telemetry
- **TelemetryOptions** - Extended with `ServiceName`, `ServiceVersion`, `Environment`, `ActivitySources`, `ResourceAttributes`

### Key Files

- `src/HVO.Enterprise.Telemetry/Abstractions/ITelemetryService.cs` - Unified telemetry interface
- `src/HVO.Enterprise.Telemetry/TelemetryService.cs` - Core service implementation
- `src/HVO.Enterprise.Telemetry/Telemetry.cs` - Static entry point (dual-mode)
- `src/HVO.Enterprise.Telemetry/TelemetryServiceCollectionExtensions.cs` - DI registration
- `src/HVO.Enterprise.Telemetry/TelemetryBuilder.cs` - Fluent builder
- `src/HVO.Enterprise.Telemetry/TelemetryHostedService.cs` - Hosted service bridge
- `src/HVO.Enterprise.Telemetry/Configuration/TelemetryOptionsValidator.cs` - Options validation
- `src/HVO.Enterprise.Telemetry/Correlation/CorrelationIdProvider.cs` - Correlation provider
- `src/HVO.Enterprise.Telemetry/Internal/NoOpOperationScope.cs` - No-op scope
- `tests/HVO.Enterprise.Telemetry.Tests/Initialization/` - 7 test files (~101 tests)

### Decisions Made

- Adapted to existing subsystems (`TelemetryBackgroundWorker`, `OperationScopeFactory`, `TelemetryStatistics`) rather than creating spec-proposed types (`ITelemetryQueue`, `IActivitySourceManager`) that don't exist
- Extended existing `TelemetryOptions` in `Configuration` namespace rather than creating new class in root namespace
- `TelemetryService` DI constructor uses `ITelemetryStatistics` (public interface) with runtime cast to `TelemetryStatistics` (internal class)
- Used explicit cast `(Action<TelemetryOptions>)` to resolve overload ambiguity between `IConfiguration` and `Action<TelemetryOptions>?` overloads
- `RecordException()` on static API works without initialization (fallback to `TelemetryExceptionExtensions`)
- Added `Microsoft.Extensions.Configuration.Binder` package for `IConfiguration.Bind()` support

### Quality Gates

- ✅ Build: 0 warnings, 0 errors
- ✅ Tests: 924/924 passed (804 telemetry + 120 common), 1 skipped
- ✅ XML documentation: Complete on all public APIs
- ✅ Thread safety: Verified with parallel initialization tests

### Next Steps

This story unblocks US-027 (NET48 Sample), US-028 (NET8 Sample), and US-029 (Project Documentation) which need initialization patterns.
