# Scripts Directory

This directory contains automation scripts for managing the HVO.Enterprise project.

## Available Scripts

### 1. `create-issues.md`

**Purpose**: Documentation for creating GitHub issues from user stories.

**Description**: Comprehensive guide explaining:
- How to create GitHub issues manually from user story markdown files
- List of all 30 user stories with their status, story points, and sprint assignments
- Completed stories that need to be marked as such
- Label scheme and dependency relationships
- Verification checklist

**Usage**:
```bash
# Read the documentation
cat scripts/create-issues.md
```

### 2. `create-github-issues.sh`

**Purpose**: Shell script that creates GitHub issues directly via the `gh` CLI.

**Description**: Parses every user story markdown file, builds the full issue body (Description, Acceptance Criteria, Technical Requirements, etc.), applies the correct label set, and optionally creates the issue immediately.

**Requirements**:
```bash
gh auth login      # GitHub CLI authentication
jq --version       # jq is bundled in the dev container
```

**Usage**:
```bash
# Dry run (default)
./scripts/create-github-issues.sh

# Actually create issues
./scripts/create-github-issues.sh --no-dry-run

# Override the destination repository
./scripts/create-github-issues.sh --repo other-owner/other-repo --no-dry-run
```

**Notes**:
- Dry run mode shows the computed title/labels/body path for each story without calling GitHub.
- The script enforces the same completed-story mapping (US-001/2/3/4/19) and label scheme as the historical Python version.
- Issues are created using `gh issue create --body-file <tempfile>`, so no Python runtime is required.

### 3. `generate-issue-commands.sh` ⭐ **RECOMMENDED**

**Purpose**: Generate GitHub CLI commands for creating issues.

**Description**: Parses all user story markdown files and generates a shell script with `gh issue create` commands. This is the recommended approach as it:
- Uses the GitHub CLI (no API token setup needed)
- Generates a reviewable script before execution
- Allows selective issue creation
- Properly handles completed vs. not-started stories

**Usage**:
```bash
# Step 1: Generate the creation script
./scripts/generate-issue-commands.sh > create-all-issues.sh

# Step 2: Review the generated script
cat create-all-issues.sh

# Step 3: Make it executable
chmod +x create-all-issues.sh

# Step 4: Authenticate with GitHub (if not already)
gh auth login

# Step 5: Execute the script to create all issues
./create-all-issues.sh

# Optional: Create issues selectively
# Edit create-all-issues.sh to comment out stories you don't want to create
```

## Issue Creation Workflow

### Quick Start (Recommended Method)

```bash
# 1. Ensure you have GitHub CLI installed
gh --version

# 2. Authenticate (if needed)
gh auth login

# 3. Generate the issue creation script
cd /home/runner/work/HVO.Enterprise/HVO.Enterprise
./scripts/generate-issue-commands.sh > create-all-issues.sh

# 4. Review the script
less create-all-issues.sh

# 5. Execute to create all 30 issues
chmod +x create-all-issues.sh
./create-all-issues.sh
```

### Completed Stories

The following stories are marked as complete and will have the `status:complete` label:

- **US-001**: Core Package Setup (3 SP)
- **US-002**: Auto-Managed Correlation (5 SP)
- **US-003**: Background Job Correlation (5 SP)
- **US-004**: Bounded Queue Worker (8 SP)
- **US-019**: HVO.Common Library (5 SP)

All other stories will be marked as `status:not-started`.

### Label Scheme

Each issue will be automatically labeled with:

**Status Labels**:
- `status:complete` - For completed stories (US-001, US-002, US-003, US-004, US-019)
- `status:not-started` - For all other stories

**Story Points**:
- `sp-1`, `sp-2`, `sp-3`, `sp-5`, `sp-8`, `sp-13`, `sp-21`, `sp-30`

**Sprint Assignment**:
- `sprint-1` through `sprint-10`

**Category**:
- `core-package` - Core telemetry functionality (US-001 to US-018)
- `extension-package` - Platform extensions (US-019 to US-025)
- `testing` - Test projects (US-026 to US-028)
- `documentation` - Documentation (US-029 to US-030)

**Priority** (auto-assigned based on sprint):
- `priority:p0` - Sprints 1-2 (foundation)
- `priority:p1` - Sprints 3-4 (core features)
- `priority:p2` - Sprints 5-8 (extensions)
- `priority:p3` - Sprints 9-10 (testing & docs)

## Issue Titles

All issues follow the format:
```
[USER STORY]: US-XXX - Title
```

Examples:
- `[USER STORY]: US-001 - Core Package Setup and Dependencies`
- `[USER STORY]: US-019 - HVO.Common Library`

## Verification

After creating all issues, verify:

```bash
# Check total issue count
gh issue list --repo RoySalisbury/HVO.Enterprise --label user-story | wc -l
# Should show: 30

# Check completed stories
gh issue list --repo RoySalisbury/HVO.Enterprise --label status:complete | wc -l
# Should show: 5

# Check not-started stories
gh issue list --repo RoySalisbury/HVO.Enterprise --label status:not-started | wc -l
# Should show: 25

# List all user story issues
gh issue list --repo RoySalisbury/HVO.Enterprise --label user-story
```

## Troubleshooting

### GitHub CLI not installed

```bash
# macOS
brew install gh

# Linux (Debian/Ubuntu)
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
sudo apt update
sudo apt install gh

# Windows
winget install --id GitHub.cli
```

### Authentication issues

```bash
# Check authentication status
gh auth status

# Re-authenticate
gh auth logout
gh auth login
```

### Issue body too large

If you encounter "body is too large" errors, the markdown files may need to be truncated. The GitHub API has a limit on issue body size.

Solution:
- Manually create the issue with a shorter body
- Add the full content as a comment after creation
- Or link to the markdown file in the repository

## Project Context

### User Story Files

All user stories are located in: `/home/runner/work/HVO.Enterprise/HVO.Enterprise/docs/user-stories/`

Files:
- `US-001-core-package-setup.md` through `US-030-future-extensibility.md`
- Supporting files: `README.md`, `SUMMARY.md`, `CREATION-GUIDE.md`, etc.

### Implementation Status

Based on the current repository state:

✅ **Completed** (implementation exists):
- US-001: Core Package Setup - HVO.Common project structure exists
- US-002, US-003, US-004: Partial implementation in HVO.Common
- US-019: HVO.Common Library - Main implementation exists

❌ **Not Started** (25 stories):
- US-005 through US-018: Core package features
- US-020 through US-025: Extension packages
- US-026 through US-030: Testing, samples, documentation

### Total Effort

- **30 stories**: 180 story points total
- **10 sprints**: ~18 SP per sprint average
- **Estimated duration**: 8-10 weeks with 2-person team

## Next Steps

1. ✅ All user story markdown files created (30/30)
2. ⬜ Create GitHub issues from markdown files using the scripts
3. ⬜ Set up GitHub Project board for tracking
4. ⬜ Create milestones for each sprint
5. ⬜ Link issues to milestones
6. ⬜ Begin implementation following sprint schedule

## Support

For questions or issues with these scripts:
1. Check the [CREATING-ISSUES.md](../.github/CREATING-ISSUES.md) guide
2. Review existing user story files as examples
3. Consult the [user-story.yml](../.github/ISSUE_TEMPLATE/user-story.yml) template
4. Check the project plan at [docs/project-plan.md](../docs/project-plan.md)
