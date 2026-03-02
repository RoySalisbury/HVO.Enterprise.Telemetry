using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Abstractions
{
    /// <summary>
    /// Represents an operation scope with automatic timing and telemetry capture.
    /// </summary>
    public interface IOperationScope : IDisposable
    {
        /// <summary>
        /// Gets the operation name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the correlation ID for this operation.
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Gets the Activity associated with this scope (if any).
        /// </summary>
        Activity? Activity { get; }

        /// <summary>
        /// Gets the elapsed time since the operation started.
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Adds a tag to the operation.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <param name="value">Tag value. Passing <see langword="null"/> removes the tag if it was previously set.</param>
        /// <returns>The current scope.</returns>
        /// <remarks>
        /// Supplying <see langword="null"/> is treated as a directive to remove the tag rather than storing a null value.
        /// This enables fluent code to clear sensitive values without throwing or mutating the tag key casing.
        /// </remarks>
        IOperationScope WithTag(string key, object? value);

        /// <summary>
        /// Adds multiple tags to the operation.
        /// </summary>
        /// <param name="tags">Tags to add.</param>
        /// <returns>The current scope.</returns>
        IOperationScope WithTags(IEnumerable<KeyValuePair<string, object?>> tags);

        /// <summary>
        /// Adds a property that will be evaluated on disposal.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="valueFactory">Factory for property value.</param>
        /// <returns>The current scope.</returns>
        IOperationScope WithProperty(string key, Func<object?> valueFactory);

        /// <summary>
        /// Marks the operation as failed with an exception.
        /// </summary>
        /// <param name="exception">Exception that caused failure.</param>
        /// <returns>The current scope.</returns>
        IOperationScope Fail(Exception exception);

        /// <summary>
        /// Marks the operation as succeeded.
        /// </summary>
        /// <returns>The current scope.</returns>
        IOperationScope Succeed();

        /// <summary>
        /// Sets the result of the operation.
        /// </summary>
        /// <param name="result">Result object.</param>
        /// <returns>The current scope.</returns>
        IOperationScope WithResult(object? result);

        /// <summary>
        /// Creates a child operation scope.
        /// </summary>
        /// <param name="name">Child operation name.</param>
        /// <returns>Child scope.</returns>
        IOperationScope CreateChild(string name);

        /// <summary>
        /// Records an exception that occurred during this operation.
        /// </summary>
        /// <param name="exception">Exception to record.</param>
        void RecordException(Exception exception);
    }
}
