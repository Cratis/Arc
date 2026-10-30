---
title: Endpoint groups
description: Serve internal commands and queries only on a local port of their own, separate from the public API.
---

**Goal:** some commands and queries are for internal callers only, such as an operations tool or another service inside the cluster. You want them on a port the public never reaches, while the rest of the API stays where it is.

An endpoint group does that. Tag the commands, read models or individual queries that belong to a group, give the group a local port, and Arc serves them on that port only.

> [!WARNING]
> The port is the boundary only if the network makes it one. Never expose or forward a group port to the public network, and never let a reverse proxy route public traffic to it. Arc decides by the local port of the connection, which is only as trustworthy as the network in front of it.

## Tag commands and read models

Put `[EndpointGroup]` on a command, or on a read model to place all of its queries in the group:

```csharp
using Cratis.Arc;
using Cratis.Arc.Commands.ModelBound;

[Command]
[EndpointGroup("internal")]
public record RebuildSearchIndex(IndexName Index)
{
    public IndexRebuildStarted Handle() => new(Index);
}
```

To place every command and read model in an assembly in one group, put the attribute on the assembly:

```csharp
[assembly: Cratis.Arc.EndpointGroup("internal")]
```

An attribute on a type wins over one on its assembly. Commands and read models without the attribute are in the default group and are served exactly as before.

## Put one query in a group

Put `[EndpointGroup]` on a query method when the other queries on the read model should stay public. This illustrative read model uses fixed values to show the boundary:

```csharp
using Cratis.Arc;
using Cratis.Arc.Queries.ModelBound;

[ReadModel]
public record InventorySummary(int InStock)
{
    public static InventorySummary Current() => new(42);

    [EndpointGroup("internal")]
    public static InventorySummary Reserved() => new(7);
}
```

`Current` stays in the default group; `Reserved` belongs to `internal`. A method's attribute wins over its type, which wins over its assembly. You cannot use a method attribute to move a query back to the default group: group names must be non-empty.

Method-level grouping applies only to query methods and MVC controller actions. An attribute on another method has no effect.

## Assign a port to the group

Listen on a separate port for the group, and assign it in the Arc options:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:5000", "http://*:5001");

builder.AddCratisArc(options => options.EndpointGroups.Assign("internal", 5001));

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

The same assignment can come from configuration under `Cratis:Arc`:

```json
{
    "Cratis": {
        "Arc": {
            "EndpointGroups": {
                "Ports": {
                    "internal": [5001]
                }
            }
        }
    }
}
```

## Give a group its own route prefix

By default, grouped commands and queries use the global generated API prefix. Set a group's prefix when internal callers need a separate route layout. In the host setup above, replace the `AddCratisArc` call with:

```csharp
builder.AddCratisArc(options => options.EndpointGroups
    .Assign("internal", 5001)
    .SetRoutePrefix("internal", "internal-api"));
```

The same settings can come from configuration:

```json
{
    "Cratis": {
        "Arc": {
            "EndpointGroups": {
                "Ports": {
                    "internal": [5001]
                },
                "RoutePrefixes": {
                    "internal": "internal-api"
                }
            }
        }
    }
}
```

The override applies only to Arc-generated command and query routes. It does not change custom `[Route]` templates, controller routes or the observable-query hub routes under `/.cratis/queries`. Groups without an override, and the default group, keep the global prefix. The prefix does not replace the port boundary.

When generating proxies for this group, set `CratisProxiesApiPrefix` to the same prefix (or pass `--api-prefix=internal-api` to the generator).

## What each port serves

| Request arrives on | Default group | A named group |
| --- | --- | --- |
| A port not assigned to any group | Served | Not found (404) |
| A port assigned to the group | Not found (404), unless `ServeDefaultGroupOnGroupPorts` is set | Served |

The decision is made while routing chooses an endpoint, from the local port of the connection. The `Host` header plays no part, so a request on the public port cannot reach a grouped endpoint by naming the internal host or port.

The default group is not served on group ports. Set `options.EndpointGroups.ServeDefaultGroupOnGroupPorts = true` if internal callers should also reach the default endpoints on the group port.

MVC controllers follow the same rule: put `[EndpointGroup]` on the controller class or its assembly, and every action of the controller is served only on the ports of that group. An attribute on an individual action overrides the controller's group without changing its siblings. A grouped controller or action without an assigned port now stops startup instead of being served on no port; assign a port before starting the host.

Endpoints you map yourself, such as minimal API endpoints, are not affected by endpoint groups.

## Observable queries

The observable-query hub routes (WebSocket and server-sent events under `/.cratis/queries`) are reachable on every port. A subscription resolves only the queries whose group is served on the port the request arrived on, so a client on the public port cannot subscribe to a grouped query.

A server-sent events connection is bound to the groups of the port it was opened on. Subscribe and unsubscribe requests for that connection must arrive on a port serving the same groups; from any other port they are answered as though the connection did not exist.

## Startup checks

Arc refuses to start rather than expose a group by mistake:

- A command, read model query, controller or controller action is in a group that has no port assigned.
- Every port the server listens on is assigned to a group, which would make a group port the public port.
- The application runs on the Arc.Core HTTP listener host, which cannot restrict an endpoint to a port. Endpoint groups need the ASP.NET Core host.

## Discovery and proxies

Grouped commands, queries and controllers are left out of [OpenAPI](open-api/index.md) and [introspection](introspection/index.md).

The proxy generator emits the default group only. Model-bound query proxies follow each query method's group, so selecting a group does not pull in its public siblings. To generate the proxies of a named group instead, set the `CratisProxiesEndpointGroup` MSBuild property in the project that generates them. Include `CratisProxiesApiPrefix` when you configured a group prefix:

```xml
<PropertyGroup>
    <CratisProxiesEndpointGroup>internal</CratisProxiesEndpointGroup>
    <CratisProxiesApiPrefix>internal-api</CratisProxiesApiPrefix>
</PropertyGroup>
```

When you run the generator yourself, pass `--endpoint-group=<name>` instead.
