---
title: Map Arc tenants to Cratis Chronicle namespaces
description: Arc's Chronicle integration maps the active tenant to an event-store namespace; tenant authorization and query-storage isolation need separate setup.
---

Cratis Arc's Chronicle integration maps the active tenant to a Cratis Chronicle namespace, a named partition of event-store data. Namespace-aware client operations then use that partition without a tenant argument on each command. You still need to authorize access to the selected tenant and configure isolation for queries against other storage.

Use this mapping when an event-sourced application serves multiple tenants. An unset or default Arc tenant selects Chronicle's default namespace; it does not cause the integration to reject the request.

## What you write

Adding the Chronicle integration registers `TenantNamespaceResolver`. The command can identify its author without carrying a tenant or namespace parameter. This fragment assumes an `AuthorId : EventSourceId<Guid>` domain identity and an `AuthorRegistered` event:

```csharp
[Command]
public record RegisterAuthor(AuthorId Id, AuthorName Name)
{
    public AuthorRegistered Handle() => new(Name);
}
```

The author's id selects the stream within the namespace chosen from Arc's tenant context. The resolver does not add filters to arbitrary query code or independently configured databases. For off-request work, [select the tenant before the pipeline creates its DI scope](../commands/command-pipeline.md#execute-for-a-specific-tenant); Chronicle selects the namespace when its scoped event store is resolved.

## The mapping rule

`TenantNamespaceResolver` implements Chronicle's `IEventStoreNamespaceResolver` and reads Arc's current tenant:

| Arc tenant context | Chronicle namespace |
| --- | --- |
| A tenant is resolved | the tenant id, used verbatim as the namespace name |
| Default or unset tenant (`TenantId.Default` or `TenantId.NotSet`) | `EventStoreNamespaceName.Default` |

Single-tenant applications can use the default namespace. For a multi-tenant application, decide whether requests without a resolved tenant may access default-namespace data. If not, reject missing or invalid tenant context before accessing the store. Namespace selection does not make that access-policy decision for you.

For direct MongoDB or EF queries, configure the corresponding provider's tenancy and align it with the Chronicle sink. A shared CLR read-model type does not make an arbitrary database query namespace-aware.

:::caution[Isolation follows the tenant resolver, not Chronicle]
The namespace follows the tenant id Arc supplies. If an untrusted header can select another tenant, namespace mapping will use that selection too. Choose a trusted tenant source and authorize the caller's access to it before performing store operations.
:::

## Configure how the tenant is resolved

Configure the tenant source in Arc: a claim, a header, a subdomain, or a custom resolver. Chronicle consumes the resulting tenant context.

See [Tenancy](../tenancy/index.md) for the resolver choices and configuration, and [Namespaces](/chronicle/namespaces/) for what a Chronicle namespace is and how it partitions the event store.

## Where the namespace's data lives

A namespace name does not select the application's database connection. The Chronicle kernel decides which database, server, and credentials back the namespace, using its defaults or its own connection resolution. Follow [Tenancy end to end](../tenancy/end-to-end.md#chronicle-the-kernel-decides-its-own-placement) to align that placement with Arc's MongoDB or EF Core queries. Chronicle's [Tenancy](/chronicle/tenancy/) covers the kernel configuration.

## Request unit-of-work binding

The ASP.NET Core `WithChronicle()` composition preserves Chronicle's scoped, deferred request unit-of-work manager. Resolving the manager or beginning a unit of work does not resolve the event store or select its namespace. The first event-store operation binds the unit to the tenant resolved at that point. An empty request never needs a store.

For claim-based tenancy, authenticate before performing event-store operations. Middleware may begin the unit of work before authentication without selecting an anonymous tenant's namespace. Once the store has been used, the unit stays bound to that tenant for its lifetime.

An explicitly registered `IUnitOfWorkManager` takes precedence and owns its tenant-binding and lifecycle behavior. The generic-host composition resolves the manager through the scoped event store instead; see the [package composition details](cratis-package.md#running-arc-or-chronicle-on-their-own) before applying request-binding assumptions to that path.
