# File Index Tracking

The proxy generator maintains a file index to enable intelligent cleanup of stale files when commands or queries are removed from your codebase.

## Overview

When you rename or delete a command or query from your backend, the corresponding TypeScript proxy file would normally be left behind. File index tracking solves this by keeping track of all generated files and automatically removing ones that are no longer needed.

## How it Works

1. **Index Storage**: A `GeneratedFileIndex.json` file is stored in a `.cratis` folder next to your `.csproj` file, containing a hierarchical representation of all generated files.

2. **Incremental Cleanup**: When running proxy generation, the generator compares the current set of files to generate against the previous index. Any files that were previously generated but are no longer needed are automatically deleted.

3. **Index.ts Updates**: When files are removed from a directory, the corresponding `index.ts` file is updated to reflect the remaining exports. If a directory becomes empty after cleanup, both the `index.ts` and the directory itself are removed.

## Configuration

File index tracking is enabled by default. To disable it, add the following to your `.csproj`:

```xml
<PropertyGroup>
    <CratisProxiesSkipFileIndexTracking>true</CratisProxiesSkipFileIndexTracking>
</PropertyGroup>
```

### CLI Usage

When using the proxy generator CLI directly, use the `--skip-file-index-tracking` flag:

```bash
proxygenerator assembly.dll output-path --skip-file-index-tracking
```

You can also specify a custom project directory for the `.cratis` folder:

```bash
proxygenerator assembly.dll output-path --project-directory=/path/to/project
```

## Version Control

### Option 1: Ignore the Index (Recommended)

Add the `.cratis` folder to your `.gitignore` file, as it contains build-time tracking information that can be regenerated:

```gitignore
.cratis/
```

This is the recommended approach because:
- The index is regenerated on each build
- Different developers may have different build outputs
- CI/CD pipelines will generate fresh indexes

### Option 2: Commit the Index

Alternatively, you may choose to commit the `.cratis` folder if you want to:
- Track the generated file history across team members
- Ensure consistent cleanup behavior in all environments
- Have visibility into what files are being generated

## Example Scenario

Consider the following scenario:

1. **Initial State**: You have `CreateOrder.ts` and `UpdateOrder.ts` in your commands folder.

2. **Change**: You rename `UpdateOrder` to `ModifyOrder` in your C# code.

3. **Build**: The proxy generator runs and:
   - Generates `CreateOrder.ts` (unchanged)
   - Generates `ModifyOrder.ts` (new)
   - Detects `UpdateOrder.ts` is no longer in the current generation
   - Deletes `UpdateOrder.ts`
   - Updates `index.ts` to export only `CreateOrder` and `ModifyOrder`

4. **Result**: Your frontend has clean, up-to-date proxies without stale files.

## Behavior When Disabled

When file index tracking is disabled (`CratisProxiesSkipFileIndexTracking=true`):

- No `.cratis` folder or index file is created
- Stale files are not automatically cleaned up
- The `CratisProxiesSkipOutputDeletion` option becomes more important for managing old files
- You may need to manually delete old proxy files when renaming or removing backend code

## Troubleshooting

### Files Not Being Cleaned Up

1. **Check if tracking is enabled**: Ensure `CratisProxiesSkipFileIndexTracking` is not set to `true`.

2. **Verify the index exists**: Check for `.cratis/GeneratedFileIndex.json` next to your `.csproj` file.

3. **Clean and rebuild**: Delete the `.cratis` folder and rebuild to regenerate the index.

### Index File Corruption

If the index file becomes corrupted or causes issues:

1. Delete the `.cratis` folder
2. Optionally delete the proxy output folder
3. Rebuild your project

The generator will create a fresh index on the next build.
