using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Context
{
    /// <summary>
    /// Controls how request, environment, and user context are captured and redacted before being attached to telemetry.
    /// </summary>
    public sealed class EnrichmentOptions
    {
        /// <summary>
        /// Gets or sets the maximum enrichment level to apply (None, Minimal, Standard, or Verbose). Default: <see cref="EnrichmentLevel.Standard"/>.
        /// </summary>
        public EnrichmentLevel MaxLevel { get; set; } = EnrichmentLevel.Standard;

        /// <summary>
        /// Gets or sets whether personally identifiable information should be redacted before being emitted. Default: <see langword="true"/>.
        /// </summary>
        public bool RedactPii { get; set; } = true;

        /// <summary>
        /// Gets or sets the PII redaction strategy (masking vs. hashing). Default: <see cref="PiiRedactionStrategy.Mask"/>.
        /// </summary>
        public PiiRedactionStrategy RedactionStrategy { get; set; } = PiiRedactionStrategy.Mask;

        /// <summary>
        /// Gets or sets HTTP header names that should never be captured, even when enrichment is configured for Verbose mode.
        /// Pre-populated with common sensitive headers.
        /// </summary>
        public HashSet<string> ExcludedHeaders { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Cookie",
            "X-API-Key",
            "X-Auth-Token"
        };

        /// <summary>
        /// Gets or sets property names that should be treated as PII regardless of location (tags, payloads, etc.).
        /// Default list targets common credential and contact fields.
        /// </summary>
        public HashSet<string> PiiProperties { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "email",
            "ssn",
            "creditcard",
            "password",
            "phone",
            "token",
            "apikey"
        };

        /// <summary>
        /// Gets or sets custom environment tags (for example, cloud provider metadata) to include with every scope.
        /// </summary>
        public Dictionary<string, string> CustomEnvironmentTags { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Ensures collection defaults are initialized.
        /// </summary>
        internal void EnsureDefaults()
        {
            ExcludedHeaders ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            PiiProperties ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CustomEnvironmentTags ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
