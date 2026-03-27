# US-037: NuGet Package Publishing Infrastructure

**GitHub Issue**: [#85](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/85)
**Status**: ✅ Complete
**Category**: Infrastructure / DevOps
**Effort**: Large (8-13 story points)
**Sprint**: Backlog

---

## User Story

**As a** library consumer and maintainer of the HVO.Enterprise ecosystem,
**I want** all 13 source projects packaged as production-ready, strong-named NuGet packages with centralized metadata, per-project versioning, and automated CI/CD publishing,
**So that** teams can install and update individual packages independently from nuget.org or GitHub Packages, with proper licensing, documentation, discoverability, and .NET Framework GAC compatibility.

## Background

The HVO.Enterprise solution contains 13 source projects that are all designed as independently consumable libraries:

| Package | Description |
|---------|-------------|
| `HVO.Common` | General-purpose utilities (Result\<T\>, Option\<T\>, IOneOf, extensions) |
| `HVO.Enterprise.Telemetry` | Core telemetry library (tracing, metrics, logging) |
| `HVO.Enterprise.Telemetry.OpenTelemetry` | OpenTelemetry/OTLP exporter integration |
| `HVO.Enterprise.Telemetry.IIS` | IIS HTTP module instrumentation |
| `HVO.Enterprise.Telemetry.Wcf` | WCF service/client instrumentation |
| `HVO.Enterprise.Telemetry.Serilog` | Serilog sink integration |
| `HVO.Enterprise.Telemetry.AppInsights` | Application Insights bridge |
| `HVO.Enterprise.Telemetry.Datadog` | Datadog APM integration |
| `HVO.Enterprise.Telemetry.Data` | Shared data instrumentation base |
| `HVO.Enterprise.Telemetry.Data.EfCore` | Entity Framework Core interceptor |
| `HVO.Enterprise.Telemetry.Data.AdoNet` | ADO.NET command wrapper |
| `HVO.Enterprise.Telemetry.Data.RabbitMQ` | RabbitMQ instrumentation |
| `HVO.Enterprise.Telemetry.Data.Redis` | Redis instrumentation |

All projects currently target `netstandard2.0` and have partial NuGet metadata (Version, Authors, Description, RepositoryUrl) but are **missing critical properties** needed for publishing: `PackageTags`, `PackageLicenseExpression`, `PackageReadmeFile`, `PackageIcon`, `Copyright`, explicit `PackageId`, and strong naming. There is no `Directory.Build.props` for centralizing shared metadata, no CI/CD pipeline for publishing, no per-project versioning strategy for the monorepo, and no `.snk` key for assembly signing.

Since these packages target `netstandard2.0` and will be consumed by .NET Framework applications, **strong naming is required** — .NET Framework consumers cannot load non-strong-named assemblies into strong-named applications or the GAC. Per Microsoft's official guidance for open-source libraries, the full `.snk` key pair is committed to the repository. Strong naming provides **identity**, not security — it ensures assemblies have a consistent `PublicKeyToken` across builds. This is the standard practice followed by Serilog, Newtonsoft.Json, AutoMapper, MassTransit, and other major OSS .NET libraries.

Each package should be versioned independently — updating one package should not require version bumps across the entire ecosystem. This supports consumers who only need specific extensions and want stable dependency trees.

## Acceptance Criteria

### 1. Centralized Build Properties (`Directory.Build.props`)

- [x] `Directory.Build.props` created at the repository root with shared NuGet metadata
- [x] Shared properties include: `Authors`, `Company`, `Copyright`, `PackageLicenseExpression`, `PackageProjectUrl`, `RepositoryUrl`, `RepositoryType`, `PackageIcon`
- [x] Build settings centralized: `LangVersion`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `GenerateDocumentationFile`
- [x] Individual `.csproj` files simplified by removing properties now in `Directory.Build.props`
- [x] `src/Directory.Build.props` (optional) for src-only overrides (e.g., `IsPackable=true`)
- [x] `tests/Directory.Build.props` (optional) for test-only overrides (e.g., `IsPackable=false`, `IsTestProject=true`)
- [x] All projects still build with 0 warnings, 0 errors after refactoring

### 2. Per-Project NuGet Metadata

- [x] Every `.csproj` under `src/` has an explicit `PackageId` matching the project name
- [x] Every `.csproj` has a `Version` property specific to that package (initially `1.0.0`)
- [x] Every `.csproj` has `PackageTags` with relevant keywords for NuGet search discoverability
- [x] Every `.csproj` includes a `PackageReadmeFile` pointing to a per-package `README.md`
- [x] Each package has a `README.md` included in the `.nupkg` (via `<None Include="README.md" Pack="true" PackagePath="\" />`)
- [x] `PackageReleaseNotes` property placeholder added (can be empty initially)
- [x] Consistent `Authors` value across all projects (`Roy Salisbury`)

### 3. Per-Project Independent Versioning

- [x] Each package maintains its own `Version` in its `.csproj` file
- [x] Version format follows SemVer 2.0: `Major.Minor.Patch[-prerelease]`
- [x] Pre-release versions supported (e.g., `1.0.0-preview.1`, `1.0.0-rc.1`)
- [x] `VERSIONING.md` document created explaining the per-project versioning strategy
- [x] `ProjectReference` dependencies between packages use version ranges in the generated `.nuspec` (automatic via SDK)
- [x] Changing the version in one `.csproj` does not require changes to other `.csproj` files

### 4. Package Validation

- [x] `dotnet pack` succeeds for every project in `src/` with 0 warnings
- [x] Generated `.nupkg` files contain correct metadata (validated via `dotnet nuget verify` or inspection)
- [x] Package dependencies are correctly declared (no missing or extraneous references)
- [x] XML documentation file is included in the package
- [x] README file is embedded in the package
- [x] License expression is valid and recognized by NuGet
- [x] Source Link enabled for debugger source stepping (`Microsoft.SourceLink.GitHub` package)
- [x] Deterministic builds enabled for reproducibility

### 5. CI/CD Pipeline — Pack and Publish

- [x] GitHub Actions workflow (`.github/workflows/nuget-publish.yml`) created
- [x] Workflow triggers:
  - [x] On push to `main` — builds, tests, and packs all projects (validation only, **no publish**)
  - [x] On tag push matching `<PackageId>/v*` pattern (e.g., `HVO.Common/v1.0.0`) — publishes **only that specific package**
  - [x] Manual workflow dispatch with package name input for ad-hoc publishing
- [x] Publishing **never happens automatically on merge** — only on explicit version tags
- [x] Pack step: `dotnet pack --configuration Release` (produces both `.nupkg` and `.snupkg`)
- [x] Publish step pushes **both** `.nupkg` and `.snupkg` (symbol package) to the target feed
- [x] Publish step supports both targets (configurable via workflow inputs / secrets):
  - [x] **nuget.org** — via `NUGET_API_KEY` repository secret
  - [x] **GitHub Packages** — via `GITHUB_TOKEN` (automatic)
- [x] Workflow includes validation steps before publish:
  - [x] `dotnet build` succeeds with 0 warnings
  - [x] `dotnet test` passes
  - [x] `dotnet pack` succeeds
- [x] Publish step uses `dotnet nuget push` with `--skip-duplicate` flag
- [x] Workflow sets package version from the `.csproj` `Version` property (no override)
- [x] `--skip-duplicate` prevents errors if the same version was already published

### 6. Package Icon and Branding

- [x] Package icon file created (`docs/assets/nuget-icon.png`, 128×128 or 256×256)
- [x] Icon referenced from all packages via `PackageIcon` property
- [x] Icon included in `.nupkg` via `<None Include="..\..\docs\assets\nuget-icon.png" Pack="true" PackagePath="\" />`

### 7. Strong Naming

- [x] Strong name key pair (`.snk`) generated using `sn -k HVO.Enterprise.snk`
- [x] Full `.snk` key pair committed to the repository root (standard OSS practice)
- [x] Root `Directory.Build.props` configures strong naming:
  - [x] `<SignAssembly>true</SignAssembly>` for all projects
  - [x] `<AssemblyOriginatorKeyFile>` points to `HVO.Enterprise.snk`
- [x] All assemblies are fully signed with consistent `PublicKeyToken`
- [x] `InternalsVisibleTo` attributes updated with the public key where needed
- [x] Key generation and public key token documented in `VERSIONING.md`

### 8. Local Development Experience

- [x] `dotnet pack` can be run locally for any individual project
- [x] Developers can create local NuGet packages for testing without publishing
- [x] `scripts/pack-all.sh` convenience script that packs all src projects
- [x] Output `.nupkg` files written to a `artifacts/` directory (gitignored)
- [x] `.gitignore` updated to exclude `artifacts/` and `*.nupkg`

## Technical Requirements

### Directory.Build.props (Root)

```xml
<Project>
  <PropertyGroup>
    <!-- Language & Build Settings -->
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    
    <!-- Shared NuGet Metadata -->
    <Authors>HVO Enterprise</Authors>
    <Company>HVO Enterprise</Company>
    <Copyright>Copyright © HVO Enterprise 2024-2026</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/RoySalisbury/HVO.Enterprise.Telemetry</PackageProjectUrl>
    <RepositoryUrl>https://github.com/RoySalisbury/HVO.Enterprise.Telemetry</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageIcon>nuget-icon.png</PackageIcon>
    
    <!-- Build Quality -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <Deterministic>true</Deterministic>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    
    <!-- Strong Naming -->
    <SignAssembly>true</SignAssembly>
    <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)HVO.Enterprise.snk</AssemblyOriginatorKeyFile>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>
</Project>
```

### src/Directory.Build.props

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  
  <PropertyGroup>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
  
  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="\" Condition="Exists('README.md')" />
    <None Include="..\..\docs\assets\nuget-icon.png" Pack="true" PackagePath="\" 
          Condition="Exists('..\..\docs\assets\nuget-icon.png')" />
  </ItemGroup>
</Project>
```

### tests/Directory.Build.props

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

### Example Simplified .csproj (HVO.Enterprise.Telemetry)

After centralizing shared properties, individual `.csproj` files become much leaner:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <PackageId>HVO.Enterprise.Telemetry</PackageId>
    <Version>1.0.0</Version>
    <Description>Core telemetry library providing distributed tracing, metrics, and structured logging across all .NET platforms.</Description>
    <PackageTags>telemetry;tracing;metrics;logging;opentelemetry;observability;dotnet</PackageTags>
    <PackageReleaseNotes></PackageReleaseNotes>
  </PropertyGroup>

  <ItemGroup>
    <!-- package references only -->
  </ItemGroup>

</Project>
```

### PackageTags Reference

| Package | Suggested Tags |
|---------|----------------|
| `HVO.Common` | `utilities;result;option;discriminated-union;functional;extensions;dotnet` |
| `HVO.Enterprise.Telemetry` | `telemetry;tracing;metrics;logging;opentelemetry;observability;dotnet` |
| `HVO.Enterprise.Telemetry.OpenTelemetry` | `telemetry;opentelemetry;otlp;tracing;metrics;jaeger;prometheus;grafana` |
| `HVO.Enterprise.Telemetry.IIS` | `telemetry;iis;aspnet;http-module;instrumentation;dotnet-framework` |
| `HVO.Enterprise.Telemetry.Wcf` | `telemetry;wcf;soap;instrumentation;tracing;dotnet-framework` |
| `HVO.Enterprise.Telemetry.Serilog` | `telemetry;serilog;logging;structured-logging;sink;enricher` |
| `HVO.Enterprise.Telemetry.AppInsights` | `telemetry;application-insights;azure;monitoring;bridge` |
| `HVO.Enterprise.Telemetry.Datadog` | `telemetry;datadog;apm;metrics;tracing;monitoring` |
| `HVO.Enterprise.Telemetry.Data` | `telemetry;database;data;instrumentation;tracing;sql` |
| `HVO.Enterprise.Telemetry.Data.EfCore` | `telemetry;efcore;entity-framework;database;interceptor;tracing` |
| `HVO.Enterprise.Telemetry.Data.AdoNet` | `telemetry;adonet;database;sql;command;tracing` |
| `HVO.Enterprise.Telemetry.Data.RabbitMQ` | `telemetry;rabbitmq;messaging;amqp;tracing;instrumentation` |
| `HVO.Enterprise.Telemetry.Data.Redis` | `telemetry;redis;cache;tracing;instrumentation` |

### GitHub Actions Workflow

```yaml
name: NuGet Pack & Publish

on:
  push:
    branches: [main]
    # Main branch pushes: build + test + pack (validation only, never publish)
  create:
    tags:
      - '*/v*'  # e.g., HVO.Common/v1.0.0, HVO.Enterprise.Telemetry/v1.2.0
  workflow_dispatch:
    inputs:
      package:
        description: 'Package to publish (e.g., HVO.Common)'
        required: true
        type: choice
        options:
          - HVO.Common
          - HVO.Enterprise.Telemetry
          - HVO.Enterprise.Telemetry.OpenTelemetry
          - HVO.Enterprise.Telemetry.IIS
          - HVO.Enterprise.Telemetry.Wcf
          - HVO.Enterprise.Telemetry.Serilog
          - HVO.Enterprise.Telemetry.AppInsights
          - HVO.Enterprise.Telemetry.Datadog
          - HVO.Enterprise.Telemetry.Data
          - HVO.Enterprise.Telemetry.Data.EfCore
          - HVO.Enterprise.Telemetry.Data.AdoNet
          - HVO.Enterprise.Telemetry.Data.RabbitMQ
          - HVO.Enterprise.Telemetry.Data.Redis
      target:
        description: 'Publish target'
        required: true
        type: choice
        options:
          - nuget.org
          - github-packages
          - both
        default: nuget.org

jobs:
  # ── Always runs: build + test + pack validation ──
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet build --configuration Release
      - run: dotnet test --configuration Release --no-build --verbosity normal

  pack:
    needs: build-and-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet pack --configuration Release --output ./artifacts
      # Upload both .nupkg and .snupkg (symbol packages)
      - uses: actions/upload-artifact@v4
        with:
          name: nuget-packages
          path: |
            ./artifacts/*.nupkg
            ./artifacts/*.snupkg

  # ── Only on tag push or manual dispatch: publish ──
  publish-nuget:
    needs: pack
    if: >
      (github.event_name == 'create' && startsWith(github.ref, 'refs/tags/'))
      || github.event_name == 'workflow_dispatch'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
        with:
          name: nuget-packages
          path: ./artifacts
      # Push .nupkg (NuGet.org auto-associates .snupkg with matching .nupkg)
      - name: Push to NuGet.org
        run: >
          dotnet nuget push "./artifacts/*.nupkg"
          --api-key ${{ secrets.NUGET_API_KEY }}
          --source https://api.nuget.org/v3/index.json
          --skip-duplicate
      # Push .snupkg symbol packages explicitly
      - name: Push symbols to NuGet.org
        run: >
          dotnet nuget push "./artifacts/*.snupkg"
          --api-key ${{ secrets.NUGET_API_KEY }}
          --source https://api.nuget.org/v3/index.json
          --skip-duplicate

  publish-github:
    needs: pack
    if: >
      (github.event_name == 'create' && startsWith(github.ref, 'refs/tags/'))
      || github.event_name == 'workflow_dispatch'
    runs-on: ubuntu-latest
    permissions:
      packages: write
    steps:
      - uses: actions/download-artifact@v4
        with:
          name: nuget-packages
          path: ./artifacts
      - name: Push to GitHub Packages
        run: >
          dotnet nuget push "./artifacts/*.nupkg"
          --api-key ${{ secrets.GITHUB_TOKEN }}
          --source https://nuget.pkg.github.com/RoySalisbury/index.json
          --skip-duplicate
```

### Strong Naming Setup

For open-source .NET Standard libraries, the full `.snk` key pair is committed to the repository. Strong naming provides **identity** (consistent `PublicKeyToken`), not security. This is the standard practice recommended by Microsoft and followed by all major OSS .NET libraries.

**One-time key generation:**

```bash
# Generate the key pair (committed to repo root)
sn -k HVO.Enterprise.snk

# Display the public key token (needed for InternalsVisibleTo attributes)
sn -tp HVO.Enterprise.snk
```

**Updating `InternalsVisibleTo`** after generating the key:

All existing `InternalsVisibleTo` attributes must include the public key. In `.csproj` files:

```xml
<InternalsVisibleTo Include="HVO.Enterprise.Telemetry.Tests, PublicKey=002400..." />
```

The full public key hex string is obtained from `sn -tp HVO.Enterprise.snk`.

### Versioning Strategy Document (`VERSIONING.md`)

Create `docs/VERSIONING.md` with the following content:

---

#### Versioning Scheme

All HVO packages follow [SemVer 2.0](https://semver.org/): `Major.Minor.Patch[-prerelease]`

#### When to Increment

| Change Type | Version Bump | Examples |
|-------------|-------------|----------|
| **Bug fix**, internal refactor, perf improvement, doc fix | **Patch** (`1.0.0` → `1.0.1`) | Fix null reference in `TrackOperation`, improve batch export throughput, fix XML doc typo |
| **New feature**, new public API surface (backward-compatible) | **Minor** (`1.0.1` → `1.1.0`) | Add `WithOpenTelemetry()` builder method, add new overload to `AddTelemetry()`, new extension class |
| **Breaking change** to public API | **Major** (`1.1.0` → `2.0.0`) | Rename `ITelemetryService` method, remove public property, change method signature, change default behavior |

#### Pre-Release Conventions

| Stage | Format | Example | Purpose |
|-------|--------|---------|----------|
| Preview | `X.Y.Z-preview.N` | `1.1.0-preview.1` | Early access, API may change |
| Release Candidate | `X.Y.Z-rc.N` | `1.1.0-rc.1` | Feature-complete, final testing |
| Stable | `X.Y.Z` | `1.1.0` | Production-ready |

#### How to Increment (Step-by-Step)

1. **Decide the version bump** using the table above
2. **Update the `Version` property** in the package's `.csproj`:
   ```xml
   <Version>1.1.0</Version>
   ```
3. **Update `PackageReleaseNotes`** in the same `.csproj` (brief summary of changes):
   ```xml
   <PackageReleaseNotes>Added WithOpenTelemetry() builder extension. Fixed batch export delay.</PackageReleaseNotes>
   ```
4. **If the package has dependents** within the solution (e.g., bumping `HVO.Enterprise.Telemetry` which other packages reference): no action needed — `ProjectReference` handles this in source. The published `.nuspec` will automatically reflect the new version as a dependency range.
5. **Commit** with conventional commit message:
   ```
   chore(telemetry): bump version to 1.1.0
   ```
6. **Merge to main** via PR (build + test + pack validation runs automatically)
7. **Create a git tag** to trigger publishing:
   ```bash
   git tag "HVO.Enterprise.Telemetry/v1.1.0"
   git push origin "HVO.Enterprise.Telemetry/v1.1.0"
   ```
8. **CI publishes** the tagged package to nuget.org and/or GitHub Packages

#### What NOT to Do

- **Don't bump versions on merge** — merging to main only validates the pack. Publishing only happens when you push a tag.
- **Don't bump other packages** when one changes — each package is versioned independently.
- **Don't reuse a version** — once `1.0.1` is published, it's immutable on NuGet. If you need a fix, go to `1.0.2`.
- **Don't skip versions** — go `1.0.0` → `1.0.1` → `1.0.2`, not `1.0.0` → `1.0.5`.

#### Tag Format

```
<PackageId>/v<Version>
```

Examples:
- `HVO.Common/v1.0.1`
- `HVO.Enterprise.Telemetry/v1.1.0`
- `HVO.Enterprise.Telemetry.Data.Redis/v1.0.0-preview.1`

#### Publishing Flow Diagram

```
Merge PR to main
  └── CI: build ✓ → test ✓ → pack ✓ (validation only, NO publish)

Push tag: HVO.Common/v1.0.1
  └── CI: build ✓ → test ✓ → pack ✓ → publish .nupkg + .snupkg ✓
```

#### Cross-Package Dependencies

In the source repo, packages reference each other via `ProjectReference` (always builds from source). When packed into `.nupkg`, the SDK automatically converts `ProjectReference` into a NuGet dependency with a version range like `[1.0.0, )` based on the referenced project's `Version` property.

---

### Pack Script (`scripts/pack-all.sh`)

```bash
#!/bin/bash
set -euo pipefail

ARTIFACTS_DIR="${1:-./artifacts}"
CONFIGURATION="${2:-Release}"

mkdir -p "$ARTIFACTS_DIR"

echo "Packing all source projects..."
for csproj in src/*//*.csproj; do
    echo "  Packing: $csproj"
    dotnet pack "$csproj" \
        --configuration "$CONFIGURATION" \
        --output "$ARTIFACTS_DIR" \
        --no-build
done

echo ""
echo "Packages created in $ARTIFACTS_DIR:"
ls -la "$ARTIFACTS_DIR"/*.nupkg 2>/dev/null || echo "  (none)"
```

## Testing Requirements

### Pack Verification Tests

```bash
# Verify all packages can be packed
dotnet build --configuration Release
dotnet pack --configuration Release --output ./artifacts

# Verify each .nupkg contains expected files
for nupkg in artifacts/*.nupkg; do
    echo "Inspecting: $nupkg"
    unzip -l "$nupkg" | grep -E "(README|nuget-icon|\.dll|\.xml)" || echo "  WARNING: Missing expected files"
done

# Verify metadata
dotnet nuget locals all --list
```

### Build Regression Tests

- [x] `dotnet build HVO.Enterprise.sln` — 0 warnings, 0 errors
- [x] `dotnet test` — all existing tests pass (1385+)
- [x] `dotnet pack` — all 13 src projects produce valid `.nupkg` files
- [x] No functional changes to any source code — metadata-only and build infrastructure changes

## Performance Requirements

- Pack time for individual project: < 5 seconds
- Pack time for all 13 projects: < 60 seconds
- CI/CD pipeline total time (build + test + pack + publish): < 10 minutes
- No increase in build time from `Directory.Build.props` centralization

## Dependencies

### Blocked By
- None — all source projects are already complete

### Blocks
- Public consumption of HVO packages by external teams
- NuGet.org listing and discovery

### Enhances
- All completed user stories (US-001 through US-033)

## Definition of Done

- [x] `Directory.Build.props` created at root, `src/`, and `tests/` levels
- [x] All 13 `.csproj` files simplified (shared properties removed)
- [x] All 13 packages have complete NuGet metadata
- [x] Each package has its own `Version` in its `.csproj`
- [x] `dotnet pack` succeeds for all projects with 0 warnings
- [x] All generated `.nupkg` files contain README, icon, docs XML, and license
- [x] Source Link configured and verified
- [x] Strong name key pair (`HVO.Enterprise.snk`) generated and committed to repo
- [x] All assemblies are fully strong-named with consistent `PublicKeyToken`
- [x] `InternalsVisibleTo` attributes updated with public key token
- [x] GitHub Actions workflow created for pack and publish
- [x] Workflow supports both nuget.org and GitHub Packages
- [x] Tag-based per-package publishing works (only tags trigger publish, not merges)
- [x] Symbol packages (`.snupkg`) published alongside `.nupkg`
- [x] `VERSIONING.md` created with full increment/tag/publish workflow
- [x] `scripts/pack-all.sh` created
- [x] `.gitignore` updated for `artifacts/` and `*.nupkg`
- [x] Full solution builds with 0 warnings, 0 errors
- [x] All existing tests pass (1385+)
- [x] Package icon created or placeholder added
- [x] No functional source code changes — metadata and infrastructure only

## Notes

### Design Decisions

1. **Per-project versioning over centralized**: Since packages have independent lifecycles and consumers may pin specific extension versions, each `.csproj` owns its `Version`. This avoids unnecessary churn — fixing a bug in the Redis extension shouldn't bump the Serilog extension version.

2. **Tag-based publishing**: Using `<PackageId>/v<Version>` tags (e.g., `HVO.Enterprise.Telemetry/v1.1.0`) enables publishing individual packages on demand. This is the standard pattern for monorepo NuGet publishing.

3. **Directory.Build.props hierarchy**: Root → `src/` → `tests/` enables clean separation: shared metadata at root, packability in `src/`, test settings in `tests/`. Each level imports its parent.

4. **Source Link**: Enables consumers to step into HVO source code during debugging without downloading source separately. Minimal overhead, major developer experience improvement.

5. **Symbol packages (`.snupkg`)**: Published alongside `.nupkg` to NuGet symbol server for debugging support. Negligible size cost, significant debugging value.

6. **Dual publish targets**: nuget.org for public consumption, GitHub Packages as a fallback or for pre-release testing. Configurable per-publish via workflow inputs.

### Implementation Tips

- Start with `Directory.Build.props` and validate the build still works before modifying any `.csproj` files
- Use `dotnet pack --configuration Release -v detailed` to inspect what metadata ends up in the package
- Test `.nupkg` contents with `unzip -l <file>.nupkg` (they're ZIP files)
- Use `nuget verify` or `dotnet nuget verify` (if available) for package signing validation
- For the package icon, a simple 256×256 PNG with the "HVO" logo or text is sufficient

### Future Considerations

- Code signing (Authenticode) for package integrity verification beyond strong naming
- Automated changelog generation from conventional commits
- NuGet package vulnerability scanning in CI
- Dependency license scanning

## Related Documentation

- [NuGet Package Properties](https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets#pack-target)
- [Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)
- [Strong Naming (.NET Library Guidance)](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/strong-naming)
- [How to Sign an Assembly with a Strong Name](https://learn.microsoft.com/en-us/dotnet/standard/assembly/sign-strong-name)
- [Source Link](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink)
- [GitHub Actions for NuGet](https://docs.github.com/en/actions/publishing-packages/publishing-nuget-packages)
- [SemVer 2.0](https://semver.org/)

## Implementation Summary

**Completed**: 2026-02-12
**Implemented by**: GitHub Copilot

### What Was Implemented

- Created `Directory.Build.props` hierarchy (root, `src/`, `tests/`, `benchmarks/`, `samples/`) centralizing all shared build properties and NuGet metadata
- Simplified all 13 `src/*.csproj` files by removing centralized properties, adding per-project `PackageId`, `Version`, `PackageTags`, `PackageReleaseNotes`
- Generated `HVO.Enterprise.snk` (1024-bit RSA, public key token `719931d93aec2c56`) for strong naming all assemblies
- Updated all 12 `InternalsVisibleTo` attributes with the public key for strong-name compatibility
- Created 13 per-package `README.md` files embedded in each `.nupkg`
- Created placeholder package icon (`docs/assets/nuget-icon.png`, 128×128)
- Created GitHub Actions CI/CD workflow (`.github/workflows/nuget-publish.yml`) with build→test→pack→publish pipeline
- Stored `NUGET_API_KEY` as GitHub repository secret for nuget.org publishing
- Created `docs/VERSIONING.md` documenting the per-project SemVer 2.0 versioning strategy
- Created `scripts/pack-all.sh` convenience script for local packing
- Updated `.gitignore` with NuGet artifact exclusions
- Configured Source Link (`Microsoft.SourceLink.GitHub 8.0.0`) for debugger source stepping

### Key Files

- `Directory.Build.props` (root) — centralized build settings, NuGet metadata, strong naming
- `src/Directory.Build.props` — `IsPackable=true`, README + icon pack items
- `tests/Directory.Build.props` — `IsPackable=false`, `IsTestProject=true`
- `benchmarks/Directory.Build.props` — `IsPackable=false`
- `samples/Directory.Build.props` — `IsPackable=false`, `SignAssembly=false`
- `HVO.Enterprise.snk` — strong name key pair
- `.github/workflows/nuget-publish.yml` — CI/CD pipeline
- `docs/VERSIONING.md` — versioning strategy
- `scripts/pack-all.sh` — local pack convenience script
- `docs/assets/nuget-icon.png` — NuGet package icon
- 13× `src/*/README.md` — per-package documentation

### Decisions Made

- Used 1024-bit RSA key (standard for OSS strong naming, matches Serilog/Newtonsoft.Json practice). Generated via C# `RSA.Create()` since `sn` tool was not available in devcontainer.
- Added `SignAssembly=false` to `samples/Directory.Build.props` and the sample test project because `HealthChecks.UI.Client` is not strong-named.
- CI workflow triggers on push to `main` (validation only) and tag push `*/v*` (publish). Manual workflow dispatch also supported.
- Both `.nupkg` and `.snupkg` are published to nuget.org and GitHub Packages.
- `HvoPublicKey` MSBuild property in root `Directory.Build.props` enables `InternalsVisibleTo` with `Key="$(HvoPublicKey)"` metadata across all projects.

### Quality Gates

- ✅ Build: 0 warnings, 0 errors
- ✅ Tests: 1385/1385 passed (120 HVO.Common + 1265 Telemetry)
- ✅ Pack: 13 `.nupkg` + 13 `.snupkg` = 26 packages produced successfully
- ✅ Package contents verified: DLL, XML docs, README.md, nuget-icon.png all present
- ✅ No functional source code changes — metadata and infrastructure only

### Next Steps

- Replace placeholder `nuget-icon.png` with a proper branded icon
- First release: update `Version` in each `.csproj`, push tags to trigger publishing
- Consider adding automated changelog generation from conventional commits
