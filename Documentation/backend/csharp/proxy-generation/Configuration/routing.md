---
title: Proxy route configuration in Cratis Arc
description: Cratis Arc proxy route settings control conventional model-bound URLs; align API prefixes, namespace segments, and name inclusion with the server.
---

## Which routes these settings affect

The settings below control **conventional model-bound** routes. Controller-based proxies derive routes from ASP.NET controller/action attributes. An explicit `[Path]` on a model-bound command, query method or read-model type is used as the route instead; the query method's attribute wins over the read-model type's attribute. The generator does not prepend the configured API prefix to that explicit path. Import the shared `[Path]` from `Cratis.Arc.Queries.ModelBound` for both commands and queries; a command's path is declared on its own type rather than inherited from a base type.

[Namespace roots](namespace-roots.md) control output folders, not route construction.

## Conventional routes

This MSBuild fragment belongs inside your existing project:

```xml
<PropertyGroup>
    <CratisProxiesApiPrefix>api</CratisProxiesApiPrefix>
    <CratisProxiesSegmentsToSkip>1</CratisProxiesSegmentsToSkip>
    <CratisProxiesSkipCommandNameInRoute>false</CratisProxiesSkipCommandNameInRoute>
    <CratisProxiesSkipQueryNameInRoute>false</CratisProxiesSkipQueryNameInRoute>
</PropertyGroup>
```

The generator skips the first namespace segment, converts the remaining segments and artifact name to kebab case, and prefixes `api`. For namespace `MyApp.Orders.Registration`, command `CreateOrder` generates `/api/orders/registration/create-order`. Without the explicit segment count (default `0`), `my-app` also appears in the route.

A query uses its **method name**, not the read-model type name, as the final segment. Defaults include both command and query names.

## Name skipping and conflict fallback

Set `CratisProxiesSkipCommandNameInRoute` or `CratisProxiesSkipQueryNameInRoute` to `true` to omit that final segment. If more than one discovered command shares the namespace after segment skipping, command names are restored. Query conflict detection counts eligible methods across read-model types in that namespace and restores method names when needed.

For `MyApp.Orders.Registration` with one skipped segment and command-name skipping:

| Discovered commands             | Generated routes                                                                 |
| ------------------------------- | -------------------------------------------------------------------------------- |
| Only `CreateOrder`              | `/api/orders/registration`                                                       |
| `CreateOrder` and `UpdateOrder` | `/api/orders/registration/create-order`, `/api/orders/registration/update-order` |

This is a conventional namespace conflict fallback, not a global collision detector for custom paths or controller routes.

## Settings that must match the proxy generator

Build properties do not change the running application's endpoint configuration. Each route setting exists twice: as an MSBuild property that the generator uses to write the client's URLs, and as an `ArcOptions.GeneratedApis` property that the host uses to map endpoints. **The two must match.** Arc does not compare them. If they differ, the generated client calls a URL the host did not map and gets a 404, with no build error, startup warning or log entry that points at the mismatch.

| MSBuild property (generator)          | `ArcOptions.GeneratedApis` property (host), config key `Cratis:Arc:GeneratedApis:...` | Build default | Host default |
| ------------------------------------- | ------------------------------------------------------------------------------------- | ------------- | ------------ |
| `CratisProxiesApiPrefix`              | `RoutePrefix`                                                                         | `api`         | `"api"`      |
| `CratisProxiesSegmentsToSkip`         | `SegmentsToSkipForRoute`                                                              | empty (none)  | `0`          |
| `CratisProxiesSkipCommandNameInRoute` | `IncludeCommandNameInRoute` (**inverse**)                                             | `false`       | `true`       |
| `CratisProxiesSkipQueryNameInRoute`   | `IncludeQueryNameInRoute` (**inverse**)                                               | `false`       | `true`       |

The skip properties and the include properties have opposite senses. Setting `CratisProxiesSkipCommandNameInRoute` to `true` matches `IncludeCommandNameInRoute = false`.

When you change one side, change the other in the same commit. Then inspect a generated client's `route` and compare it to the hosted endpoint.

See [model-bound commands](../../commands/model-bound/index.md) and [model-bound queries](../../queries/model-bound/index.md).

## CLI

With the [executable alias prerequisite](output-behavior.md#direct-executable), this invocation skips one namespace segment and requests namespace-only routes:

```bash
proxygenerator assembly.dll output-path 1 --skip-output-deletion --api-prefix=v1 --skip-command-name-in-route --skip-query-name-in-route
```

The corresponding runtime prefix must be `v1`; conflict fallback still applies.
