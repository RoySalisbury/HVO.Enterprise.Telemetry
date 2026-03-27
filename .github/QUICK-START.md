# Quick Start: Using GitHub Issue Templates

This guide shows you how to create a GitHub Issue from a user story in just a few minutes.

## Example: Creating Issue for US-001

Let's walk through creating an issue for **US-001: Core Package Setup and Dependencies**.

### Step 1: Open the Issue Template

Click here: [Create New User Story](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/new?template=user-story.yml)

Or navigate manually:
1. Go to the [Issues page](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues)
2. Click the green "New issue" button
3. Select "💡 User Story" → Click "Get started"

### Step 2: Fill in Basic Information

**Story ID**: `US-001`

**Category**: Select `Core Package` from dropdown

**Story Points**: Select `3` from dropdown

**Sprint**: Enter `Sprint 1`

### Step 3: Fill in Description

Copy from the markdown file `docs/user-stories/US-001-core-package-setup.md`:

```
As a **library developer**,
I want to **set up the core HVO.Enterprise.Telemetry package with proper dependencies and folder structure**,
So that **I have a solid foundation for implementing telemetry features with .NET Standard 2.0 compatibility**.
```

### Step 4: Fill in Acceptance Criteria

Copy the entire Acceptance Criteria section from the markdown file:

```markdown
## Acceptance Criteria

1. **Project Creation**
   - [ ] `HVO.Enterprise.Telemetry.csproj` created targeting `netstandard2.0`
   - [ ] Project builds successfully with zero warnings
   - [ ] Package metadata configured (Version, Authors, Description, etc.)

2. **Dependencies Configured**
   - [ ] `System.Diagnostics.DiagnosticSource` v8.0.1 added
   - [ ] `OpenTelemetry.Api` v1.9.0 added
   - [ ] `Microsoft.Extensions.Logging.Abstractions` added
   - [ ] `Microsoft.Extensions.DependencyInjection.Abstractions` added
   - [ ] `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions` v8.0.0 added
   - [ ] `Microsoft.Extensions.Configuration.Abstractions` added
   - [ ] `System.Threading.Channels` v7.0.0 added
   - [ ] `System.Net.Http` added

3. **Folder Structure Created**
   - [ ] `Abstractions/` - Interfaces and base classes
   - [ ] `ActivitySources/` - Activity and tracing infrastructure
   - [ ] `Metrics/` - Metrics collection and recording
   - [ ] `Correlation/` - Correlation ID management
   - [ ] `Proxies/` - DispatchProxy implementation
   - [ ] `Http/` - HTTP client instrumentation
   - [ ] `HealthChecks/` - Health check implementations
   - [ ] `Configuration/` - Configuration management
   - [ ] `Lifecycle/` - Lifecycle and shutdown management
   - [ ] `Enrichers/` - Context enrichers
   - [ ] `BackgroundJobs/` - Background job correlation
   - [ ] `Exceptions/` - Exception tracking and aggregation
   - [ ] `Logging/` - ILogger integration

4. **Language Configuration**
   - [ ] `<LangVersion>latest</LangVersion>` set
   - [ ] `<Nullable>enable</Nullable>` enabled
   - [ ] `<ImplicitUsings>disable</ImplicitUsings>` disabled
   - [ ] `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` set
```

### Step 5: Fill in Technical Requirements

Copy the Technical Requirements section:

```markdown
### Project File Configuration

\`\`\`xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    
    <!-- Package Information -->
    <PackageId>HVO.Enterprise.Telemetry</PackageId>
    <Version>1.0.0-preview.1</Version>
    <Authors>HVO Enterprise</Authors>
    <Description>Core telemetry library for unified observability across .NET platforms</Description>
    ...
  </PropertyGroup>
</Project>
\`\`\`

### Folder Structure Details

Each folder should contain:
- Placeholder `.gitkeep` file initially
- Namespace matching: `HVO.Enterprise.Telemetry.{FolderName}`
- README.md explaining folder purpose (optional for v1.0)

### Compatibility Constraints

- **Must work on**: .NET Framework 4.8, .NET Core 2.0+, .NET 5+, .NET 6+, .NET 8+
- **Language features**: Limited to C# features available in .NET Standard 2.0
...
```

### Step 6: Fill in Testing Requirements

```markdown
### Unit Tests

1. **Build Verification**
   - [ ] Project builds successfully with `dotnet build`
   - [ ] No compilation warnings
   - [ ] Documentation XML file generated

2. **Compatibility Tests**
   - [ ] Reference project from .NET Framework 4.8 test project
   - [ ] Reference project from .NET 8 test project
   - [ ] Both projects build and reference successfully

3. **Package Tests**
   - [ ] `dotnet pack` creates NuGet package successfully
   - [ ] Package contains correct assemblies
   - [ ] Package metadata is correct
```

### Step 7: Fill in Dependencies

```markdown
**Blocked By**: None (this is the first story)
**Blocks**: All other core package stories (US-002 through US-018)
```

### Step 8: Fill in Definition of Done

```markdown
- [ ] Project file created with all required dependencies
- [ ] All folder structure in place with `.gitkeep` files
- [ ] Project builds with zero warnings
- [ ] Package can be created with `dotnet pack`
- [ ] Successfully referenced from both .NET Framework 4.8 and .NET 8 projects
- [ ] Code reviewed and approved
- [ ] Committed to feature branch
```

### Step 9: Fill in Additional Notes

Copy the Notes section about design decisions, implementation tips, etc.

### Step 10: Fill in Related Documentation

```markdown
- [Project Plan](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/docs/project-plan.md#1-create-core-netstandard20-package-with-microsoft-abstractions)
- [Coding Standards](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/blob/main/.github/copilot-instructions.md)
- [.NET Standard 2.0 API Reference](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0)
```

### Step 11: Submit the Issue

Click "Submit new issue" button at the bottom.

### Step 12: Add Additional Labels

After the issue is created, add these labels:
1. Click "Labels" in the right sidebar
2. Add: `core-package`
3. Add: `sp-3`
4. Add: `sprint-1`
5. Add: `priority-high` (if applicable)
6. Add: `ready` (when ready to start)

The issue template will automatically add `user-story` and `needs-triage` labels.

## Result

Your issue will look like this:

**Issue #1**: [USER STORY] US-001: Core Package Setup and Dependencies

**Labels**: 
- 🟢 `user-story` (auto-added)
- 🟢 `sp-3` (manually added)
- 🟣 `sprint-1` (manually added)
- 🔵 `core-package` (manually added)
- ⚪ `needs-triage` (auto-added)

**Body**: Full structured user story with all sections

**Checkboxes**: All acceptance criteria and definition of done items are clickable checkboxes that update when checked!

## Tracking Progress

As work progresses:

1. **Check off items** - Click the checkboxes in Acceptance Criteria and Definition of Done
2. **Update labels** - Change `needs-triage` → `ready` → `in-progress` → `in-review`
3. **Link PRs** - Reference the issue in PR descriptions: "Closes #1"
4. **Add comments** - Discuss implementation details, blockers, etc.

## Viewing Your Backlog

Use these filters to view different issue sets:

- **All user stories**: `is:issue label:user-story`
- **Sprint 1**: `is:issue label:sprint-1`
- **Ready to work**: `is:issue label:ready`
- **Core package**: `is:issue label:core-package`
- **Your stories**: `is:issue assignee:@me label:user-story`

## Tips

- ✅ **Use copy-paste** - Copy entire sections from markdown files
- ✅ **Keep formatting** - Markdown works in issue body
- ✅ **Fill everything** - Complete all sections for consistency
- ✅ **Link issues** - Reference other issues with #number
- ✅ **Add labels** - Labels enable filtering and organization

- ❌ **Don't skip sections** - Incomplete stories are harder to work with
- ❌ **Don't forget labels** - They're essential for organization
- ❌ **Don't ignore dependencies** - Track what blocks what

## Next Stories

After US-001, continue with:
- US-019: HVO.Common Library (can be done in parallel)
- US-002: Auto-Managed Correlation
- US-005: Lifecycle Management
- US-004: Bounded Queue Worker

See `docs/user-stories/SUMMARY.md` for the complete implementation order.

## Getting Help

- **Helper script**: Run `./scripts/create-issues-helper.sh` for quick info
- **Full guide**: See `.github/CREATING-ISSUES.md`
- **Labels**: See `.github/LABELS.md`
- **Templates**: See `.github/ISSUE_TEMPLATE/`

---

**Pro Tip**: Create issues in batches for a sprint, then use a GitHub Project board to track progress visually!
