#!/bin/bash
#
# Create GitHub issues directly from user story markdown files
#
# Usage:
#   ./scripts/create-github-issues.sh                # Dry run (default)
#   ./scripts/create-github-issues.sh --no-dry-run   # Create issues
#   ./scripts/create-github-issues.sh --repo owner/repo
#
# Requirements:
#   - GitHub CLI (`gh`) must be installed and authenticated
#   - `jq` must be installed (used for label processing)
#

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
USER_STORIES_DIR="$REPO_ROOT/docs/user-stories"

REPO="RoySalisbury/HVO.Enterprise"
DRY_RUN=1

COMPLETED_STORIES=(
    "US-001"
    "US-002"
    "US-003"
    "US-004"
    "US-019"
)

usage() {
    cat <<'EOF'
Create GitHub issues from user story markdown files.

Options:
  --no-dry-run        Actually create issues (default is dry run)
  --dry-run           Force dry run
  --repo owner/name   Override the default repository
  -h, --help          Show this help message
EOF
}

contains() {
    local needle="$1"
    shift
    for value in "$@"; do
        if [[ "$value" == "$needle" ]]; then
            return 0
        fi
    done
    return 1
}

extract_section() {
    local file="$1"
    local section="$2"
    awk -v section="$section" '
        BEGIN { in_section=0; line_count=0 }
        /^##[[:space:]]+/ {
            if (in_section) {
                exit
            }
            heading=$0
            sub(/^##[[:space:]]+/, "", heading)
            if (heading == section) {
                in_section=1
                next
            }
        }
        {
            if (in_section) {
                lines[line_count++] = $0
            }
        }
        END {
            start=0
            end=line_count
            while (start < end && lines[start] ~ /^[[:space:]]*$/) {
                start++
            }
            while (end > start && lines[end-1] ~ /^[[:space:]]*$/) {
                end--
            }
            for (i=start; i<end; i++) {
                print lines[i]
            }
        }
    ' "$file"
}

parse_args() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --no-dry-run)
                DRY_RUN=0
                shift
                ;;
            --dry-run)
                DRY_RUN=1
                shift
                ;;
            --repo)
                if [[ $# -lt 2 ]]; then
                    echo "ERROR: --repo requires a value" >&2
                    exit 1
                fi
                REPO="$2"
                shift 2
                ;;
            --repo=*)
                REPO="${1#*=}"
                shift
                ;;
            -h|--help)
                usage
                exit 0
                ;;
            *)
                echo "Unknown option: $1" >&2
                usage >&2
                exit 1
                ;;
        esac
    done
}

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "ERROR: Required command '$1' not found" >&2
        exit 1
    fi
}

build_issue_body() {
    local file="$1"
    local tmp_file="$2"
    : >"$tmp_file"

    append_section() {
        local heading="$1"
        local content="$2"
        if [[ -n "$content" ]]; then
            printf '## %s\n\n%s\n\n' "$heading" "$content" >>"$tmp_file"
        fi
    }

    append_section "Description" "$(extract_section "$file" "Description")"
    append_section "Acceptance Criteria" "$(extract_section "$file" "Acceptance Criteria")"
    append_section "Technical Requirements" "$(extract_section "$file" "Technical Requirements")"
    append_section "Testing Requirements" "$(extract_section "$file" "Testing Requirements")"
    append_section "Performance Requirements" "$(extract_section "$file" "Performance Requirements")"
    append_section "Dependencies" "$(extract_section "$file" "Dependencies")"
    append_section "Definition of Done" "$(extract_section "$file" "Definition of Done")"
    append_section "Notes" "$(extract_section "$file" "Notes")"
    append_section "Related Documentation" "$(extract_section "$file" "Related Documentation")"
}

get_field() {
    local file="$1"
    local label="$2"
    local line
    line=$(grep -m1 "^\\*\\*$label\\*\\*:" "$file" || true)
    if [[ -n "$line" ]]; then
        echo "$line" | sed "s/^\\*\\*$label\\*\\*: *//"
    fi
}

main() {
    parse_args "$@"

    require_command gh
    require_command jq

    if [[ ! -d "$USER_STORIES_DIR" ]]; then
        echo "ERROR: User stories directory not found at $USER_STORIES_DIR" >&2
        exit 1
    fi

    if (( DRY_RUN == 0 )) && ! gh auth status >/dev/null 2>&1; then
        echo "ERROR: GitHub CLI is not authenticated. Run 'gh auth login'." >&2
        exit 1
    fi

    shopt -s nullglob
    story_files=("$USER_STORIES_DIR"/US-*.md)
    shopt -u nullglob

    if [[ ${#story_files[@]} -eq 0 ]]; then
        echo "No user story files found." >&2
        exit 0
    fi

    total=0
    created=0

    for story_file in "${story_files[@]}"; do
        total=$((total + 1))
        filename=$(basename "$story_file")
        story_id="$(echo "$filename" | cut -d'-' -f1-2)"
        title=$(sed -n '1s/^# US-[0-9]*: //p' "$story_file" | head -n1)
        if [[ -z "$title" ]]; then
            title="Untitled Story"
        fi

        category=$(get_field "$story_file" "Category")
        effort_line=$(get_field "$story_file" "Effort")
        sprint_line=$(get_field "$story_file" "Sprint")

        effort=""
        if [[ -n "$effort_line" ]]; then
            effort=$(echo "$effort_line" | grep -oE '[0-9]+' | head -n1 || true)
        fi

        sprint=""
        if [[ -n "$sprint_line" ]]; then
            sprint=$(echo "$sprint_line" | grep -oE '[0-9]+' | head -n1 || true)
        fi

        labels=("user-story")
        if contains "$story_id" "${COMPLETED_STORIES[@]}"; then
            labels+=("status:complete")
        else
            labels+=("status:not-started")
        fi

        if [[ -n "$effort" ]]; then
            labels+=("sp-$effort")
        fi
        if [[ -n "$sprint" ]]; then
            labels+=("sprint-$sprint")
        fi

        if [[ "$category" == *"Core Package"* ]]; then
            labels+=("core-package")
        elif [[ "$category" == *"Extension Package"* ]]; then
            labels+=("extension-package")
        elif [[ "$category" == *"Testing"* ]] || [[ "$category" == *"Samples"* ]]; then
            labels+=("testing")
        elif [[ "$category" == *"Documentation"* ]]; then
            labels+=("documentation")
        fi

        if [[ -n "$sprint" ]]; then
            if (( sprint <= 2 )); then
                labels+=("priority:p0")
            elif (( sprint <= 4 )); then
                labels+=("priority:p1")
            elif (( sprint <= 8 )); then
                labels+=("priority:p2")
            else
                labels+=("priority:p3")
            fi
        fi

        labels_arg=$(printf '%s\n' "${labels[@]}" | jq -R -s -r 'split("\n")[:-1] | map(select(length>0)) | join(",")')

        body_file=$(mktemp)
        build_issue_body "$story_file" "$body_file"

        echo "--------------------------------------------------------------------------------"
        echo "Story: $story_id"
        echo "Title: [USER STORY]: $story_id - $title"
        echo "Labels: $labels_arg"
        echo "Body file: $body_file"

        if (( DRY_RUN == 1 )); then
            echo "DRY RUN - issue not created"
        else
            if gh issue create \
                --repo "$REPO" \
                --title "[USER STORY]: $story_id - $title" \
                --label "$labels_arg" \
                --body-file "$body_file"; then
                created=$((created + 1))
            else
                echo "Failed to create issue for $story_id" >&2
            fi
        fi

        rm -f "$body_file"
        echo
    done

    echo "========================================"
    echo "Stories processed: $total"
    if (( DRY_RUN == 1 )); then
        echo "Mode: DRY RUN"
    else
        echo "Issues created: $created"
    fi
}

main "$@"
