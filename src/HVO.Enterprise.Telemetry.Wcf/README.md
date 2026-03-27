# HVO.Enterprise.Telemetry.Wcf

WCF extension for HVO.Enterprise.Telemetry.

## Features

- **Distributed Tracing** — Automatic W3C TraceContext propagation in SOAP headers
- **Message Inspectors** — Client and server-side telemetry capture
- **Fault Tracking** — Automatic WCF fault exception correlation

## Installation

```
dotnet add package HVO.Enterprise.Telemetry.Wcf
```

## Quick Start

```csharp
services.AddTelemetry(builder =>
    builder.WithWcfInstrumentation(options =>
    {
        options.PropagateTraceContextInReply = true;
    }));
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## Documentation

See the [HVO.Enterprise documentation](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry) for full usage guides.

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/LICENSE) for details.
