namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Feature flags that toggle optional instrumentation components without recompiling the application.
    /// </summary>
    public sealed class FeatureFlags
    {
        /// <summary>
        /// Gets or sets whether automatic HTTP instrumentation is enabled for <see cref="System.Net.Http.HttpClient"/>
        /// and ASP.NET Core handlers. Default: <see langword="true"/>.
        /// </summary>
        public bool EnableHttpInstrumentation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether DispatchProxy-based instrumentation is enabled for interface proxies. Default: <see langword="true"/>.
        /// </summary>
        public bool EnableProxyInstrumentation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether exception tracking (including first-chance subscriptions when configured)
        /// is enabled. Default: <see langword="true"/>.
        /// </summary>
        public bool EnableExceptionTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether method parameter capture is enabled for decorated operations. Default: <see langword="false"/>.
        /// This feature is opt-in because it can record sensitive data.
        /// </summary>
        public bool EnableParameterCapture { get; set; } = false;
    }
}
