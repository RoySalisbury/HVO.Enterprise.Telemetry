using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Configures how HVO telemetry enriches and filters application logs.
    /// </summary>
    public sealed class LoggingOptions
    {
        /// <summary>
        /// Gets or sets whether correlation IDs and Activity metadata are injected into <see cref="Microsoft.Extensions.Logging.ILogger"/> scopes.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableCorrelationEnrichment { get; set; } = true;

        /// <summary>
        /// Gets or sets per-category minimum log level overrides. Keys are logger category names and values are
        /// <see cref="Microsoft.Extensions.Logging.LogLevel"/> strings (case-insensitive). Empty by default.
        /// </summary>
        public Dictionary<string, string> MinimumLevel { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
