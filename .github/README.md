# GitHub Configuration for HVO.Enterprise

This directory contains GitHub-specific configuration for issue templates, labels, and automation.

## 📋 Issue Templates

We use structured issue templates to maintain consistency and quality:

### 💡 User Story Template (`ISSUE_TEMPLATE/user-story.yml`)

The primary template for creating user stories following Azure DevOps style structure:

- **Pre-filled sections** with "As a [user], I want [goal], so that [benefit]" format
- **Trackable checklists** in Acceptance Criteria and Definition of Done
- **Story points** and sprint tracking
- **Dependencies** tracking between stories
- **Technical requirements** with code samples
- **Testing requirements** for validation

**When to use**: For all feature development work and planned enhancements

### 🐛 Bug Report Template (`ISSUE_TEMPLATE/bug-report.yml`)

Structured template for reporting bugs with:

- Reproduction steps
- Expected vs actual behavior
- Environment details
- Severity classification

**When to use**: When reporting bugs or unexpected behavior

### ✨ Feature Request Template (`ISSUE_TEMPLATE/feature-request.yml`)

Template for suggesting new features:

- Problem statement
- Proposed solution
- Use cases
- Acceptance criteria

**When to use**: For new feature suggestions not yet in the backlog

## 🏷️ Labels System

See `LABELS.md` for complete label documentation.

### Key Label Categories

- **Type**: `user-story`, `bug`, `enhancement`, `documentation`
- **Status**: `needs-triage`, `ready`, `in-progress`, `in-review`, `blocked`
- **Priority**: `priority-critical`, `priority-high`, `priority-medium`, `priority-low`
- **Category**: `core-package`, `extension-package`, `testing`, `samples`, `infrastructure`
- **Story Points**: `sp-1`, `sp-2`, `sp-3`, `sp-5`, `sp-8`, `sp-13`, `sp-21`
- **Sprint**: `sprint-1`, `sprint-2`, etc.

### Setting Up Labels

```bash
# Use GitHub CLI to create all labels
cd .github
bash setup-labels.sh  # (create this script based on LABELS.md)
```

Or manually create them via GitHub UI: Repository → Issues → Labels

## 📝 Creating Issues from User Stories

We have comprehensive user story documentation in `docs/user-stories/`. To convert these to GitHub issues:

### Option 1: Manual Creation (Recommended)

1. Go to [Issues → New Issue](https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/new/choose)
2. Select "User Story" template
3. Copy content from the corresponding markdown file in `docs/user-stories/`
4. Apply appropriate labels
5. Create the issue

### Option 2: Helper Script

```bash
# Run the helper script to see all stories and get creation URLs
./scripts/create-issues-helper.sh
```

### Option 3: Automated Creation

See `CREATING-ISSUES.md` for detailed instructions on:
- Batch creation using GitHub CLI
- Python script for automated conversion
- API-based creation

## 📚 Documentation

- **LABELS.md** - Complete label system and usage guidelines
- **CREATING-ISSUES.md** - Guide for converting user stories to issues
- **copilot-instructions.md** - Development guidelines and coding standards

## 🔄 Workflow

### For New Work

1. **Create issue** using appropriate template
2. **Apply labels** (type, category, priority, story points, sprint)
3. **Link dependencies** in the Dependencies field
4. **Move to "Ready"** when dependencies are met
5. **Assign to sprint/milestone** for planning

### During Development

1. **Update status label** (`in-progress`, `in-review`)
2. **Check off items** in Acceptance Criteria and Definition of Done
3. **Link PRs** to the issue
4. **Update blockers** if any arise

### On Completion

1. **Verify Definition of Done** is complete
2. **Ensure PR is merged**
3. **Close issue** with reference to PR
4. **Update dependent issues** (remove blocks)

## 🤖 Automation Opportunities

Consider setting up:

### GitHub Actions
- Auto-label based on template used
- Auto-assign to project board
- Notify on critical/blocked issues
- Update sprint milestones

### Project Boards
- Kanban board with columns: Backlog, Ready, In Progress, In Review, Done
- Auto-move based on status labels
- Sprint planning views

### Link Checking
- Validate dependency links between issues
- Check for circular dependencies
- Warn on unlinked blocked issues

## 🎯 Best Practices

### Creating User Stories

- ✅ **DO**: Use the template completely
- ✅ **DO**: Make acceptance criteria specific and testable
- ✅ **DO**: Include code examples in technical requirements
- ✅ **DO**: Link to dependent issues by number
- ✅ **DO**: Apply all relevant labels

- ❌ **DON'T**: Create blank issues
- ❌ **DON'T**: Skip sections in the template
- ❌ **DON'T**: Forget to link dependencies
- ❌ **DON'T**: Leave without applying labels

### Managing Issues

- **Triage regularly**: Review `needs-triage` issues daily
- **Update status**: Keep status labels current
- **Track blockers**: Mark and track blocked issues
- **Close completed**: Don't leave done issues open
- **Link related work**: Connect issues and PRs

### Using Labels Effectively

- **Minimum labels**: Every issue should have type, status, and priority
- **User stories**: Also need category, story points, and sprint
- **Consistency**: Use the same labels for similar work
- **Filters**: Create saved filters for common views

## 📞 Getting Help

- **Template issues**: Check the `.yml` files in `ISSUE_TEMPLATE/`
- **Label questions**: See `LABELS.md`
- **Conversion help**: See `CREATING-ISSUES.md`
- **Development**: See `copilot-instructions.md`

## 🔧 Maintenance

### Updating Templates

Edit the `.yml` files in `ISSUE_TEMPLATE/`. Changes take effect immediately.

### Adding New Labels

Update `LABELS.md` and create labels via GitHub UI or CLI.

### New Issue Types

Create a new `.yml` file in `ISSUE_TEMPLATE/` following the existing pattern.

---

**Last Updated**: 2026-02-07  
**Maintained By**: Development Team
