# Output Behavior

## Incremental Generation (Default)

By default (`CratisProxiesSkipOutputDeletion=true`), the proxy generator uses incremental generation:

- Only files whose content has changed are written to disk — unchanged file timestamps are preserved.
- [File index tracking](../file-index-tracking.md) automatically removes orphaned files from renamed or deleted commands and queries.
- Build times are faster because only changed files are regenerated.
- Proxy files committed to the repository keep their original timestamps when another developer builds.

This default suits virtually all projects.

## Full Regeneration

To delete the entire output directory on every build instead of updating incrementally:

```xml
<PropertyGroup>
    <CratisProxiesSkipOutputDeletion>false</CratisProxiesSkipOutputDeletion>
</PropertyGroup>
```

When full regeneration is enabled:

- All proxies are recreated on every build.
- Any manual files in the output directory are deleted.
- Build times may be longer.
- Committed proxy files will always appear modified after a build.

**Recommendation:** Keep the default (`true`) for incremental generation. Set to `false` only if you specifically need a guaranteed clean-state output.

## Index File Generation

An `index.ts` barrel file is generated for each output folder by default. To disable:

```xml
<PropertyGroup>
    <CratisProxiesSkipIndexGeneration>true</CratisProxiesSkipIndexGeneration>
</PropertyGroup>
```

## File Index Tracking

File index tracking records which files the generator owns so that orphaned files (from renamed or deleted commands/queries) are cleaned up automatically. To disable:

```xml
<PropertyGroup>
    <CratisProxiesSkipFileIndexTracking>true</CratisProxiesSkipFileIndexTracking>
</PropertyGroup>
```

See [File Index Tracking](../file-index-tracking.md) for details.

## CLI

```bash
proxygenerator assembly.dll output-path \
  --skip-output-deletion \
  --skip-index-generation \
  --skip-file-index-tracking
```
