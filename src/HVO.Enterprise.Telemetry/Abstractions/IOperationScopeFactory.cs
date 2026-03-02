using System;

namespace HVO.Enterprise.Telemetry.Abstractions
{
    /// <summary>
    /// Creates instrumented <see cref="IOperationScope"/> instances that coordinate
    /// <see cref="System.Diagnostics.Activity"/> lifecycles, correlation IDs, logging, and metrics.
    /// Implementations must be thread-safe because factories are registered as singletons in DI containers.
    /// </summary>
    public interface IOperationScopeFactory
    {
        /// <summary>
        /// Creates a new operation scope.
        /// </summary>
        /// <param name="name">Operation name used for Activity display names, metrics dimensions, and log output.</param>
        /// <param name="options">Optional per-call overrides. When <see langword="null"/>, the factory default is used.</param>
        /// <returns>A started operation scope that must be disposed to flush telemetry.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
        /// <remarks>
        /// <para>
        /// Consumers should prefer the overloads exposed on <see cref="ITelemetryService"/> when they do not
        /// need to supply custom <see cref="OperationScopeOptions"/>. Use this factory directly when constructing
        /// scopes outside the telemetry subsystem (for example, in background services).
        /// </para>
        /// <para>
        /// Passing an <see cref="OperationScopeOptions"/> value lets callers override log verbosity, metrics recording,
        /// PII redaction, or initial tags on a per-operation basis without mutating global configuration.
        /// </para>
        /// </remarks>
        IOperationScope Begin(string name, OperationScopeOptions? options = null);
    }
}
