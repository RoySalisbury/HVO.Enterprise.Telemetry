# Testing

The HVO.Enterprise telemetry solution uses MSTest 3.7 with method-level parallelization.  All projects build with warnings treated as errors, so the test suite must remain clean on every run.

## Test Layers

| Layer | Location | Description |
|-------|----------|-------------|
| Unit | `tests/HVO.Enterprise.Telemetry.Tests` and extension-specific test projects | Fast tests that mock external dependencies.  These are the default tests executed on every `dotnet test`. |
| Integration | `tests/HVO.Enterprise.Telemetry.Data.*.Tests`, `tests/HVO.Enterprise.Telemetry.OpenTelemetry.Tests` | Use real infrastructure (PostgreSQL, Redis, RabbitMQ, OTLP collector).  Tagged with `[TestCategory("Integration")]`. |
| Compatibility | `tests/HVO.Common.Tests` | Confirms the `HVO.Core` NuGet dependency surface that telemetry consumes. |

## Running Tests Locally

```bash
# From the repo root
cd /workspaces/HVO.Workspace/repos/HVO.Enterprise.Telemetry

# All unit tests
 dotnet test --filter "TestCategory!=Integration"
```

Integration tests require live services.  Start the docker-compose harness, run the targeted tests, and tear the stack down when finished:

```bash
docker compose -f docker-compose.test.yml up -d
 dotnet test --filter "TestCategory=Integration"
docker compose -f docker-compose.test.yml down
```

### docker-compose services

| Service | Image | Purpose |
|---------|-------|---------|
| `postgres` | `postgres:16-alpine` | Backing store for `HVO.Enterprise.Telemetry.Data.AdoNet/EfCore` connectivity tests. |
| `redis` | `redis:7-alpine` | Validates the Redis instrumentation. |
| `rabbitmq` | `rabbitmq:3.13-alpine` | Exercises RabbitMQ instrumentation. |
| `otel-collector` | `otel/opentelemetry-collector-contrib:0.120.0` | Captures OTLP traffic for exporter tests. |

All ports are bound to `127.0.0.1` so the harness does not conflict with other local containers.  Health checks are built into the compose file, so `docker compose up` blocks until each service is ready.

### Tips

- Use `--logger "console;verbosity=detailed"` when triaging flaky integration runs.
- Keep compose services running while iterating to avoid container churn; just rerun `dotnet test` with the appropriate filter.
- Integration tests are also executed in CI (see [Pipeline Integration](pipeline-integration.md)), so avoid adding environment-specific assumptions.
