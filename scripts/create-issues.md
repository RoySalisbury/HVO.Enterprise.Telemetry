# Creating GitHub Issues from User Stories

This document provides instructions and tools for creating GitHub issues from the user story markdown files.

## Overview

All 30 user stories have been created as markdown files in `docs/user-stories/`. These need to be converted into GitHub issues using the `user-story.yml` template.

## Completed User Stories

According to SUMMARY.md, the following stories are already complete:
- **US-001**: Core Package Setup - ✅ Complete
- **US-002**: Auto-Managed Correlation - ✅ Complete  
- **US-003**: Background Job Correlation - ✅ Complete
- **US-004**: Bounded Queue Worker - ✅ Complete
- **US-019**: HVO.Common Library - ✅ Complete

These should be marked with appropriate labels when creating issues.

## All User Stories to Create

### Core Package (US-001 to US-018) - 79 SP

| Story | Title | Status | SP | Sprint |
|-------|-------|--------|-----|---------|
| US-001 | Core Package Setup | ✅ Complete | 3 | Sprint 1 |
| US-002 | Auto-Managed Correlation | ✅ Complete | 5 | Sprint 1 |
| US-003 | Background Job Correlation | ✅ Complete | 5 | Sprint 3 |
| US-004 | Bounded Queue Worker | ✅ Complete | 8 | Sprint 2 |
| US-005 | Lifecycle Management | ❌ Not Started | 5 | Sprint 1 |
| US-006 | Runtime-Adaptive Metrics | ❌ Not Started | 8 | Sprint 2 |
| US-007 | Exception Tracking | ❌ Not Started | 3 | Sprint 5 |
| US-008 | Configuration Hot Reload | ❌ Not Started | 5 | Sprint 5 |
| US-009 | Multi-Level Configuration | ❌ Not Started | 5 | Sprint 2 |
| US-010 | ActivitySource Sampling | ❌ Not Started | 5 | Sprint 3 |
| US-011 | Context Enrichment | ❌ Not Started | 5 | Sprint 5 |
| US-012 | Operation Scope | ❌ Not Started | 8 | Sprint 3 |
| US-013 | ILogger Enrichment | ❌ Not Started | 5 | Sprint 4 |
| US-014 | DispatchProxy Instrumentation | ❌ Not Started | 8 | Sprint 6 |
| US-015 | Parameter Capture | ❌ Not Started | 5 | Sprint 6 |
| US-016 | Statistics & Health Checks | ❌ Not Started | 5 | Sprint 4 |
| US-017 | HTTP Instrumentation | ❌ Not Started | 3 | Sprint 5 |
| US-018 | DI & Static Initialization | ❌ Not Started | 5 | Sprint 4 |

### Extension Packages (US-019 to US-025) - 34 SP

| Story | Title | Status | SP | Sprint |
|-------|-------|--------|-----|---------|
| US-019 | HVO.Common Library | ✅ Complete | 5 | Sprint 6 |
| US-020 | IIS Extension | ❌ Not Started | 3 | Sprint 7 |
| US-021 | WCF Extension | ❌ Not Started | 5 | Sprint 7 |
| US-022 | Database Extension | ❌ Not Started | 8 | Sprint 7 |
| US-023 | Serilog Extension | ❌ Not Started | 3 | Sprint 8 |
| US-024 | AppInsights Extension | ❌ Not Started | 5 | Sprint 8 |
| US-025 | Datadog Extension | ❌ Not Started | 5 | Sprint 8 |

### Testing & Samples (US-026 to US-028) - 56 SP

| Story | Title | Status | SP | Sprint |
|-------|-------|--------|-----|---------|
| US-026 | Unit Test Project | ❌ Not Started | 30 | Sprint 9 |
| US-027 | .NET Framework 4.8 Sample | ❌ Not Started | 13 | Sprint 10 |
| US-028 | .NET 8 Sample | ❌ Not Started | 13 | Sprint 10 |

### Documentation (US-029 to US-030) - 11 SP

| Story | Title | Status | SP | Sprint |
|-------|-------|--------|-----|---------|
| US-029 | Project Documentation | ❌ Not Started | 8 | Sprint 10 |
| US-030 | Future Extensibility | ❌ Not Started | 3 | Sprint 10 |

**Total**: 180 Story Points across 30 stories

## Method 1: Manual Creation via GitHub UI

For each user story:

1. Open the [Create User Story Issue](https://github.com/RoySalisbury/HVO.Enterprise/issues/new?template=user-story.yml) link
2. Fill in the fields from the corresponding markdown file:
   - **Story ID**: From the filename (e.g., US-001)
   - **Category**: From the markdown file header
   - **Story Points**: From the markdown file header
   - **Sprint**: From the table above
   - **Description**: Copy from "Description" section
   - **Acceptance Criteria**: Copy from "Acceptance Criteria" section
   - **Technical Requirements**: Copy from "Technical Requirements" section
   - **Testing Requirements**: Copy from "Testing Requirements" section
   - **Dependencies**: Copy from "Dependencies" section
   - **Definition of Done**: Copy from "Definition of Done" section
   - **Additional Notes**: Copy from "Notes" section
3. Add appropriate labels:
   - `user-story` (automatically added)
   - `sp-X` where X is the story points
   - `sprint-X` where X is the sprint number
   - `status:complete` for completed stories (US-001, US-002, US-003, US-004, US-019)
   - `status:not-started` for others
   - Category label: `core-package`, `extension-package`, `testing`, or `documentation`
4. Create the issue

## Method 2: Programmatic Creation (Requires GitHub Token)

### Prerequisites
- GitHub CLI installed and authenticated
- Or GitHub Personal Access Token with `repo` scope

### Using the Provided Script

Use `scripts/create-github-issues.sh` to automate issue creation with the GitHub CLI:

```bash
# Dry run (prints the title/labels/body path per story)
./scripts/create-github-issues.sh

# Create all issues
./scripts/create-github-issues.sh --no-dry-run
```

This script mirrors the manual process, including completed-story labels and the full issue body content.

## Method 3: Import Tool

Consider using a GitHub issue import tool or the GitHub GraphQL API for bulk creation.

## Label Scheme

### Status Labels
- `status:complete` - Story has been implemented and merged
- `status:in-progress` - Story is currently being worked on
- `status:not-started` - Story has not been started yet
- `status:blocked` - Story is blocked by dependencies

### Story Point Labels
- `sp-1`, `sp-2`, `sp-3`, `sp-5`, `sp-8`, `sp-13`, `sp-21`, `sp-30`

### Sprint Labels
- `sprint-1` through `sprint-10`

### Category Labels
- `core-package` - Core telemetry functionality
- `extension-package` - Platform-specific extensions
- `testing` - Test projects and frameworks
- `documentation` - Documentation and guides

### Priority Labels
- `priority:p0` - Critical, must-have features
- `priority:p1` - Important features
- `priority:p2` - Nice-to-have features
- `priority:p3` - Future enhancements

## Dependencies and Blocking Relationships

When creating issues, ensure dependencies are linked using "Blocked by" and "Blocks" relationships:

### Foundation Dependencies
- US-005, US-006, US-009, US-010 blocked by US-001
- US-007, US-008, US-011 blocked by US-002
- US-012, US-013, US-014, US-015 blocked by US-004

### Feature Dependencies
- US-013 blocked by US-012
- US-014, US-015 blocked by US-012
- US-016 blocked by US-006, US-007
- US-017 blocked by US-002, US-010
- US-018 blocked by US-012, US-013, US-016

### Extension Dependencies
- All extensions (US-020 to US-025) blocked by US-001, US-002, US-018
- US-021 also blocked by US-003
- US-022 also blocked by US-012

### Testing Dependencies
- US-026 blocked by all core features (US-001 through US-018)
- US-027, US-028 blocked by US-026 and key extensions

## Verification Checklist

After creating all issues:

- [ ] All 30 issues created
- [ ] All issues have proper labels (user-story, sp-X, sprint-X, category)
- [ ] Completed stories (US-001, US-002, US-003, US-004, US-019) marked with `status:complete`
- [ ] All dependencies linked between issues
- [ ] All acceptance criteria formatted as checkboxes
- [ ] All definition of done items formatted as checkboxes
- [ ] Issue titles follow format: `[USER STORY]: US-XXX - Title`

## Next Steps

1. Create all GitHub issues from the user stories
2. Set up GitHub Project board to track progress
3. Link issues to sprints and milestones
4. Begin implementation following the sprint schedule
