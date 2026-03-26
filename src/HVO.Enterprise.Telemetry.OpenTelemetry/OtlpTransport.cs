namespace HVO.Enterprise.Telemetry.OpenTelemetry
{
    /// <summary>
    /// OTLP transport protocol.
    /// </summary>
    public enum OtlpTransport
    {
        /// <summary>gRPC transport (port 4317).</summary>
        Grpc = 0,

        /// <summary>HTTP/protobuf transport (default, port 4318).</summary>
        HttpProtobuf = 1
    }
}
