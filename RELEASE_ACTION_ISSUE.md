# Fix: Prevent Duplicate Releases Due to Missing Input Validation

## Problem Summary

The release-action has created 6 duplicate releases for the same commit (571235c) within a 24-hour period (v20.34.0 through v20.39.0). All releases share an identical `created_at` timestamp (2026-06-04T17:09:02Z) but were published at staggered intervals. This indicates the action lacks safeguards against:

1. **Missing input values** — the action creates a release even when inputs (version, release-notes) are empty/null
2. **Duplicate releases** — no check exists to prevent creating a new release for a commit that already has one
3. **Invalid version input** — no validation of semantic version format

## Root Cause Analysis

### Trigger Chain
The `publish.yml` workflow in Arc repository triggers on both:
- `workflow_dispatch` — manual trigger with explicit `version` and `release-notes` inputs ✓
- `pull_request: types: [closed]` — automatic trigger that has **no inputs** ✗

When PRs close, the workflow runs without inputs, but `cratis/release-action` still creates releases with:
- Empty/null `version` → likely defaults to auto-increment
- Empty/null `release-notes` → likely defaults to empty

### Why This is a Problem
Each closed PR triggers the release action with the same commit SHA but no explicit version, causing sequential auto-incremented versions (20.34.0 → 20.35.0 → 20.36.0, etc.) for the **same commit**.

## Required Fixes

All fixes should be **defensive** (prevent invalid releases rather than create them).

### 1. Input Validation

**File:** `Source/HandleRelease.ts`

Add validation at the start of the handler:

```typescript
// At the beginning of the main release logic:

// Validate required inputs are provided
if (!version || version.trim() === '') {
    console.warn('⚠️  No version input provided. Skipping release creation.');
    return {
        published: false,
        shouldPublish: false,
        message: 'Skipped: missing version input'
    };
}

if (!releaseNotes || releaseNotes.trim() === '') {
    console.warn('⚠️  No release notes provided. Skipping release creation.');
    return {
        published: false,
        shouldPublish: false,
        message: 'Skipped: missing release notes input'
    };
}

// Validate semantic version format (basic validation)
const semverRegex = /^\d+\.\d+\.\d+(-[a-zA-Z0-9.]+)?(\+[a-zA-Z0-9.]+)?$/;
if (!semverRegex.test(version)) {
    console.error(`❌ Invalid semantic version format: "${version}". Expected format: X.Y.Z`);
    throw new Error(`Invalid version format: ${version}`);
}

console.log(`✓ Input validation passed. Version: ${version}`);
```

### 2. Duplicate Release Detection

**File:** `Source/Tags.ts`

Add methods to check if a release already exists for the SHA:

```typescript
/**
 * Checks if a release already exists for the given commit SHA
 * @param sha The commit SHA to check
 * @returns true if a release exists for this SHA, false otherwise
 */
async releaseExistsForSha(sha: string): Promise<boolean> {
    try {
        const releases = await this.getReleases();
        for (const release of releases) {
            if (release.target_commitish === sha) {
                console.log(`ℹ️  Release already exists for SHA ${sha}: ${release.tag_name}`);
                return true;
            }
        }
        return false;
    } catch (error) {
        console.error(`Error checking for existing releases: ${error}`);
        throw error;
    }
}

/**
 * Gets all releases for the repository
 * @returns Array of release objects
 */
private async getReleases() {
    const octokit = github.getOctokit(this.token);
    const { data } = await octokit.rest.repos.listReleases({
        owner: this.owner,
        repo: this.repo,
        per_page: 100 // Adjust if needed for repos with >100 releases
    });
    return data;
}
```

**File:** `Source/ITags.ts`

Update the interface:

```typescript
interface ITags {
    tagExists(tag: string): Promise<boolean>;
    releaseExistsForSha(sha: string): Promise<boolean>;
    // ... other methods
}
```

### 3. Duplicate Check in HandleRelease

**File:** `Source/HandleRelease.ts`

Before creating the release, check if one already exists:

```typescript
// After input validation, before createRelease():

const tags = new Tags(token, owner, repo);

// Check if a release already exists for this commit
if (await tags.releaseExistsForSha(process.env.GITHUB_SHA || '')) {
    console.warn(`⚠️  Release already exists for commit ${process.env.GITHUB_SHA}. Skipping duplicate.`);
    return {
        published: false,
        shouldPublish: false,
        message: 'Skipped: release already exists for this commit'
    };
}

// Proceed with release creation
const releaseResult = await tags.createRelease(version, releaseNotes);
```

## Implementation Details

### File Structure
```
Source/
  ├── HandleRelease.ts      (Add input validation + duplicate check call)
  ├── Tags.ts               (Add releaseExistsForSha() + getReleases())
  └── ITags.ts              (Update interface with new methods)
dist/
  └── index.js              (Recompile with `ncc build`)
```

### Compilation
After making changes, recompile the action bundle:

```bash
npm install  # If needed
yarn run ncc build Source/HandleRelease.ts -o dist/index.js
```

### Testing Checklist

1. **Unit Test: Input Validation**
   - Call with empty `version` → should return `{ published: false, shouldPublish: false }`
   - Call with empty `releaseNotes` → should return `{ published: false, shouldPublish: false }`
   - Call with invalid version format (e.g., "20.34.0.extra") → should throw error
   - Call with valid version → should proceed

2. **Unit Test: Duplicate Detection**
   - Mock `Tags.getReleases()` to return a release for SHA `abc123` 
   - Call with SHA `abc123` → should return `{ published: false, shouldPublish: false }`
   - Call with SHA `xyz789` (no existing release) → should proceed

3. **Integration Test**
   - Trigger publish workflow with valid inputs → should create release ✓
   - Trigger publish workflow again with same commit → should skip release (not create duplicate) ✓
   - Trigger publish workflow with empty inputs → should skip release ✓

## Expected Behavior After Fix

| Scenario | Before | After |
|----------|--------|-------|
| workflow_dispatch with version + notes | Creates release ✓ | Creates release ✓ |
| workflow_dispatch without inputs | Creates release (🐛 bug) | Skips release ✓ |
| pull_request closed (no inputs) | Creates release (🐛 bug) | Skips release ✓ |
| Same commit, second trigger | Creates duplicate (🐛 bug) | Skips duplicate ✓ |
| Invalid version format | Creates release | Throws error ✓ |

## Workflow Configuration Recommendation

For additional safety, Arc's `publish.yml` should be updated to only run on `workflow_dispatch`:

```yaml
on:
  workflow_dispatch:
    inputs:
      version:
        description: 'Version to release (e.g., 20.34.0)'
        required: true
        type: string
      release-notes:
        description: 'Release notes content'
        required: true
        type: string
```

Alternatively, add explicit guards:

```yaml
if: github.event_name == 'workflow_dispatch' || (github.event_name == 'pull_request' && github.event.pull_request.merged == true)
```

However, the action-level safeguards in this issue should be implemented regardless to prevent issues in other repositories using the action.

## Impact

- **Severity:** High — causes duplicate releases in production
- **Scope:** Affects all repositories using `cratis/release-action`
- **Backward Compatibility:** ✓ Fully backward-compatible (only prevents invalid releases)
- **Testing Impact:** No test changes needed (defensive logic only)
