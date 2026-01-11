# Configuration

Configure the proxy generator by adding MSBuild properties to your `.csproj` file.

## Required Configuration

```xml
<PropertyGroup>
    <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../Web</CratisProxiesOutputPath>
</PropertyGroup>
```

- `CratisProxiesOutputPath`: Specifies where the generated TypeScript files will be written. This should typically point to your frontend project directory.

## Optional Configuration

```xml
<PropertyGroup>
    <CratisProxiesSegmentsToSkip>1</CratisProxiesSegmentsToSkip>
    <CratisProxiesSkipOutputDeletion>false</CratisProxiesSkipOutputDeletion>
    <CratisProxiesSkipCommandNameInRoute>false</CratisProxiesSkipCommandNameInRoute>
    <CratisProxiesSkipQueryNameInRoute>false</CratisProxiesSkipQueryNameInRoute>
    <CratisProxiesApiPrefix>api</CratisProxiesApiPrefix>
    <CratisProxiesSkipFileIndexTracking>false</CratisProxiesSkipFileIndexTracking>
    <CratisProxiesSkipIndexGeneration>false</CratisProxiesSkipIndexGeneration>
</PropertyGroup>
```

## Configuration Options Reference

| Property | Default | Description |
|----------|---------|-------------|
| `CratisProxiesOutputPath` | *(Required)* | The output directory for generated TypeScript files |
| `CratisProxiesSegmentsToSkip` | `0` | Number of namespace segments to skip when creating the folder structure |
| `CratisProxiesSkipOutputDeletion` | `false` | **When `false` (default), the entire output directory is deleted on every build.** Set to `true` to enable incremental generation. See [Output Deletion Behavior](#output-deletion-behavior) below. |
| `CratisProxiesSkipCommandNameInRoute` | `false` | When `true`, excludes the command name from the generated route for command endpoints |
| `CratisProxiesSkipQueryNameInRoute` | `false` | When `true`, excludes the query name from the generated route for query endpoints |
| `CratisProxiesApiPrefix` | `api` | The API prefix used in generated routes |
| `CratisProxiesSkipFileIndexTracking` | `false` | When `true`, disables file index tracking for incremental cleanup |
| `CratisProxiesSkipIndexGeneration` | `false` | When `true`, skips generating `index.ts` files for directories |

## Output Deletion Behavior

**Important**: By default (`CratisProxiesSkipOutputDeletion=false`), the proxy generator **deletes the entire output directory** before generating proxies on every build. This ensures a clean generation but means:

- All proxies are regenerated on every build
- Any manual files in the output directory will be deleted
- Build times may be longer as everything is recreated each time

This default behavior is ideal when:
- Proxies are in a dedicated folder separate from other source files
- You want guaranteed clean state with no stale files
- Your project structure keeps generated code isolated from feature code

**To enable incremental generation**, set:

```xml
<PropertyGroup>
    <CratisProxiesSkipOutputDeletion>true</CratisProxiesSkipOutputDeletion>
</PropertyGroup>
```

When output deletion is skipped:
- The generator only updates changed proxies
- [File index tracking](file-index-tracking.md) automatically removes orphaned files from renamed/deleted commands or queries
- Build times are faster as only necessary files are regenerated
- You can safely mix generated proxies with other code in the same directory

**Recommendation**: Use `CratisProxiesSkipOutputDeletion=true` when your proxies are intertwined with feature code in your frontend. Use the default `false` when proxies are in a dedicated output folder.

## Namespace Segment Skipping

The `CratisProxiesSegmentsToSkip` property is particularly useful in multi-project solutions with consistent naming conventions.

For example, if you have a folder structure like:

```shell
<Your Root Folder>
|
├── Api
│   └── MyFeature
├── Domain
│   └── MyFeature
├── Events
│   └── MyFeature
└── Read
    └── MyFeature
```

And corresponding namespaces like `Api.MyFeature`, `Domain.MyFeature`, `Read.MyFeature`, you might want to skip the first segment (`Api`, `Domain`, `Read`) to create a unified structure in your frontend. Setting `CratisProxiesSegmentsToSkip` to `1` would generate:

```shell
MyFeature/
├── commands/
└── queries/
```

Instead of:

```shell
Domain/
└── MyFeature/
    └── commands/
Read/
└── MyFeature/
    └── queries/
```

## Route Name Configuration

The `CratisProxiesSkipCommandNameInRoute` and `CratisProxiesSkipQueryNameInRoute` properties allow you to control whether the command or query type names are included in the generated routes.

By default, both properties are `false`, meaning type names are included in routes:

- Command `CreateOrderCommand` → `/api/orders/create-order-command`
- Query `GetOrdersQuery` → `/api/orders/get-orders-query`

When set to `true`, the type names are excluded:

- Command `CreateOrderCommand` → `/api/orders`
- Query `GetOrdersQuery` → `/api/orders`

### Automatic Conflict Detection

When `CratisProxiesSkipCommandNameInRoute` or `CratisProxiesSkipQueryNameInRoute` is set to `true`, the proxy generator automatically detects route conflicts. If multiple commands or queries exist in the same namespace (after skipping segments), the type/method name is automatically included in the route to prevent collisions.

This conflict detection ensures that:

- Routes remain clean when there's only one command/query in a namespace
- Route conflicts are automatically resolved by including type names when needed
- The generated proxy routes match the runtime endpoint mapping behavior exactly

**Examples:**

Single command in namespace `MyApp.Orders.Commands`:

```text
CreateOrderCommand → /api/orders
```

Multiple commands in namespace `MyApp.Orders.Commands`:

```text
CreateOrderCommand → /api/orders/create-order-command
UpdateOrderCommand → /api/orders/update-order-command
DeleteOrderCommand → /api/orders/delete-order-command
```

**Example Configuration:**

```xml
<PropertyGroup>
    <!-- Include command names but exclude query names from routes -->
    <CratisProxiesSkipCommandNameInRoute>false</CratisProxiesSkipCommandNameInRoute>
    <CratisProxiesSkipQueryNameInRoute>true</CratisProxiesSkipQueryNameInRoute>
</PropertyGroup>
```

This allows for more granular control over API route generation, which can be particularly useful when you want cleaner URLs for queries while keeping explicit command names.

## Advanced Configuration

For more complex scenarios, you can combine multiple configuration options:

```xml
<PropertyGroup>
    <!-- Output to a specific frontend directory -->
    <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../../Frontend/src/api</CratisProxiesOutputPath>
    
    <!-- Skip Domain/Read prefixes in namespaces -->
    <CratisProxiesSegmentsToSkip>1</CratisProxiesSegmentsToSkip>
    
    <!-- Use a custom API prefix -->
    <CratisProxiesApiPrefix>v1</CratisProxiesApiPrefix>
    
    <!-- Don't delete existing files (useful for debugging) -->
    <CratisProxiesSkipOutputDeletion>true</CratisProxiesSkipOutputDeletion>
</PropertyGroup>
```

This configuration would:

- Generate proxies in the `Frontend/src/api` directory
- Skip the first namespace segment when creating folders
- Use `v1` instead of `api` in routes
- Preserve any existing files in the output directory

## Troubleshooting

### Common Issues

1. **No proxies generated**: Ensure the NuGet package is referenced and the output path is correctly configured
2. **Missing types**: Complex types need to be public and discoverable by the generator
3. **Wrong folder structure**: Adjust `CratisProxiesSegmentsToSkip` to match your desired output structure
4. **Build errors**: Check that the target framework matches between your project and the proxy generator

### Debugging

Enable detailed MSBuild logging to see the proxy generation process:

```bash
dotnet build -v detailed
```

Look for messages starting with "MSBuildProjectDirectory", "OutputPath", and "ProxyGenerator base directory" to verify the configuration is correct.
