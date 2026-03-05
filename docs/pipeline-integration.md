# Pipeline Integration

## GitHub Actions CI

` .github/workflows/ci.yml` runs on every push and pull request.  The workflow has two jobs:

1. **build-and-unit-tests** – restores, builds, and executes the default MSTest suites.
2. **integration-tests** – launches [`docker-compose.test.yml`](../docker-compose.test.yml) and runs only the tests tagged `Integration`.

Both jobs upload test and coverage artifacts that feed the README badges.  Keep the workflow in sync with any new test projects so we maintain parity between local and CI executions.

## Publishing Flow

Telemetry packages are published per-package using the tag convention `<PackageId>/v<SemVer>` (e.g., `HVO.Enterprise.Telemetry/v1.0.0`).  GiHub Actions listens for those tags and pushes the matching `.nupkg/.snupkg` pair to NuGet (and GitHub Packages when credentials are available).

Recommended steps for each release:

1. **Validate dependencies** – Telemetry consumes `HVO.Core` from the `HVO.SDK` repo.  Ensure the desired version is already published on NuGet (`https://api.nuget.org/v3-flatcontainer/hvo.core/index.json`).
2. **Bump package versions** – update `<Version>` in each affected `.csproj`.  Keep dependency projects on the same version to avoid mismatched ranges.
3. **Tag and push** – create annotated tags for every package you plan to publish:
   ```bash
   git tag -a "HVO.Enterprise.Telemetry/v1.0.1" -m "Telemetry 1.0.1"
   git push origin "HVO.Enterprise.Telemetry/v1.0.1"
   ```
4. **Verify NuGet** – confirm the version appears on NuGet via the flat-container endpoint or the UI.  Repeat for GitHub Packages when PAT access is available.

> **Note:** `HVO.Enterprise.Telemetry.Grpc` is still pending release while the final API surface is validated.  Leave the README entry marked as "Coming soon" until we cut the first preview tag.

## Ordering

Because consumers (e.g., HVO.SkyMonitor) reference `HVO.Enterprise.Telemetry` and that package references `HVO.Core`, publish packages in the following order when coordinating multi-repo releases:

1. `HVO.Core` / `HVO.Core.SourceGenerators` (HVO.SDK repo)
2. Core telemetry library (`HVO.Enterprise.Telemetry`)
3. Extension packages (IIS, WCF, Serilog, AppInsights, Datadog, Data.*, OpenTelemetry, etc.)
4. Sample or tooling packages

Tagging in this order ensures NuGet dependency resolution succeeds immediately after the pipeline completes.
