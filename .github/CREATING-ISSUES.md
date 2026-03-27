# Creating GitHub Issues from User Stories

This guide explains how to convert the user story documentation files in `docs/user-stories/` into GitHub Issues using the structured templates.

## Quick Start

### Option 1: Manual Creation (Recommended for First Few Stories)

1. **Go to GitHub Issues**: Navigate to https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues
2. **Click "New Issue"**: Green button in top-right
3. **Select "User Story" template**: Click "Get started" next to 💡 User Story
4. **Fill in the form** using the content from your markdown file

### Option 2: Using GitHub CLI (Batch Creation)

For bulk creation of issues from existing user stories:

```bash
# Install GitHub CLI if needed
# https://cli.github.com/

# Login to GitHub
gh auth login

# Navigate to repository
cd /path/to/HVO.Enterprise

# Use the provided script
./scripts/create-issues-from-stories.sh
```

### Option 3: Using GitHub API

See the Python script provided in `scripts/create-issues-from-stories.py`

## Mapping User Story Markdown to Issue Template

Here's how to map each section from the markdown files to the issue template:

| Markdown Section | Issue Template Field | Notes |
|-----------------|---------------------|--------|
| Title (e.g., "US-001: Core Package Setup") | **Story ID** | Extract US-XXX |
| **Category** metadata | **Category** dropdown | Core Package, Extension Package, etc. |
| **Effort** metadata | **Story Points** dropdown | Select from 1, 2, 3, 5, 8, 13, 21 |
| **Sprint** metadata | **Sprint** field | Enter sprint identifier |
| ## Description section | **Description** text area | Copy the "As a... I want... So that..." |
| ## Acceptance Criteria | **Acceptance Criteria** text area | Copy entire section with checkboxes |
| ## Technical Requirements | **Technical Requirements** text area | Copy with code blocks |
| ## Testing Requirements | **Testing Requirements** text area | Copy testing section |
| ## Dependencies | **Dependencies** text area | Convert links to issue references |
| ## Definition of Done | **Definition of Done** text area | Copy checklist |
| ## Notes section | **Additional Notes** text area | Copy design decisions, tips, etc. |
| ## Related Documentation | **Related Documentation** text area | Copy links |

## Example Conversion: US-001

### From Markdown File (`US-001-core-package-setup.md`)

```markdown
# US-001: Core Package Setup and Dependencies

**Status**: ❌ Not Started  
**Category**: Core Package  
**Effort**: 3 story points  
**Sprint**: 1

## Description

As a **library developer**,  
I want to **set up the core HVO.Enterprise.Telemetry package...**,  
So that **I have a solid foundation...**
```

### To GitHub Issue

1. **Story ID**: `US-001`
2. **Category**: Core Package
3. **Story Points**: 3
4. **Sprint**: Sprint 1
5. **Description**: Copy the entire "As a... I want... So that..." paragraph
6. Continue with remaining sections...

## Additional Labels to Apply

After creating the issue, manually add these labels:

### For US-001 (example):
- `user-story` (auto-added by template)
- `core-package`
- `sp-3`
- `sprint-1`
- `priority-critical` (if applicable)
- `ready` (when ready to start)

## Batch Conversion Script

### Using Bash Script

Create and run `scripts/create-issues-from-stories.sh`:

```bash
#!/bin/bash

# Create issues from user story markdown files
# Requires: gh CLI tool installed and authenticated

STORIES_DIR="docs/user-stories"

# Array of story files to convert
STORIES=(
    "US-001-core-package-setup.md"
    "US-002-auto-managed-correlation.md"
    "US-003-background-job-correlation.md"
    "US-004-bounded-queue-worker.md"
    "US-019-common-library.md"
)

for story in "${STORIES[@]}"; do
    echo "Processing $story..."
    
    # Extract story metadata and create issue
    # (This is a template - you'll need to parse the markdown)
    
    # For now, we'll open the issue creation page
    echo "Please create issue for $story manually using the template"
    echo "URL: https://github.com/RoySalisbury/HVO.Enterprise.Telemetry/issues/new?template=user-story.yml"
    echo ""
done
```

### Using Python Script

Create `scripts/create-issues-from-stories.py`:

```python
#!/usr/bin/env python3
"""
Convert user story markdown files to GitHub issues.

Requires:
- PyGithub library: pip install PyGithub
- GitHub Personal Access Token with 'repo' scope
"""

import os
import re
from pathlib import Path
from github import Github

# Configuration
GITHUB_TOKEN = os.environ.get('GITHUB_TOKEN')
REPO_NAME = "RoySalisbury/HVO.Enterprise.Telemetry"
STORIES_DIR = Path("docs/user-stories")

def parse_user_story(markdown_path):
    """Parse a user story markdown file and extract structured data."""
    with open(markdown_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Extract metadata
    story_id = re.search(r'^# (US-\d+):', content, re.MULTILINE)
    category = re.search(r'\*\*Category\*\*: (.+)$', content, re.MULTILINE)
    effort = re.search(r'\*\*Effort\*\*: (\d+) story point', content, re.MULTILINE)
    sprint = re.search(r'\*\*Sprint\*\*: (.+)$', content, re.MULTILINE)
    
    # Extract sections
    description_match = re.search(
        r'## Description\n\n(.+?)(?=\n## )', 
        content, 
        re.DOTALL
    )
    acceptance_match = re.search(
        r'## Acceptance Criteria\n\n(.+?)(?=\n## )',
        content,
        re.DOTALL
    )
    
    return {
        'story_id': story_id.group(1) if story_id else 'US-XXX',
        'category': category.group(1) if category else 'Core Package',
        'effort': effort.group(1) if effort else '3',
        'sprint': sprint.group(1) if sprint else '1',
        'description': description_match.group(1).strip() if description_match else '',
        'acceptance_criteria': acceptance_match.group(1).strip() if acceptance_match else '',
        # Add more fields as needed
    }

def create_github_issue(repo, story_data):
    """Create a GitHub issue from parsed story data."""
    title = f"[USER STORY] {story_data['story_id']}: {story_data.get('title', '')}"
    
    body = f"""
**Story ID**: {story_data['story_id']}
**Category**: {story_data['category']}
**Story Points**: {story_data['effort']}
**Sprint**: {story_data['sprint']}

## Description

{story_data['description']}

## Acceptance Criteria

{story_data['acceptance_criteria']}

<!-- Add remaining sections here -->
"""
    
    labels = [
        'user-story',
        f"sp-{story_data['effort']}",
        f"sprint-{story_data['sprint']}",
    ]
    
    # Add category label
    category_lower = story_data['category'].lower().replace(' ', '-')
    labels.append(category_lower)
    
    issue = repo.create_issue(
        title=title,
        body=body,
        labels=labels
    )
    
    print(f"Created issue #{issue.number}: {title}")
    return issue

def main():
    """Main function to convert all user stories."""
    if not GITHUB_TOKEN:
        print("Error: GITHUB_TOKEN environment variable not set")
        return
    
    g = Github(GITHUB_TOKEN)
    repo = g.get_repo(REPO_NAME)
    
    # Get all user story markdown files
    story_files = sorted(STORIES_DIR.glob("US-*.md"))
    
    for story_file in story_files:
        print(f"Processing {story_file.name}...")
        story_data = parse_user_story(story_file)
        
        # Uncomment to actually create issues
        # create_github_issue(repo, story_data)
        
        print(f"  Story ID: {story_data['story_id']}")
        print(f"  Category: {story_data['category']}")
        print(f"  Effort: {story_data['effort']} SP")
        print()

if __name__ == '__main__':
    main()
```

## Manual Creation Workflow

For the most control and accuracy:

1. **Open the markdown file** for the user story
2. **Open GitHub Issues** in another window
3. **Click "New Issue"** → Select "User Story" template
4. **Copy-paste sections** from markdown to the form fields
5. **Apply labels** as appropriate
6. **Create the issue**
7. **Link dependencies** by editing the issue to add issue number references

## Recommended Order for Creating Issues

Create issues in dependency order:

### Sprint 1 (Foundation)
1. US-001 - Core Package Setup
2. US-019 - HVO.Common Library (parallel with US-001)
3. US-002 - Auto-Managed Correlation
4. US-005 - Lifecycle Management

### Sprint 2 (Background Processing)
5. US-004 - Bounded Queue Worker
6. US-006 - Runtime Adaptive Metrics
7. US-009 - Multi-Level Configuration

... continue in the order specified in `SUMMARY.md`

## Tracking Progress

Once issues are created:

1. **Use GitHub Projects** to create a kanban board
2. **Link related issues** using "blocked by" and "blocks" comments
3. **Track via milestones** for each sprint
4. **Update checklist items** in the issue as work progresses
5. **Close issues** when Definition of Done is complete

## Tips

- **Don't rush**: Take time to ensure each issue is complete
- **Review before creating**: Double-check labels and fields
- **Link dependencies**: Use issue numbers like #1, #2 in the Dependencies field
- **Update as needed**: Issues can be edited after creation
- **Use templates**: Always use the User Story template for consistency

## Automation Ideas

Consider automating:
- Label application based on metadata
- Issue linking based on dependency declarations
- Status updates when PRs are opened/merged
- Notification on critical/blocked issues

## Getting Help

- **Template issues**: See `.github/ISSUE_TEMPLATE/user-story.yml`
- **Labels guide**: See `.github/LABELS.md`
- **Project plan**: See `docs/project-plan.md`
- **Story structure**: See `docs/user-stories/CREATION-GUIDE.md`
