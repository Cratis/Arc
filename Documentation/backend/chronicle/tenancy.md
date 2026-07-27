---
title: Tenancy
description: Each tenant's events and projections land in their own Chronicle namespace automatically, resolved from Arc's tenant context with no per-query filtering.
---

The usual way multi-tenancy goes wrong is a forgotten `WHERE TenantId = ...`. One query misses the filter and one tenant sees another's data. The defense is discipline, applied everywhere, forever.

Chronicle removes the filter instead of asking you to remember it. Each tenant gets its own **namespace** — a separate event store partition — so there is no shared table to accidentally read across. Arc wires the two together: whatever resolved Arc's tenant for the current request also picks the Chronicle namespace, automatically.

## What you write

Nothing. Adding the Chronicle integration registers `TenantNamespaceResolver`, and from then on every append, projection, and query runs in the current tenant's namespace:

```csharp
[Command]
public record RegisterAuthor(AuthorId Id, AuthorName Name)
{
    public AuthorRegistered Handle() => new(Name);
}
```

That command is tenant-aware. There is no tenant parameter, no filter, and no namespace argument — the event lands in the namespace belonging to whoever made the request.

## The mapping rule

`TenantNamespaceResolver` implements Chronicle's `IEventStoreNamespaceResolver` and reads Arc's current tenant:

| Arc tenant context | Chronicle namespace |
|---|---|
| A tenant is resolved | the tenant id, used verbatim as the namespace name |
| No tenant is resolved (`TenantId.NotSet`) | `EventStoreNamespaceName.Default` |

The fallback matters: a request with no tenant does not fail and does not leak across tenants — it uses the default namespace. Single-tenant applications therefore need no tenancy configuration at all; they simply always use the default.

:::caution[Isolation follows the tenant resolver, not Chronicle]
Chronicle isolates by whatever tenant id Arc hands it. If the resolver picks the wrong tenant — a spoofable header, say — Chronicle will faithfully write to the wrong namespace. The security boundary lives in how the tenant is resolved, so choose that resolver deliberately.
:::

## Configure how the tenant is resolved

Chronicle consumes Arc's tenant resolution rather than defining its own, so the interesting decision — where the tenant comes from — is an Arc-level one: a claim, a header, a subdomain, or a custom resolver.

See [Tenancy](../tenancy/index.md) for the resolver choices and configuration, and [Namespaces](/chronicle/namespaces/) for what a Chronicle namespace is and how it partitions the event store.
