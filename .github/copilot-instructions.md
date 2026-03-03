# GitHub Copilot Instructions for HVO.Enterprise

## Project Overview

HVO.Enterprise is a modular .NET telemetry and logging library providing unified observability across all .NET platforms (.NET Framework 4.8 through .NET 10+). The solution includes:

- **HVO.Core** (NuGet from [HVO.SDK](https://github.com/RoySalisbury/HVO.SDK)) - Shared utilities and functional patterns (`Result<T>`, `Option<T>`, discriminated unions) used across all HVO projects
- **HVO.Enterprise.Telemetry** - Core telemetry library (distributed tracing, metrics, structured logging)
- **Extension Packages** - Platform-specific integrations (IIS, WCF, Serilog, App Insights, Datadog, Database)

The library targets .NET Standard 2.0 for single-binary deployment while supporting runtime-adaptive features for modern .NET versions. It standardizes logging and performance telemetry across diverse platforms including legacy WCF services, ASP.NET applications, and modern ASP.NET Core APIs.

> **Note:** Shared functional primitives (`Result<T>`, `Option<T>`, OneOf, guards, extensions) come from the external [HVO.Core](https://www.nuget.org/packages/HVO.Core) NuGet package published from the [HVO.SDK](https://github.com/RoySalisbury/HVO.SDK) repository. There is no `HVO.Common` source project in this repo — the `HVO.Common.Tests` and `HVO.Common.Benchmarks` projects test the `HVO.Core` NuGet package.

## Target Frameworks

- **Primary**: .NET 10 (latest features and performance)
- **Modern**: .NET 8+ (full modern C# feature set available)
- **Compatibility**: .NET Standard 2.0 (broad compatibility across .NET implementations)
- **Legacy**: .NET Framework 4.8.1 (support for legacy enterprise applications)

## Critical Compatibility Guidelines

### .NET 8+ Projects

When working with .NET 8+ projects:

- **Full modern C# features**: Use all latest C# language features including records, init properties, pattern matching, ranges, etc.
- **Modern APIs**: `ArgumentNullException.ThrowIfNull()`, `ArgumentException.ThrowIfNullOrEmpty()`, etc.
- **Nullable reference types**: Fully supported and encouraged
- **Implicit usings**: **DISABLED** by project convention - always add explicit `using` statements
- **System.Text.Json**: Preferred for JSON serialization
- **Span<T> and Memory<T>**: Use for high-performance scenarios
- **ValueTask<T>**: Preferred for hot paths and async methods that often complete synchronously

### .NET Standard 2.0 Projects

When working with .NET Standard 2.0 projects:

- **No modern C# shortcuts**: Avoid `ArgumentNullException.ThrowIfNull()` - use traditional null checks: `if (x == null) throw new ArgumentNullException(nameof(x));`
- **Nullable reference types**: Use nullable annotations (`?`) but ensure compatibility
- **Pattern matching**: Limited to C# 7.x features
- **Implicit usings**: **DISABLED** - always add explicit `using` statements
- **Required usings**: Always include `using System;` and other necessary namespaces explicitly
- **Language version**: Set `<LangVersion>latest</LangVersion>` but write compatible code
- **Avoid**: `^` and `..` range operators, `init` accessors for .NET Standard 2.0

### .NET Framework 4.8.1 Projects

When working with .NET Framework 4.8.1 projects:

- **No modern C# features**: Limited to C# 7.3 and below
- **Nullable reference types**: Generally not supported
- **Async/await**: Supported but requires `System.Threading.Tasks` references
- **JSON**: Use `Newtonsoft.Json` instead of `System.Text.Json`
- **HTTP clients**: Use `System.Net.Http` with explicit package references

## Project Structure

```
HVO.Enterprise/
├── src/                          # Source code
│   └── HVO.Enterprise.Telemetry/  # Core telemetry library (.NET Standard 2.0)
├── tests/                        # Unit and integration tests
│   ├── HVO.Common.Tests/         # Tests for HVO.Core NuGet package
│   └── HVO.Enterprise.Telemetry.Tests/
├── benchmarks/                   # Performance benchmarks
│   └── HVO.Common.Benchmarks/    # Benchmarks for HVO.Core NuGet package
├── docs/                         # Documentation
└── .github/                      # GitHub workflows and configuration
```

## Coding Standards

### General Principles

- **Explicit is better than implicit**: Always use explicit types and clear names
- **Immutability preferred**: Use `readonly` structs and fields where possible
- **Functional patterns**: Leverage `Result<T>`, `Option<T>`, and discriminated unions
- **XML documentation**: All public APIs must have complete XML documentation comments
- **No warnings**: Code must build with zero warnings

### Naming Conventions

- **PascalCase**: Classes, methods, properties, public fields, namespaces
- **camelCase**: Private fields (with underscore prefix: `_fieldName`), parameters, local variables
- **UPPER_CASE**: Constants only when truly constant values
- **Async methods**: Suffix with `Async` (e.g., `GetDataAsync`)
- **Namespace convention**: 
  - `HVO.Core.*` for general-purpose utilities (published from HVO.SDK as NuGet)
  - `HVO.Enterprise.*` for Enterprise telemetry and related projects

### Code Organization

- **One class per file**: Unless nested/private classes
- **Namespace matches folder structure**: 
  - `HVO.Core.*` namespaces come from the HVO.Core NuGet package (do not create local source)
  - `HVO.Enterprise.ProjectName.FolderName` for Enterprise projects
- **File organization**: usings → namespace → class members in logical groups
- **Member ordering**: Constants → fields → properties → constructors → methods

### Error Handling Patterns

This codebase uses functional error handling patterns:

```csharp
// Use Result<T> for operations that can fail
using HVO.Core.Results;

public Result<Customer> GetCustomer(int id)
{
    try
    {
        var customer = _repository.Find(id);
        return customer ?? Result<Customer>.Failure(new NotFoundException());
    }
    catch (Exception ex)
    {
        return ex; // Implicit conversion to Result<Customer>
    }
}

// Use Option<T> for values that may not exist
public Option<string> GetConfigValue(string key)
{
    return _config.TryGetValue(key, out var value) 
        ? new Option<string>(value) 
        : Option<string>.None();
}
```

### Common Patterns

1. **Result Pattern**: Use `Result<T>` instead of throwing exceptions for expected error cases
2. **Option Pattern**: Use `Option<T>` instead of nullable returns when dealing with optional values
3. **Discriminated Unions**: Use `IOneOf` implementations for type-safe variants
4. **Extension Methods**: Group related extensions in dedicated classes (e.g., `EnumExtensions`)

## Dependency Injection Patterns

### Library Design for DI
- Design libraries to work with DI containers while remaining usable without DI
- Use **constructor injection** for required dependencies
- Accept optional `ILogger<T>?` parameters with fallback creation when not provided
- Provide both static APIs (`Telemetry.Initialize()`) and DI-based APIs (`ITelemetryService`)
- Create `IServiceCollection` extension methods for easy registration:
  ```csharp
  public static IServiceCollection AddTelemetry(
      this IServiceCollection services,
      Action<TelemetryOptions>? configure = null)
  {
      services.AddSingleton<ITelemetryService, TelemetryService>();
      services.AddSingleton<ICorrelationContext, CorrelationContext>();
      if (configure != null)
          services.Configure(configure);
      return services;
  }
  ```

### Constructor Injection Best Practices
- Prefer interfaces for testability
- Use optional parameters for optional dependencies
- Validate required dependencies early (throw `ArgumentNullException` in constructor)
- Consider using the Options pattern (`IOptions<T>`) for configuration

### Registration Patterns
- Use extension methods: `.AddTelemetry()`, `.WithDatadogExporter()`, `.WithLoggingEnrichment()`
- Chain extension methods for fluent configuration
- Register services in appropriate lifetime (Singleton for telemetry, Scoped for request-bound services)
- Document DI availability across frameworks (.NET Framework 4.6.1+ via NuGet, .NET Core 2.0+ built-in)

## Structured Logging Guidelines

### ILogger<T> Usage in Library Code
- All library classes should accept optional `ILogger<T>?` constructor parameter
- Create fallback logger if not provided: `ILogger.CreateLogger<T>()` or `NullLogger<T>.Instance`
- Use structured logging with named parameters:
  ```csharp
  _logger.LogInformation("Processing operation {OperationName} for user {UserId}", 
      operationName, userId);
  ```
- Never use string interpolation or concatenation in log messages
- Use `LoggerMessage.Define` for high-performance logging in hot paths

### Log Level Guidelines
- **Trace**: High-frequency operations (timers, GPIO toggles, tight loops) - typically disabled in production
- **Debug**: Operational state changes, method entry/exit, configuration changes - enabled during troubleshooting
- **Information**: Important business events, startup/shutdown, major state transitions - always enabled
- **Warning**: Recoverable errors, configuration issues, performance concerns - requires attention
- **Error**: Exceptions and unrecoverable errors with full context - immediate attention
- **Critical**: System-level failures requiring immediate intervention - paging/alerting

### Structured Logging Best Practices
- Use consistent property names across the codebase (e.g., `OperationId`, `CorrelationId`, `UserId`)
- Include context properties for filtering and searching
- Log exceptions with context: `_logger.LogError(ex, "Failed to process {OperationId}", operationId)`
- Avoid logging sensitive data (passwords, tokens, credit cards)
- Use log scopes for contextual information:
  ```csharp
  using (_logger.BeginScope(new Dictionary<string, object>
  {
      ["OperationId"] = operationId,
      ["UserId"] = userId
  }))
  {
      _logger.LogInformation("Processing operation");
  }
  ```

### Replace Debug.WriteLine
- **Never** use `Debug.WriteLine` or `Console.WriteLine` in library code
- Always use `ILogger` for all diagnostic output
- This enables consumers to control logging behavior and routing

## Configuration Management

### Strongly-Typed Configuration
- Use `IOptions<T>` pattern for all configuration:
  ```csharp
  public class TelemetryOptions
  {
      [Required]
      [Range(0.0, 1.0)]
      public double DefaultSamplingRate { get; set; } = 0.1;
      
      [Required]
      public DetailLevel DefaultDetailLevel { get; set; } = DetailLevel.Normal;
      
      public int QueueCapacity { get; set; } = 10000;
  }
  ```

### Configuration Validation
- Use data annotations for simple validation (`[Required]`, `[Range]`, etc.)
- Implement `IValidateOptions<T>` for complex validation logic
- Use `ValidateOnStart()` to fail fast on invalid configuration:
  ```csharp
  services.AddOptions<TelemetryOptions>()
      .Bind(configuration.GetSection("Telemetry"))
      .ValidateDataAnnotations()
      .ValidateOnStart();
  ```

### Configuration Precedence
- Follow precedence order: call-level > method-level > type-level > global
- Support hot reload for production troubleshooting
- Provide diagnostic APIs: `GetEffectiveConfiguration(Type, MethodInfo)`
- Document configuration sources and precedence clearly

## Exception Handling and Error Patterns

### When to Use Result<T> vs Exceptions
- **Use `Result<T>`** for expected failures and business logic errors:
  - Validation failures
  - Business rule violations
  - Expected external service failures
  - "Not found" scenarios
- **Use exceptions** for unexpected failures and system errors:
  - Programming errors (null reference, index out of range)
  - Infrastructure failures (database connection lost)
  - Unrecoverable errors
  - Resource exhaustion

### Result<T> Pattern
```csharp
public Result<Customer> GetCustomer(int id)
{
    try
    {
        if (id <= 0)
            return Result<Customer>.Failure(
                new ValidationException("Invalid customer ID"));
        
        var customer = _repository.Find(id);
        if (customer == null)
            return Result<Customer>.Failure(
                new NotFoundException($"Customer {id} not found"));
        
        return Result<Customer>.Success(customer);
    }
    catch (Exception ex)
    {
        return ex; // Implicit conversion to Result<Customer>
    }
}

// Usage
var result = GetCustomer(customerId);
if (result.IsSuccessful)
{
    var customer = result.Value;
}
else
{
    _logger.LogError(result.Error, "Failed to get customer {CustomerId}", customerId);
}
```

### Result<T, TEnum> for Typed Error Codes
```csharp
public enum ValidationError
{
    [Description("Invalid input format")]
    InvalidFormat,
    
    [Description("Required field missing")]
    RequiredFieldMissing
}

public Result<Order, ValidationError> ValidateOrder(OrderRequest request)
{
    if (string.IsNullOrEmpty(request.CustomerName))
        return ValidationError.RequiredFieldMissing;
    
    return Result<Order, ValidationError>.Success(order);
}
```

### Exception Wrapping and Rethrowing
- Preserve stack traces using `ExceptionDispatchInfo`:
  ```csharp
  catch (Exception ex)
  {
      var exceptionInfo = ExceptionDispatchInfo.Capture(ex);
      // Do work
      exceptionInfo.Throw(); // Preserves original stack trace
  }
  ```
- Use `throw;` to rethrow without losing stack trace
- Add context when wrapping exceptions:
  ```csharp
  catch (SqlException ex)
  {
      throw new DatabaseException($"Failed to query customer {customerId}", ex);
  }
  ```

## Performance Considerations

### Performance Budget for Telemetry
- Activity start: ~5-30ns (depending on sampling)
- Property addition: <10ns (fast-path primitives)
- Operation Dispose: ~1-5μs (synchronous timing calculation)
- Background work: non-blocking (JSON, exporters)
- Total overhead target: <100ns per operation (excluding Dispose)

### Memory Optimization
- Use `readonly struct` for small value types
- Use `Span<T>` and `Memory<T>` in .NET 8+ projects for buffer operations
- Prefer `ValueTask<T>` for hot paths when targeting .NET 8+
- Use `ArrayPool<T>` for temporary array allocations
- Avoid allocations in tight loops
- Consider `StackAlloc` for small, temporary buffers in .NET 8+

### Async Patterns
- Prefer `async/await` over `.Result` or `.Wait()`
- Use `ConfigureAwait(false)` in library code
- Use `ValueTask<T>` for frequently-called async methods that often complete synchronously
- Cache Task results when appropriate

### Caching Strategies
- Cache reflection results (`MethodInfo`, `PropertyInfo`, `Attribute`) in `ConcurrentDictionary`
- Use `Lazy<T>` for thread-safe initialization
- Implement proper cache expiration for long-running services
- Consider memory pressure when caching large objects

### Profiling Tools
- Use **dotnet-counters** for real-time metrics viewing
- Use **dotnet-trace** for performance investigation
- Use **BenchmarkDotNet** for micro-benchmarks
- Profile memory with **dotnet-gcdump** and Visual Studio diagnostics

## Testing Guidelines

### Multi-Framework Testing
- Test .NET 8+ projects with modern APIs
- Test .NET Standard 2.0 projects for compatibility
- Use runtime detection tests for feature availability
- Verify behavior on both .NET Framework 4.8 and .NET 8+

### Testing Frameworks
- **MSTest** or **xUnit** preferred for unit tests
- **Moq** for service mocking and test isolation
- Follow **Arrange-Act-Assert** pattern consistently

### Test Naming
- Use descriptive names: `MethodName_Scenario_ExpectedBehavior`
  - `TrackOperation_WithException_RecordsErrorMetrics`
  - `CorrelationContext_AsyncFlow_PreservesId`
  - `SamplingDecision_BelowThreshold_SkipsActivity`

### Test Patterns
```csharp
[TestClass]
public class TelemetryServiceTests
{
    [TestMethod]
    public async Task TrackOperation_WithValidName_CreatesActivity()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TelemetryService>>();
        var service = new TelemetryService(mockLogger.Object);
        
        // Act
        using (var operation = service.TrackOperation("TestOperation"))
        {
            operation.AddProperty("key", "value");
        }
        
        // Assert
        Assert.IsNotNull(Activity.Current);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
    
    [DataTestMethod]
    [DataRow(0.0, false)]
    [DataRow(0.5, null)] // Random
    [DataRow(1.0, true)]
    public void SamplingDecision_VariousRates_BehavesCorrectly(
        double rate, bool? expected)
    {
        // Test parameterized scenarios
    }
}
```

### Integration Testing
- Mock external dependencies (databases, HTTP clients, message queues)
- Test correlation flow across service boundaries
- Verify telemetry data is correctly exported
- Test configuration hot reload scenarios
- Verify lifecycle management (startup, shutdown, AppDomain unload)

### Test Coverage
- Aim for >85% code coverage on business logic
- Focus on edge cases and error paths
- Test both success and failure scenarios
- Include property-based tests for complex logic

## XML Documentation Requirements

All public members require XML documentation:

```csharp
/// <summary>
/// Brief description of what this does
/// </summary>
/// <typeparam name="T">Description of type parameter</typeparam>
/// <param name="parameter">Description of parameter</param>
/// <returns>Description of return value</returns>
/// <exception cref="ExceptionType">When this exception is thrown</exception>
public T Method<T>(string parameter) { }
```

## Build Configuration

- **Nullable**: Enabled across all projects
- **ImplicitUsings**: **Disabled** - explicit usings required
- **TreatWarningsAsErrors**: Should be true for production builds
- **GenerateDocumentationFile**: Enabled for library projects
- **LangVersion**: `latest` (but respect framework limitations)

## Package References

### .NET Standard 2.0 Projects

- Use `System.Text.Json` 8.0.5+ for JSON serialization
- Use `System.Threading.Tasks.Extensions` for ValueTask support
- Avoid packages that require .NET Core/.NET 5+

### .NET Framework 4.8.1 Projects

- Prefer `Newtonsoft.Json` for JSON
- Use explicit package references for BCL types
- Consider polyfill packages for modern features

## Testing Guidelines

- **Unit tests**: xUnit or NUnit preferred
- **Test naming**: `MethodName_Scenario_ExpectedBehavior`
- **Arrange-Act-Assert**: Clear separation in test methods
- **Test coverage**: Aim for >80% coverage on business logic

## Common Gotchas

1. **System.Text.Json in .NET Standard 2.0**: Works but needs explicit package reference
2. **ArgumentNullException.ThrowIfNull**: Not available in .NET Standard 2.0
3. **Init-only properties**: Not fully supported in .NET Standard 2.0
4. **Default interface implementations**: Not available in .NET Standard 2.0
5. **Records**: Not available in .NET Standard 2.0

## Performance Considerations

- Use `readonly struct` for small value types
- Prefer `ValueTask<T>` for hot paths when targeting .NET Core/5+
- Use `Span<T>` and `Memory<T>` in .NET Core/5+ projects
- Avoid allocations in tight loops
- Consider `StackAlloc` for small, temporary buffers in .NET Core/5+

## Git Workflow

- **Branch naming**: `feature/description`, `fix/description`, `docs/description`
- **Commit messages**: Clear, descriptive, present tense
- **PR requirements**: Build successful, all tests passing, zero warnings

### Conventional Commits
Follow conventional commits format: `type(scope): description`

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `perf`: Performance improvements

**Examples:**
```
feat(telemetry): add background job correlation support
fix(common): correct Result<T> exception handling
docs(readme): update installation instructions
refactor(metrics): simplify event counter implementation
test(correlation): add AsyncLocal flow tests
```

### Feature Branch Workflow
1. Create feature branch from `main`
2. Make changes with conventional commits
3. Ensure all tests pass and build has zero warnings
4. Write descriptive PR descriptions with context
5. Squash commits when merging to maintain clean history

## Security Guidelines

### Secrets Management
- **Never commit secrets** to version control
- Use User Secrets for local development (dotnet user-secrets)
- Use environment variables or Azure Key Vault in production
- Redact sensitive data in logs and telemetry
- Document PII considerations for user/request context enrichment

### Input Validation
- Validate all public API inputs
- Use data annotations or `IValidatableObject` for model validation
- Sanitize user input before logging
- Use parameterized queries (Entity Framework handles this automatically)

### Sensitive Data Detection
- Implement pattern-based detection for sensitive fields:
  - password, token, apikey, secret, ssn, creditcard, authorization
- Respect `[Sensitive]` attribute for automatic redaction
- Configure capture levels appropriately (NameOnly for sensitive operations)
- Emit warnings when capturing potentially sensitive data

### Library Security Practices
- Minimize dependencies to reduce attack surface
- Use secure defaults (e.g., sampling disabled for sensitive operations)
- Provide opt-in for features with security implications
- Document security considerations for each feature
- Follow OWASP guidelines for library development

## Common Pattern Examples

### Using Result<T> in Services
```csharp
using HVO.Core.Results;

public class CustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly ILogger<CustomerService> _logger;
    
    public Result<Customer> GetCustomer(int customerId)
    {
        try
        {
            if (customerId <= 0)
            {
                return Result<Customer>.Failure(
                    new ArgumentException("Customer ID must be positive"));
            }
            
            var customer = _repository.Find(customerId);
            if (customer == null)
            {
                _logger.LogWarning("Customer {CustomerId} not found", customerId);
                return Result<Customer>.Failure(
                    new NotFoundException($"Customer {customerId} not found"));
            }
            
            return Result<Customer>.Success(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve customer {CustomerId}", customerId);
            return ex; // Implicit conversion
        }
    }
    
    public async Task<Result<bool>> DeleteCustomerAsync(int customerId)
    {
        try
        {
            var customerResult = GetCustomer(customerId);
            if (!customerResult.IsSuccessful)
                return Result<bool>.Failure(customerResult.Error!);
            
            await _repository.DeleteAsync(customerId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
```

### Using Option<T> for Optional Values
```csharp
using HVO.Core.Options;

public class ConfigurationService
{
    private readonly Dictionary<string, string> _config;
    
    public Option<string> GetSetting(string key)
    {
        if (_config.TryGetValue(key, out var value))
            return new Option<string>(value);
        
        return Option<string>.None();
    }
    
    public string GetSettingOrDefault(string key, string defaultValue)
    {
        var option = GetSetting(key);
        return option.HasValue ? option.Value! : defaultValue;
    }
}
```

### IOneOf for Discriminated Unions
```csharp
using HVO.Core.OneOf;

// Define the union type
public interface IPaymentResult : IOneOf { }

public class SuccessfulPayment : IPaymentResult
{
    public string TransactionId { get; set; } = "";
    public decimal Amount { get; set; }
    public object? Value => this;
    public Type? ValueType => typeof(SuccessfulPayment);
    public JsonElement? RawJson => null;
}

public class FailedPayment : IPaymentResult
{
    public string ErrorCode { get; set; } = "";
    public string Message { get; set; } = "";
    public object? Value => this;
    public Type? ValueType => typeof(FailedPayment);
    public JsonElement? RawJson => null;
}

// Usage
public IPaymentResult ProcessPayment(PaymentRequest request)
{
    if (request.Amount <= 0)
    {
        return new FailedPayment
        {
            ErrorCode = "INVALID_AMOUNT",
            Message = "Amount must be positive"
        };
    }
    
    return new SuccessfulPayment
    {
        TransactionId = Guid.NewGuid().ToString(),
        Amount = request.Amount
    };
}

// Pattern matching
var result = ProcessPayment(request);
if (result.Is<SuccessfulPayment>())
{
    var payment = result.As<SuccessfulPayment>();
    Console.WriteLine($"Success: {payment.TransactionId}");
}
else if (result.Is<FailedPayment>())
{
    var failure = result.As<FailedPayment>();
    Console.WriteLine($"Failed: {failure.ErrorCode}");
}
```

### Extension Method Patterns
```csharp
// Group related extensions in dedicated classes
namespace HVO.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }
        
        public static string Truncate(this string value, int maxLength)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
    
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            // Implementation shown in HVO.Core
        }
    }
}
```

### Telemetry Integration Patterns
```csharp
// Manual instrumentation with dispose pattern
public class OrderService
{
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<OrderService> _logger;
    
    public async Task<Result<Order>> CreateOrderAsync(OrderRequest request)
    {
        using (var operation = _telemetry.TrackOperation(
            "OrderService.CreateOrder",
            detailLevel: DetailLevel.Detailed))
        {
            operation.AddProperty("customerId", request.CustomerId);
            operation.AddProperty("itemCount", request.Items.Count);
            
            try
            {
                _logger.LogInformation(
                    "Creating order for customer {CustomerId}", 
                    request.CustomerId);
                
                var order = await ProcessOrderAsync(request);
                
                operation.AddProperty("orderId", order.Id);
                operation.AddProperty("totalAmount", order.TotalAmount);
                
                return Result<Order>.Success(order);
            }
            catch (Exception ex)
            {
                operation.SetException(ex);
                _logger.LogError(ex, 
                    "Failed to create order for customer {CustomerId}", 
                    request.CustomerId);
                return ex;
            }
        }
    }
}

// Attribute-based instrumentation
[TelemetryOptions(DetailLevel = DetailLevel.Normal)]
public interface IReservationService
{
    [TelemetryOptions(
        DetailLevel = DetailLevel.Detailed,
        CaptureParameters = CaptureLevel.Values)]
    Task<Reservation> GetReservationAsync(int id);
    
    [NoTelemetry]
    Task<bool> HealthCheckAsync();
}
```

## User Story Workflow Pattern

### CRITICAL: Always Follow This Workflow

When working on user stories, **ALWAYS** follow this pattern:

#### Before Starting Work

1. **Verify Build and Tests**
   - Run `dotnet build` - ensure 0 warnings/errors
    - Run tests manually per project (do not rely on IDE test runner):
      - `dotnet test tests/HVO.Common.Tests/HVO.Common.Tests.csproj`
      - `dotnet test tests/HVO.Enterprise.Telemetry.Tests/HVO.Enterprise.Telemetry.Tests.csproj`
   - Confirm clean baseline before making changes

2. **Create Feature Branch**
   - Branch from `main`: `git checkout -b feature/us-XXX-short-description`
   - Example: `git checkout -b feature/us-004-bounded-queue`
   - **NEVER work directly on main branch**

3. **Update User Story Markdown**
   - Add GitHub issue number at top: `**GitHub Issue**: [#N](https://github.com/RoySalisbury/HVO.Enterprise/issues/N)`
   - Change status to `🚧 In Progress`
   - Add start date if tracking sprints

4. **Update GitHub Issue**
   - Comment on issue: "Starting work on US-XXX. Branch: feature/us-XXX-short-description"
   - Add label: `status:in-progress`
   - Assign to yourself if working in team

5. **Begin Implementation**
   - Write code following project standards
   - Commit frequently with conventional commits
   - Keep changes focused on the user story scope

#### After Completing Work

1. **Verify Build and Tests**
   - Run `dotnet build` - ensure 0 warnings/errors
    - Run tests manually per project (do not rely on IDE test runner):
      - `dotnet test tests/HVO.Common.Tests/HVO.Common.Tests.csproj`
      - `dotnet test tests/HVO.Enterprise.Telemetry.Tests/HVO.Enterprise.Telemetry.Tests.csproj`
   - Fix any issues before proceeding

2. **Update User Story Markdown**
   - Change status to `✅ Complete`
   - Mark all acceptance criteria checkboxes as `[x]`
   - Add implementation summary section (see template below)
   - List key files created/modified
   - Document important decisions made

3. **Commit All Changes**
   - `git add -A`
   - `git commit -m "feat(scope): implement US-XXX description"`
   - Follow conventional commits format

4. **Push Branch and Create Pull Request**
   - `git push origin feature/us-XXX-short-description`
   - `gh pr create --title "feat: US-XXX - Description" --body "[PR description]" --base main`
   - Link PR to issue: Include "Closes #N" in PR description
   - Request review if working in team

5. **Update GitHub Issue**
   - Comment: "Implementation complete. PR #N created for review."
   - Add link to PR
   - Update with test results summary

6. **After PR Merged**
   - GitHub will auto-close the issue (via "Closes #N" in PR)
   - Verify issue is closed
   - Delete feature branch: `git branch -d feature/us-XXX-short-description`
   - Pull latest main: `git checkout main && git pull`

### GitHub Issue Number Convention

**CRITICAL**: Always reference both the GitHub issue number AND user story number together:
- ✅ CORRECT: "Working on GitHub Issue #5 (US-003)"
- ✅ CORRECT: "Closing Issue #7 for US-005"
- ❌ WRONG: "Closing US-003" (ambiguous - is that issue #3 or issue #5?)
- ❌ WRONG: "Issue 5" without mentioning US-003

The mapping is: **GitHub Issue # = User Story # + 2** (because issues #1 and #2 were infrastructure setup)

### User Story Documentation Requirements

**IMPORTANT**: When completing any user story, you MUST update the corresponding documentation in `docs/user-stories/`.

#### Required Updates in Markdown File

1. **Add GitHub Issue Number** (at top of file)
   - Add line after title: `**GitHub Issue**: [#N](https://github.com/RoySalisbury/HVO.Enterprise/issues/N)`
   - This prevents confusion between issue numbers and user story numbers

2. **Update User Story Status**
   - Before work: `🚧 In Progress`
   - After completion: `✅ Complete`
   - Update the status date if present

3. **Mark Acceptance Criteria as Complete** (when done)
   - Change all checkboxes from `[ ]` to `[x]` for completed items
   - Add implementation notes if relevant

4. **Add Implementation Summary** (at end of document, when done)
   - Brief description of what was implemented
   - Key decisions made during implementation
   - Any deviations from the original plan (with justification)
   - Links to relevant code files or classes
   - Any known limitations or future considerations

5. **Update Related Documentation** (when done)
   - If the story mentions updating other docs (README, project-plan, etc.), ensure those are updated
   - Cross-reference related user stories that were blocked or are now unblocked

### Implementation Summary Template

Add this section at the end of the user story markdown file when work is complete:

```markdown
## Implementation Summary

**Completed**: 2026-02-07  
**Implemented by**: GitHub Copilot

### What Was Implemented
- Created HVO.Enterprise.Telemetry.csproj targeting .NET Standard 2.0
- Added all 8 required dependencies with specified versions
- Created 13 folder structure directories with .gitkeep files
- Added compatibility test project for .NET 8

### Key Files
- `src/HVO.Enterprise.Telemetry/HVO.Enterprise.Telemetry.csproj`
- `src/HVO.Enterprise.Telemetry/Abstractions/ITelemetryService.cs`
- `tests/HVO.Enterprise.Telemetry.Tests/`

### Decisions Made
- Used .NET Standard 2.0 only (no multi-targeting) per project requirements
- Added placeholder ITelemetryService interface for test compatibility
- Created .NET 8 test project to verify dependency resolution

### Quality Gates
- ✅ Build: 0 warnings, 0 errors
- ✅ Tests: 84/84 passed
- ✅ Code Review: No issues
- ✅ Security: 0 vulnerabilities

### Next Steps
This story unblocks US-002 through US-018.
```

### Checklist Before Creating PR

- [ ] All builds successful (0 warnings, 0 errors)
- [ ] All tests passing (including new tests)
- [ ] Update story status to `✅ Complete` in markdown file
- [ ] Mark all acceptance criteria checkboxes as `[x]`
- [ ] Add implementation summary section to markdown
- [ ] Update any related documentation referenced in the story
- [ ] Commit all changes with conventional commit message
- [ ] Push feature branch to GitHub
- [ ] Create PR with "Closes #N" in description
- [ ] Add comment to GitHub issue with PR link

## Tooling Availability Policy

### Missing CLI Tools
- When a command-line tool is missing, install it via dev container provisioning scripts
- Document supported alternatives in README until tool is installed
- Avoid repeatedly invoking known-missing commands
- Switch to confirmed binary (e.g., `python3` instead of `python`) until alias is installed

### Dev Container Maintenance
- After installing a new tool, update `.devcontainer/post-create.sh` (or equivalent)
- Ensure future containers match the current environment
- Document tool versions and installation sources
- Keep provisioning scripts idempotent and fast

### Tool Documentation
- Maintain list of required tools in README
- Document purpose and usage of each tool
- Provide installation instructions for local development
- Note any platform-specific requirements

## Additional Notes

When suggesting code:
1. **Check the project's target framework first**
2. **.NET 8+**: Use all modern C# features and APIs without restriction
3. **.NET Standard 2.0**: Use C# 7.x compatible syntax, traditional null checks, explicit usings
4. **.NET Framework 4.8.1**: Use C# 7.3 or below, Newtonsoft.Json for JSON
5. Always include necessary using statements (implicit usings are disabled)
6. Provide XML documentation for public APIs
7. Follow the established patterns in the codebase (Result<T>, Option<T>, IOneOf)

## Dev Container Tool Policy

The dev container does **not** include Node.js, Python, or Azure CLI. Do **not** attempt to use these tools or suggest installing them.

- **Scripting & automation**: Use `bash`/`zsh` shell scripts, `gh` CLI, or `dotnet` CLI
- **JSON processing**: Use `jq` (installed) or .NET `System.Text.Json`
- **Issue/PR management**: Use `gh issue create`, `gh pr create`, etc.
- **Search**: Use `rg` (ripgrep, installed) for text search
- **EF Core / database CLI**: Not installed — use `dotnet` CLI or EF via code
- **Never** suggest `npm`, `npx`, `pip`, `python`, `az`, or `dotnet-script` commands

### Heredoc / Multi-Line String Warning

**Do NOT use `cat << 'EOF'` or any heredoc syntax in terminal commands.** Heredocs are unreliable in this environment — content frequently gets corrupted, garbled, or truncated. Instead:

1. Write multi-line content to a file using the file-creation tool (e.g., `create_file`).
2. Reference that file in the terminal command (e.g., `gh issue create --body-file /tmp/issue-body.md`).

This applies to **all** cases where you need to pass multi-line text to a CLI command.
