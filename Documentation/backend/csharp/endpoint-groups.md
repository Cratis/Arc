---
title: Endpoint groups
description: Serve internal commands and queries only on a local port of their own, separate from the public API.
---

**Goal:** some commands and queries are for internal callers only, such as an operations tool or another service inside the cluster. You want them on a port the public never reaches, while the rest of the API stays where it is.

An endpoint group does that. Tag the commands and read models that belong to a group, give the group a local port, and Arc serves them on that port only.

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

## What each port serves

| Request arrives on | Default group | A named group |
| --- | --- | --- |
| A port not assigned to any group | Served | Not found (404) |
| A port assigned to the group | Not found (404), unless `ServeDefaultGroupOnGroupPorts` is set | Served |

The decision is made while routing chooses an endpoint, from the local port of the connection. The `Host` header plays no part, so a request on the public port cannot reach a grouped endpoint by naming the internal host or port.

The default group is not served on group ports. Set `options.EndpointGroups.ServeDefaultGroupOnGroupPorts = true` if internal callers should also reach the default endpoints on the group port.

Endpoints Arc does not map, such as your own minimal API endpoints and MVC controllers, are not affected by endpoint groups.

## Observable queries

The observable-query hub routes (WebSocket and server-sent events under `/.cratis/queries`) are reachable on every port. A subscription resolves only the queries whose group is served on the port the request arrived on, so a client on the public port cannot subscribe to a grouped query.

A server-sent events connection is bound to the groups of the port it was opened on. Subscribe and unsubscribe requests for that connection must arrive on a port serving the same groups; from any other port they are answered as though the connection did not exist.

## Startup checks

Arc refuses to start rather than expose a group by mistake:

- A command or read model is in a group that has no port assigned.
- Every port the server listens on is assigned to a group, which would make a group port the public port.
- The application runs on the Arc.Core HTTP listener host, which cannot restrict an endpoint to a port. Endpoint groups need the ASP.NET Core host.

## Discovery and proxies

Grouped commands and queries are left out of [OpenAPI](open-api/index.md) and [introspection](introspection/index.md).

The proxy generator emits the default group only. To generate the proxies of a named group instead, set the `CratisProxiesEndpointGroup` MSBuild property in the project that generates them:

```xml
<PropertyGroup>
    <CratisProxiesEndpointGroup>internal</CratisProxiesEndpointGroup>
</PropertyGroup>
```

When you run the generator yourself, pass `--endpoint-group=<name>` instead.
