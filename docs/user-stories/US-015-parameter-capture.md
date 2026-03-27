# US-015: Parameter Capture

**GitHub Issue**: [#59](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/59)  
**Status**: ✅ Complete  
**Category**: Core Package  
**Effort**: 5 story points  
**Sprint**: 5

## Description

As a **developer instrumenting operations**,  
I want **intelligent parameter capture with tiered verbosity levels and automatic sensitive data detection**,  
So that **I can debug issues with detailed telemetry while protecting sensitive information and controlling overhead**.

## Acceptance Criteria

1. **Tiered Capture Levels**
   - [x] `None` - No parameter capture
   - [x] `Minimal` - Capture only primitive types and strings
   - [x] `Standard` - Capture primitives, strings, and simple collections
   - [x] `Verbose` - Capture complex objects with property traversal
   - [x] Per-parameter and per-operation configuration

2. **Sensitive Data Detection**
   - [x] Built-in patterns for common PII (SSN, credit cards, emails, phone numbers)
   - [x] Attribute-based marking (`[SensitiveData]`)
   - [x] Naming convention detection (e.g., "password", "ssn", "creditCard")
   - [x] Custom pattern registration
   - [x] Configurable redaction strategies

3. **Type Support**
   - [x] Primitive types (int, string, bool, DateTime, etc.)
   - [x] Collections (arrays, lists, dictionaries)
   - [x] Complex objects (with property traversal)
   - [x] Custom `ToString()` implementations
   - [x] JSON serialization fallback

4. **Performance**
   - [x] Minimal overhead - capture only when requested
   - [x] Lazy evaluation support
   - [x] Configurable depth limits for nested objects
   - [x] Configurable size limits for collections
   - [x] No allocations for disabled capture

5. **Integration**
   - [x] Works with `IOperationScope`
   - [x] Works with `Activity` tags
   - [x] Works with DispatchProxy instrumentation
   - [x] Works with manual instrumentation

## Technical Requirements

### Core API

```csharp
namespace HVO.Enterprise.Telemetry.Capture
{
    /// <summary>
    /// Defines parameter capture verbosity levels.
    /// </summary>
    public enum CaptureLevel
    {
        /// <summary>No parameter capture.</summary>
        None = 0,
        
        /// <summary>Capture only primitive types and strings.</summary>
        Minimal = 1,
        
        /// <summary>Capture primitives, strings, and simple collections.</summary>
        Standard = 2,
        
        /// <summary>Capture complex objects with property traversal.</summary>
        Verbose = 3
    }
    
    /// <summary>
    /// Interface for capturing method parameters with sensitivity awareness.
    /// </summary>
    public interface IParameterCapture
    {
        /// <summary>
        /// Captures a parameter value based on configured capture level.
        /// </summary>
        object? CaptureParameter(
            string parameterName,
            object? value,
            Type parameterType,
            ParameterCaptureOptions options);
        
        /// <summary>
        /// Captures multiple parameters.
        /// </summary>
        IDictionary<string, object?> CaptureParameters(
            ParameterInfo[] parameters,
            object?[] values,
            ParameterCaptureOptions options);
        
        /// <summary>
        /// Registers a custom sensitive data pattern.
        /// </summary>
        void RegisterSensitivePattern(string pattern, RedactionStrategy strategy);
        
        /// <summary>
        /// Checks if a parameter name indicates sensitive data.
        /// </summary>
        bool IsSensitive(string parameterName);
    }
    
    /// <summary>
    /// Options for configuring parameter capture behavior.
    /// </summary>
    public sealed class ParameterCaptureOptions
    {
        /// <summary>
        /// The capture level to use.
        /// </summary>
        public CaptureLevel Level { get; set; } = CaptureLevel.Standard;
        
        /// <summary>
        /// Whether to automatically detect and redact sensitive data.
        /// </summary>
        public bool AutoDetectSensitiveData { get; set; } = true;
        
        /// <summary>
        /// Default redaction strategy for sensitive data.
        /// </summary>
        public RedactionStrategy RedactionStrategy { get; set; } = RedactionStrategy.Mask;
        
        /// <summary>
        /// Maximum depth for traversing nested objects.
        /// </summary>
        public int MaxDepth { get; set; } = 2;
        
        /// <summary>
        /// Maximum number of items to capture from collections.
        /// </summary>
        public int MaxCollectionItems { get; set; } = 10;
        
        /// <summary>
        /// Maximum string length before truncation.
        /// </summary>
        public int MaxStringLength { get; set; } = 1000;
        
        /// <summary>
        /// Whether to use custom ToString() implementations.
        /// </summary>
        public bool UseCustomToString { get; set; } = true;
        
        /// <summary>
        /// Whether to capture property names for complex objects.
        /// </summary>
        public bool CapturePropertyNames { get; set; } = true;
        
        /// <summary>
        /// Custom type serializers.
        /// </summary>
        public Dictionary<Type, Func<object, object?>>? CustomSerializers { get; set; }
    }
    
    /// <summary>
    /// Strategies for redacting sensitive data in captured parameters.
    /// </summary>
    public enum RedactionStrategy
    {
        /// <summary>Remove the value entirely.</summary>
        Remove,
        
        /// <summary>Replace with "***".</summary>
        Mask,
        
        /// <summary>Replace with SHA256 hash.</summary>
        Hash,
        
        /// <summary>Show first/last characters only (e.g., "ab***yz").</summary>
        Partial,
        
        /// <summary>Replace with type name.</summary>
        TypeName
    }
}
```

### Implementation

```csharp
namespace HVO.Enterprise.Telemetry.Capture
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Security.Cryptography;
    
    /// <summary>
    /// Default implementation of IParameterCapture.
    /// </summary>
    public sealed class ParameterCapture : IParameterCapture
    {
        private static readonly HashSet<Type> PrimitiveTypes = new HashSet<Type>
        {
            typeof(bool), typeof(byte), typeof(sbyte),
            typeof(char), typeof(decimal), typeof(double), typeof(float),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(short), typeof(ushort), typeof(string),
            typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan),
            typeof(Guid)
        };
        
        private readonly ConcurrentDictionary<string, bool> _sensitivePatternCache = new();
        private readonly List<SensitivePattern> _sensitivePatterns = new();
        
        public ParameterCapture()
        {
            // Register default sensitive patterns
            RegisterDefaultPatterns();
        }
        
        public object? CaptureParameter(
            string parameterName,
            object? value,
            Type parameterType,
            ParameterCaptureOptions options)
        {
            if (string.IsNullOrEmpty(parameterName))
                throw new ArgumentNullException(nameof(parameterName));
            if (parameterType == null)
                throw new ArgumentNullException(nameof(parameterType));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            
            // Check capture level
            if (options.Level == CaptureLevel.None)
                return null;
            
            // Check for sensitive data
            if (options.AutoDetectSensitiveData && IsSensitive(parameterName))
                return RedactValue(value, options.RedactionStrategy);
            
            // Capture based on level
            return CaptureValue(value, parameterType, options, 0);
        }
        
        public IDictionary<string, object?> CaptureParameters(
            ParameterInfo[] parameters,
            object?[] values,
            ParameterCaptureOptions options)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            
            var result = new Dictionary<string, object?>(parameters.Length);
            
            for (int i = 0; i < parameters.Length && i < values.Length; i++)
            {
                var param = parameters[i];
                var value = values[i];
                
                // Check for SensitiveData attribute
                if (param.GetCustomAttribute<SensitiveDataAttribute>() != null)
                {
                    result[param.Name!] = RedactValue(value, options.RedactionStrategy);
                    continue;
                }
                
                var captured = CaptureParameter(param.Name!, value, param.ParameterType, options);
                if (captured != null)
                    result[param.Name!] = captured;
            }
            
            return result;
        }
        
        public void RegisterSensitivePattern(string pattern, RedactionStrategy strategy)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentNullException(nameof(pattern));
            
            _sensitivePatterns.Add(new SensitivePattern(pattern, strategy));
            _sensitivePatternCache.Clear(); // Clear cache when patterns change
        }
        
        public bool IsSensitive(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
                return false;
            
            return _sensitivePatternCache.GetOrAdd(parameterName, name =>
            {
                var lowerName = name.ToLowerInvariant();
                foreach (var pattern in _sensitivePatterns)
                {
                    if (pattern.IsMatch(lowerName))
                        return true;
                }
                return false;
            });
        }
        
        private object? CaptureValue(object? value, Type type, ParameterCaptureOptions options, int depth)
        {
            if (value == null)
                return null;
            
            if (depth >= options.MaxDepth)
                return $"[Max depth {options.MaxDepth} reached]";
            
            // Handle primitive types
            if (IsPrimitiveType(type))
                return CapturePrimitive(value, type, options);
            
            // Minimal level - only primitives
            if (options.Level == CaptureLevel.Minimal)
                return null;
            
            // Handle collections
            if (value is IEnumerable enumerable && type != typeof(string))
                return CaptureCollection(enumerable, options, depth);
            
            // Standard level - primitives and collections only
            if (options.Level == CaptureLevel.Standard)
                return CaptureToString(value, options);
            
            // Verbose level - capture complex objects
            if (options.Level == CaptureLevel.Verbose)
                return CaptureComplexObject(value, type, options, depth);
            
            return null;
        }
        
        private object? CapturePrimitive(object value, Type type, ParameterCaptureOptions options)
        {
            // Handle strings with max length
            if (value is string str)
            {
                if (str.Length > options.MaxStringLength)
                    return str.Substring(0, options.MaxStringLength) + $"... ({str.Length} chars)";
                return str;
            }
            
            // Handle enums
            if (type.IsEnum)
                return value.ToString();
            
            // Handle nullable types
            if (Nullable.GetUnderlyingType(type) != null)
                return value;
            
            // All other primitives
            return value;
        }
        
        private object? CaptureCollection(IEnumerable enumerable, ParameterCaptureOptions options, int depth)
        {
            var items = new List<object?>();
            int count = 0;
            
            foreach (var item in enumerable)
            {
                if (count >= options.MaxCollectionItems)
                {
                    items.Add($"... (total: {GetCollectionCount(enumerable)} items)");
                    break;
                }
                
                var itemType = item?.GetType() ?? typeof(object);
                items.Add(CaptureValue(item, itemType, options, depth + 1));
                count++;
            }
            
            return items;
        }
        
        private object? CaptureComplexObject(object value, Type type, ParameterCaptureOptions options, int depth)
        {
            // Check for custom serializer
            if (options.CustomSerializers?.TryGetValue(type, out var serializer) == true)
            {
                try
                {
                    return serializer(value);
                }
                catch
                {
                    // Fall back to default capture
                }
            }
            
            // Use custom ToString if available and configured
            if (options.UseCustomToString && HasCustomToString(type))
                return CaptureToString(value, options);
            
            // Capture properties
            if (options.CapturePropertyNames)
            {
                var properties = new Dictionary<string, object?>();
                
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Skip indexed properties
                    if (prop.GetIndexParameters().Length > 0)
                        continue;
                    
                    // Check for sensitive attribute
                    if (prop.GetCustomAttribute<SensitiveDataAttribute>() != null)
                    {
                        properties[prop.Name] = "***";
                        continue;
                    }
                    
                    // Check for sensitive name
                    if (IsSensitive(prop.Name))
                    {
                        properties[prop.Name] = RedactValue(null, options.RedactionStrategy);
                        continue;
                    }
                    
                    try
                    {
                        var propValue = prop.GetValue(value);
                        properties[prop.Name] = CaptureValue(propValue, prop.PropertyType, options, depth + 1);
                    }
                    catch
                    {
                        properties[prop.Name] = "[Error reading property]";
                    }
                }
                
                return properties;
            }
            
            return CaptureToString(value, options);
        }
        
        private object? CaptureToString(object value, ParameterCaptureOptions options)
        {
            try
            {
                var str = value.ToString();
                if (str != null && str.Length > options.MaxStringLength)
                    return str.Substring(0, options.MaxStringLength) + "...";
                return str;
            }
            catch
            {
                return value.GetType().Name;
            }
        }
        
        private object? RedactValue(object? value, RedactionStrategy strategy)
        {
            return strategy switch
            {
                RedactionStrategy.Remove => null,
                RedactionStrategy.Mask => "***",
                RedactionStrategy.Hash => HashValue(value),
                RedactionStrategy.Partial => PartialRedact(value),
                RedactionStrategy.TypeName => value?.GetType().Name ?? "null",
                _ => "***"
            };
        }
        
        private string HashValue(object? value)
        {
            if (value == null) return "null";
            
            var str = value.ToString() ?? string.Empty;
            using var sha256 = SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash).Substring(0, 16);
        }
        
        private string PartialRedact(object? value)
        {
            if (value == null) return "null";
            
            var str = value.ToString();
            if (string.IsNullOrEmpty(str) || str.Length <= 4)
                return "***";
            
            return $"{str.Substring(0, 2)}***{str.Substring(str.Length - 2)}";
        }
        
        private int GetCollectionCount(IEnumerable enumerable)
        {
            if (enumerable is ICollection collection)
                return collection.Count;
            
            int count = 0;
            foreach (var _ in enumerable)
                count++;
            
            return count;
        }
        
        private bool IsPrimitiveType(Type type)
        {
            if (type.IsEnum)
                return true;
            
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
                return PrimitiveTypes.Contains(underlyingType);
            
            return PrimitiveTypes.Contains(type);
        }
        
        private bool HasCustomToString(Type type)
        {
            var toStringMethod = type.GetMethod("ToString", Type.EmptyTypes);
            return toStringMethod?.DeclaringType != typeof(object);
        }
        
        private void RegisterDefaultPatterns()
        {
            // Authentication & Authorization
            RegisterSensitivePattern("password", RedactionStrategy.Mask);
            RegisterSensitivePattern("passwd", RedactionStrategy.Mask);
            RegisterSensitivePattern("pwd", RedactionStrategy.Mask);
            RegisterSensitivePattern("secret", RedactionStrategy.Mask);
            RegisterSensitivePattern("token", RedactionStrategy.Mask);
            RegisterSensitivePattern("apikey", RedactionStrategy.Mask);
            RegisterSensitivePattern("api_key", RedactionStrategy.Mask);
            RegisterSensitivePattern("accesskey", RedactionStrategy.Mask);
            RegisterSensitivePattern("privatekey", RedactionStrategy.Mask);
            
            // Financial
            RegisterSensitivePattern("creditcard", RedactionStrategy.Hash);
            RegisterSensitivePattern("cardnumber", RedactionStrategy.Hash);
            RegisterSensitivePattern("cvv", RedactionStrategy.Mask);
            RegisterSensitivePattern("ccv", RedactionStrategy.Mask);
            RegisterSensitivePattern("pin", RedactionStrategy.Mask);
            RegisterSensitivePattern("accountnumber", RedactionStrategy.Hash);
            RegisterSensitivePattern("routingnumber", RedactionStrategy.Hash);
            
            // Personal Identifiable Information
            RegisterSensitivePattern("ssn", RedactionStrategy.Hash);
            RegisterSensitivePattern("socialsecurity", RedactionStrategy.Hash);
            RegisterSensitivePattern("taxid", RedactionStrategy.Hash);
            RegisterSensitivePattern("driverslicense", RedactionStrategy.Hash);
            RegisterSensitivePattern("passport", RedactionStrategy.Hash);
            
            // Contact Information
            RegisterSensitivePattern("email", RedactionStrategy.Partial);
            RegisterSensitivePattern("phone", RedactionStrategy.Partial);
            RegisterSensitivePattern("phonenumber", RedactionStrategy.Partial);
            RegisterSensitivePattern("mobile", RedactionStrategy.Partial);
            
            // Health Information
            RegisterSensitivePattern("diagnosis", RedactionStrategy.Hash);
            RegisterSensitivePattern("medication", RedactionStrategy.Hash);
            RegisterSensitivePattern("medicalrecord", RedactionStrategy.Hash);
        }
        
        private readonly struct SensitivePattern
        {
            private readonly Regex _regex;
            public readonly RedactionStrategy Strategy;
            
            public SensitivePattern(string pattern, RedactionStrategy strategy)
            {
                _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                Strategy = strategy;
            }
            
            public bool IsMatch(string value) => _regex.IsMatch(value);
        }
    }
}
```

### Extension Methods

```csharp
namespace HVO.Enterprise.Telemetry.Capture
{
    /// <summary>
    /// Extension methods for parameter capture.
    /// </summary>
    public static class ParameterCaptureExtensions
    {
        /// <summary>
        /// Captures parameters and adds them as tags to the operation scope.
        /// </summary>
        public static IOperationScope CaptureParameters(
            this IOperationScope scope,
            ParameterInfo[] parameters,
            object?[] values,
            IParameterCapture? parameterCapture = null,
            ParameterCaptureOptions? options = null)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (values == null) throw new ArgumentNullException(nameof(values));
            
            parameterCapture ??= new ParameterCapture();
            options ??= new ParameterCaptureOptions();
            
            var captured = parameterCapture.CaptureParameters(parameters, values, options);
            
            foreach (var kvp in captured)
            {
                scope.WithTag($"param.{kvp.Key}", kvp.Value);
            }
            
            return scope;
        }
        
        /// <summary>
        /// Captures a return value and adds it as a tag to the operation scope.
        /// </summary>
        public static IOperationScope CaptureReturnValue(
            this IOperationScope scope,
            object? returnValue,
            Type returnType,
            IParameterCapture? parameterCapture = null,
            ParameterCaptureOptions? options = null)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (returnType == null) throw new ArgumentNullException(nameof(returnType));
            
            parameterCapture ??= new ParameterCapture();
            options ??= new ParameterCaptureOptions();
            
            var captured = parameterCapture.CaptureParameter("return", returnValue, returnType, options);
            if (captured != null)
            {
                scope.WithTag("result", captured);
            }
            
            return scope;
        }
    }
}
```

### Configuration Integration

```csharp
namespace HVO.Enterprise.Telemetry.Capture
{
    /// <summary>
    /// Configuration for parameter capture.
    /// </summary>
    public sealed class ParameterCaptureConfiguration
    {
        /// <summary>
        /// Loads capture options from configuration.
        /// </summary>
        public static ParameterCaptureOptions LoadFromConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection("Telemetry:ParameterCapture");
            
            var options = new ParameterCaptureOptions
            {
                Level = section.GetValue<CaptureLevel>("Level", CaptureLevel.Standard),
                AutoDetectSensitiveData = section.GetValue("AutoDetectSensitiveData", true),
                RedactionStrategy = section.GetValue<RedactionStrategy>("RedactionStrategy", RedactionStrategy.Mask),
                MaxDepth = section.GetValue("MaxDepth", 2),
                MaxCollectionItems = section.GetValue("MaxCollectionItems", 10),
                MaxStringLength = section.GetValue("MaxStringLength", 1000),
                UseCustomToString = section.GetValue("UseCustomToString", true),
                CapturePropertyNames = section.GetValue("CapturePropertyNames", true)
            };
            
            return options;
        }
    }
}
```

### Usage Examples

```csharp
// Example 1: Basic usage with OperationScope
var parameterCapture = new ParameterCapture();
var options = new ParameterCaptureOptions
{
    Level = CaptureLevel.Standard,
    AutoDetectSensitiveData = true
};

using (var scope = operationFactory.Begin("ProcessOrder"))
{
    // Capture method parameters
    var parameters = method.GetParameters();
    var values = new object[] { orderId, customerName, creditCardNumber };
    
    scope.CaptureParameters(parameters, values, parameterCapture, options);
    
    ProcessOrder(orderId, customerName, creditCardNumber);
    scope.Succeed();
}

// Example 2: Manual capture
var captured = parameterCapture.CaptureParameter(
    "customerId",
    12345,
    typeof(int),
    new ParameterCaptureOptions { Level = CaptureLevel.Minimal });

scope.WithTag("param.customerId", captured);

// Example 3: Complex object capture
var order = new Order
{
    Id = 12345,
    CustomerName = "John Doe",
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = 1, Quantity = 2, Price = 29.99m },
        new OrderItem { ProductId = 2, Quantity = 1, Price = 49.99m }
    }
};

var captured = parameterCapture.CaptureParameter(
    "order",
    order,
    typeof(Order),
    new ParameterCaptureOptions
    {
        Level = CaptureLevel.Verbose,
        MaxDepth = 3,
        MaxCollectionItems = 5
    });

// Result:
// {
//   "Id": 12345,
//   "CustomerName": "John Doe",
//   "Items": [
//     { "ProductId": 1, "Quantity": 2, "Price": 29.99 },
//     { "ProductId": 2, "Quantity": 1, "Price": 49.99 }
//   ]
// }

// Example 4: Sensitive data redaction
var captured = parameterCapture.CaptureParameter(
    "creditCardNumber",
    "4111111111111111",
    typeof(string),
    new ParameterCaptureOptions { AutoDetectSensitiveData = true });

// Result: "***" (automatically detected as sensitive)

// Example 5: Custom serializer
var options = new ParameterCaptureOptions
{
    CustomSerializers = new Dictionary<Type, Func<object, object?>>
    {
        [typeof(Customer)] = obj =>
        {
            var customer = (Customer)obj;
            return new { customer.Id, customer.Name };
        }
    }
};

// Example 6: Configuration from appsettings.json
{
  "Telemetry": {
    "ParameterCapture": {
      "Level": "Standard",
      "AutoDetectSensitiveData": true,
      "RedactionStrategy": "Mask",
      "MaxDepth": 2,
      "MaxCollectionItems": 10,
      "MaxStringLength": 1000
    }
  }
}

var options = ParameterCaptureConfiguration.LoadFromConfiguration(configuration);
var parameterCapture = new ParameterCapture();
```

## Testing Requirements

### Unit Tests

1. **Capture Level Tests**
   ```csharp
   [Fact]
   public void CaptureParameter_NoneLevel_ReturnsNull()
   {
       var capture = new ParameterCapture();
       var options = new ParameterCaptureOptions { Level = CaptureLevel.None };
       
       var result = capture.CaptureParameter("test", 123, typeof(int), options);
       
       Assert.Null(result);
   }
   
   [Fact]
   public void CaptureParameter_MinimalLevel_CapturesPrimitives()
   {
       var capture = new ParameterCapture();
       var options = new ParameterCaptureOptions { Level = CaptureLevel.Minimal };
       
       var result = capture.CaptureParameter("value", 123, typeof(int), options);
       
       Assert.Equal(123, result);
   }
   
   [Fact]
   public void CaptureParameter_StandardLevel_CapturesCollections()
   {
       var capture = new ParameterCapture();
       var options = new ParameterCaptureOptions { Level = CaptureLevel.Standard };
       var list = new List<int> { 1, 2, 3 };
       
       var result = capture.CaptureParameter("values", list, typeof(List<int>), options);
       
       Assert.IsType<List<object?>>(result);
       var captured = (List<object?>)result;
       Assert.Equal(3, captured.Count);
   }
   ```

2. **Sensitive Data Detection Tests**
   ```csharp
   [Theory]
   [InlineData("password", true)]
   [InlineData("creditCard", true)]
   [InlineData("ssn", true)]
   [InlineData("email", true)]
   [InlineData("orderId", false)]
   [InlineData("customerName", false)]
   public void IsSensitive_DetectsCommonPatterns(string name, bool expected)
   {
       var capture = new ParameterCapture();
       
       Assert.Equal(expected, capture.IsSensitive(name));
   }
   
   [Fact]
   public void CaptureParameter_RedactsSensitiveData()
   {
       var capture = new ParameterCapture();
       var options = new ParameterCaptureOptions
       {
           AutoDetectSensitiveData = true,
           RedactionStrategy = RedactionStrategy.Mask
       };
       
       var result = capture.CaptureParameter(
           "password",
           "secret123",
           typeof(string),
           options);
       
       Assert.Equal("***", result);
   }
   ```

3. **Complex Object Tests**
   ```csharp
   [Fact]
   public void CaptureParameter_CapturesComplexObject()
   {
       var capture = new ParameterCapture();
       var options = new ParameterCaptureOptions
       {
           Level = CaptureLevel.Verbose,
           CapturePropertyNames = true
       };
       
       var obj = new { Id = 123, Name = "Test" };
       var result = capture.CaptureParameter("obj", obj, obj.GetType(), options);
       
       Assert.IsType<Dictionary<string, object?>>(result);
       var dict = (Dictionary<string, object?>)result;
       Assert.Equal(123, dict["Id"]);
       Assert.Equal("Test", dict["Name"]);
   }
   ```

4. **Depth Limit Tests**
   ```csharp
   [Fact]
   public void CaptureParameter_RespectsMaxDepth()
   {
       var capture = new ParameterCapture();
       var options = new ParameterCaptureOptions
       {
           Level = CaptureLevel.Verbose,
           MaxDepth = 1
       };
       
       var nested = new { Level1 = new { Level2 = new { Level3 = "deep" } } };
       var result = capture.CaptureParameter("nested", nested, nested.GetType(), options);
       
       // Should stop at level 1
       var dict = result as Dictionary<string, object?>;
       Assert.NotNull(dict);
       Assert.Contains("[Max depth 1 reached]", dict["Level1"]?.ToString());
   }
   ```

### Performance Tests

```csharp
[Benchmark]
public void CaptureParameter_Primitive()
{
    _capture.CaptureParameter("value", 123, typeof(int), _options);
}

[Benchmark]
public void CaptureParameter_String()
{
    _capture.CaptureParameter("value", "test", typeof(string), _options);
}

[Benchmark]
public void CaptureParameter_Collection()
{
    _capture.CaptureParameter("values", _testList, typeof(List<int>), _options);
}

[Benchmark]
public void CaptureParameter_ComplexObject()
{
    _capture.CaptureParameter("obj", _testObject, _testObject.GetType(), _options);
}

[Benchmark]
public void IsSensitive_Check()
{
    _capture.IsSensitive("password");
}
```

## Performance Requirements

- **Primitive capture**: <10ns
- **String capture**: <20ns
- **Collection capture**: <100ns + 10ns per item
- **Complex object capture**: <500ns + 50ns per property
- **Sensitive check (cached)**: <5ns
- **Memory allocation**: Minimal for disabled capture, <1KB for complex objects

## Dependencies

**Blocked By**: 
- US-001 (Core Package Setup)
- US-011 (Context Enrichment - for PII patterns)

**Blocks**: 
- US-014 (DispatchProxy Instrumentation)

## Definition of Done

- [x] `IParameterCapture` interface and implementation complete
- [x] All capture levels implemented
- [x] Sensitive data detection working
- [x] Redaction strategies implemented
- [x] Extension methods for OperationScope
- [x] Configuration integration
- [x] All unit tests passing (>90% coverage)
- [x] Performance benchmarks meet requirements
- [x] XML documentation complete
- [x] Code reviewed and approved
- [x] Zero warnings in build

## Notes

### Design Decisions

1. **Why tiered capture levels?**
   - Performance: Minimal overhead for production
   - Debugging: Verbose capture for troubleshooting
   - Security: Different levels for different environments

2. **Why built-in sensitive patterns?**
   - Developer convenience (secure by default)
   - Common compliance requirements (PCI-DSS, GDPR, HIPAA)
   - Extensible for custom patterns

3. **Why multiple redaction strategies?**
   - Different compliance requirements
   - Balance between debugging and security
   - Hash enables correlation without exposing PII

4. **Why depth and size limits?**
   - Prevent excessive memory usage
   - Avoid performance degradation
   - Protect against circular references

### Implementation Tips

- Use `ConcurrentDictionary` for sensitive pattern cache
- Consider object pooling for dictionaries
- Add circuit breaker for expensive captures
- Test with real-world objects (EF Core entities, DTOs)
- Profile memory allocations

### Common Pitfalls

- Don't capture passwords even if explicitly requested
- Watch for circular references in object graphs
- Be careful with property getters that have side effects
- Test with collections of different types (arrays, lists, sets)
- Handle exceptions when reading properties gracefully

### Security Considerations

1. **Never log sensitive data in plain text**
   - Always enable auto-detection in production
   - Review custom patterns regularly
   - Audit sensitive data access

2. **Compliance Requirements**
   - PCI-DSS: Never log credit card data
   - GDPR: Right to be forgotten
   - HIPAA: PHI must be protected

3. **Risk Mitigation**
   - Default to more restrictive capture levels
   - Require explicit opt-in for verbose capture
   - Log when sensitive data patterns are detected

## Related Documentation

- [Project Plan](../project-plan.md#15-implement-parameter-capture)
- [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html)
- [PCI-DSS Requirements](https://www.pcisecuritystandards.org/)

## Implementation Summary

**Completed**: 2025-07-17
**Implemented by**: GitHub Copilot

### What Was Implemented
- Created standalone `Capture/` namespace with `IParameterCapture` interface and `ParameterCapture` implementation
- Implemented all four tiered capture levels: None, Minimal, Standard, Verbose
- Built-in sensitive data detection with 30+ default patterns across 5 categories (auth, financial, PII, contact, health)
- Five redaction strategies: Remove, Mask, Hash (SHA256), Partial (first/last 2), TypeName
- Configurable depth limits, collection limits, string truncation, custom serializers
- Extension methods for `IOperationScope` integration (`CaptureParameters`, `CaptureReturnValue`)
- Bridge method `InstrumentationOptions.ToParameterCaptureOptions()` for proxy integration
- Refactored `TelemetryDispatchProxy` to delegate all capture logic to `IParameterCapture`
- DI registration of `IParameterCapture` singleton in `AddTelemetryProxyFactory()`
- Added `Partial` and `TypeName` values to existing `RedactionStrategy` enum
- 87 new tests across 5 test files covering all capture levels, sensitivity, redaction, extensions, and options

### Key Files
- `src/HVO.Enterprise.Telemetry/Capture/CaptureLevel.cs` — Tiered capture enum
- `src/HVO.Enterprise.Telemetry/Capture/IParameterCapture.cs` — Core interface
- `src/HVO.Enterprise.Telemetry/Capture/ParameterCapture.cs` — Full implementation
- `src/HVO.Enterprise.Telemetry/Capture/ParameterCaptureOptions.cs` — Configuration class
- `src/HVO.Enterprise.Telemetry/Capture/ParameterCaptureExtensions.cs` — IOperationScope extensions
- `src/HVO.Enterprise.Telemetry/Proxies/TelemetryDispatchProxy.cs` — Refactored to use IParameterCapture
- `src/HVO.Enterprise.Telemetry/Proxies/TelemetryProxyFactory.cs` — Accepts IParameterCapture
- `src/HVO.Enterprise.Telemetry/Proxies/TelemetryInstrumentationExtensions.cs` — DI registration
- `tests/HVO.Enterprise.Telemetry.Tests/Capture/` — 5 test files (87 tests)

### Decisions Made
- Kept `RedactionStrategy` and `SensitiveDataAttribute` in `Proxies/` namespace to avoid breaking changes; `Capture/` references them via using
- Primitives are always captured regardless of depth limit (bug fix: moved primitive check before depth check)
- `ConcurrentDictionary` used for sensitive pattern cache with lock-based registration
- SHA256 hash truncated to first 8 hex characters for compact redacted values
- Two `ParameterCapture` constructors: default (with 30+ patterns) and `registerDefaults: false` for testing
- `GetRedactionStrategy()` added to interface for strategy introspection

### Quality Gates
- ✅ Build: 0 errors, 0 warnings
- ✅ Tests: 542 passed (120 common + 422 telemetry), 0 failed, 1 skipped
- ✅ XML documentation: Complete for all public APIs
- ✅ Proxy integration: Existing tests updated and passing

### Next Steps
This story enables richer parameter telemetry in US-016 (Statistics & Health Checks), US-017 (HTTP Instrumentation), and US-018 (DI/Static Initialization).
