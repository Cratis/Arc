# Routing

The proxy generator builds API route strings for commands and queries from the C# namespace hierarchy. These options let you adjust the resulting routes.

## API Prefix

```xml
<PropertyGroup>
    <CratisProxiesApiPrefix>api</CratisProxiesApiPrefix>
</PropertyGroup>
```

The `CratisProxiesApiPrefix` value is prepended to every generated route. Default is `api`.

**Example:** Namespace `MyApp.Orders.Registration` with prefix `api` → `/api/orders/registration`.

## Excluding Type Names from Routes

By default, the command or query type name is appended to the route:

- Command `RegisterAuthor` → `/api/authors/registration/register-author`
- Query `GetActiveAuthors` → `/api/authors/listing/get-active-authors`

Setting either flag to `true` removes the type name from the route:

```xml
<PropertyGroup>
    <CratisProxiesSkipCommandNameInRoute>true</CratisProxiesSkipCommandNameInRoute>
    <CratisProxiesSkipQueryNameInRoute>true</CratisProxiesSkipQueryNameInRoute>
</PropertyGroup>
```

### Automatic Conflict Detection

When the type name is skipped, the generator detects route conflicts automatically. If multiple commands or queries share the same namespace, their type names are re-added to prevent collisions. This matches the runtime endpoint mapping behavior exactly.

**Example with a single command in `MyApp.Orders.Commands`:**

```text
CreateOrderCommand → /api/orders/commands
```

**Example with multiple commands in `MyApp.Orders.Commands`:**

```text
CreateOrderCommand → /api/orders/commands/create-order-command
UpdateOrderCommand → /api/orders/commands/update-order-command
DeleteOrderCommand → /api/orders/commands/delete-order-command
```

## CLI

```bash
proxygenerator assembly.dll output-path \
  --api-prefix=v1 \
  --skip-command-name-in-route \
  --skip-query-name-in-route
```
