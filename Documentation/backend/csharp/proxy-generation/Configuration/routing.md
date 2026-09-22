---
title: Routing
description: Align conventional model-bound proxy routes with Arc runtime endpoint options.
---

## Which routes these settings affect

The settings below control **conventional model-bound** routes. Controller-based proxies derive routes from ASP.NET controller/action attributes. An explicit `[Path]` on a model-bound query method or read-model type is used as the route instead; the method's attribute wins. The generator does not prepend the configured API prefix to that explicit path.

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

## Match the server

Build properties do not change the running application's endpoint configuration. Align them with `ArcOptions.GeneratedApis`:

| Generator setting                     | Runtime property                          | Runtime default |
| ------------------------------------- | ----------------------------------------- | --------------- |
| `CratisProxiesApiPrefix`              | `RoutePrefix`                             | `"api"`         |
| `CratisProxiesSegmentsToSkip`         | `SegmentsToSkipForRoute`                  | `0`             |
| `CratisProxiesSkipCommandNameInRoute` | `IncludeCommandNameInRoute` (**inverse**) | `true`          |
| `CratisProxiesSkipQueryNameInRoute`   | `IncludeQueryNameInRoute` (**inverse**)   | `true`          |

Inspect a generated client's `route` and compare it to the hosted endpoint after configuration changes. See [model-bound commands](../../commands/model-bound/index.md) and [model-bound queries](../../queries/model-bound/index.md).

## CLI

With the [executable alias prerequisite](output-behavior.md#direct-executable), this invocation skips one namespace segment and requests namespace-only routes:

```bash
proxygenerator assembly.dll output-path 1 --skip-output-deletion --api-prefix=v1 --skip-command-name-in-route --skip-query-name-in-route
```

The corresponding runtime prefix must be `v1`; conflict fallback still applies.
