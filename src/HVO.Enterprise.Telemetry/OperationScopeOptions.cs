using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using HVO.Enterprise.Telemetry.Context;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Controls how an <see cref="HVO.Enterprise.Telemetry.Abstractions.IOperationScope"/> behaves while timing an operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options can be configured globally (via dependency injection) or per scope invocation to override
    /// logging, metrics, or serialization behavior without mutating global defaults. Unless stated otherwise, all
    /// properties apply to both parent and child scopes that are created from the same instance.
    /// </para>
    /// </remarks>
    public sealed class OperationScopeOptions
    {
        /// <summary>
        /// Gets or sets whether an underlying <see cref="Activity"/> is created for each scope. Default: <see langword="true"/>.
        /// </summary>
        public bool CreateActivity { get; set; } = true;

        /// <summary>
        /// Gets or sets the <see cref="ActivityKind"/> used when <see cref="CreateActivity"/> is enabled. Default: <see cref="ActivityKind.Internal"/>.
        /// </summary>
        public ActivityKind ActivityKind { get; set; } = ActivityKind.Internal;

        /// <summary>
        /// Gets or sets whether the scope emits start/stop log entries via the injected <see cref="ILogger"/>. Default: <see langword="true"/>.
        /// </summary>
        public bool LogEvents { get; set; } = true;

        /// <summary>
        /// Gets or sets the log level used when <see cref="LogEvents"/> is enabled. Default: <see cref="LogLevel.Information"/>.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets whether duration and error metrics are recorded. Default: <see langword="true"/>.
        /// </summary>
        public bool RecordMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether contextual enrichers (user, request, environment tags) run for this scope. Default: <see langword="true"/>.
        /// </summary>
        public bool EnrichContext { get; set; } = true;

        /// <summary>
        /// Gets or sets whether captured exceptions are recorded on the Activity and metrics pipeline. Default: <see langword="true"/>.
        /// </summary>
        public bool CaptureExceptions { get; set; } = true;

        /// <summary>
        /// Gets or sets tags that are applied as soon as the scope is created. Values participate in PII redaction rules.
        /// </summary>
        public Dictionary<string, object?>? InitialTags { get; set; }

        /// <summary>
        /// Gets or sets PII redaction options for tags and properties. When <see langword="null"/>, defaults are applied lazily.
        /// </summary>
        public EnrichmentOptions? PiiOptions { get; set; } = new EnrichmentOptions();

        /// <summary>
        /// Gets or sets whether objects that are not primitive/simple are serialized to JSON. Default: <see langword="true"/>.
        /// </summary>
        public bool SerializeComplexTypes { get; set; } = true;

        /// <summary>
        /// Gets or sets a custom serializer for complex tag values. When specified, <see cref="SerializeComplexTypes"/> must remain enabled.
        /// </summary>
        public Func<object, string>? ComplexTypeSerializer { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="JsonSerializerOptions"/> used when serializing complex values via <see cref="System.Text.Json"/>.
        /// </summary>
        public JsonSerializerOptions? JsonSerializerOptions { get; set; }

        internal OperationScopeOptions CreateChildOptions()
        {
            return new OperationScopeOptions
            {
                CreateActivity = CreateActivity,
                ActivityKind = ActivityKind,
                LogEvents = LogEvents,
                LogLevel = LogLevel,
                RecordMetrics = RecordMetrics,
                EnrichContext = false,
                CaptureExceptions = CaptureExceptions,
                PiiOptions = PiiOptions,
                SerializeComplexTypes = SerializeComplexTypes,
                ComplexTypeSerializer = ComplexTypeSerializer,
                JsonSerializerOptions = JsonSerializerOptions
            };
        }
    }
}
