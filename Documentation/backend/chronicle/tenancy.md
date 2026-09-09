---
title: Tenancy
description: Map Arc's tenant context to Chronicle namespaces, and configure access policy and other storage providers separately.
---

The usual way multi-tenancy goes wrong is a forgotten `WHERE TenantId = ...`. One query misses the filter and one tenant sees another's data. The defense is discipline, applied everywhere, forever.

Chronicle removes the filter instead of asking you to remember it. Each tenant gets its own **namespace** — a separate event store partition — so there is no shared table to accidentally read across. Arc wires the two together: whatever resolved Arc's tenant for the current request also picks the Chronicle namespace, automatically.

## What you write

Adding the Chronicle integration registers `TenantNamespaceResolver`. Namespace-aware Chronicle client operations use that mapping. It does not add tenant filters to arbitrary query code or independently configured databases. This command fragment assumes a supported `AuthorId : EventSourceId<Guid>` and an `AuthorRegistered` event:

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
| --- | --- |
| A tenant is resolved | the tenant id, used verbatim as the namespace name |
| Default or unset tenant (`TenantId.Default` or `TenantId.NotSet`) | `EventStoreNamespaceName.Default` |

The fallback does not reject missing tenants; it deliberately selects the default namespace. Single-tenant applications can use that mapping. Multi-tenant applications must decide whether access to default-namespace data is allowed and reject missing/invalid tenant context when it is not. This mapping alone is neither proof of a cross-tenant leak nor a complete isolation policy.

For direct MongoDB or EF queries, configure the corresponding provider's tenancy and align it with the Chronicle sink. A shared CLR read-model type does not make an arbitrary database query namespace-aware.

:::caution[Isolation follows the tenant resolver, not Chronicle]
Chronicle isolates by whatever tenant id Arc hands it. If the resolver picks the wrong tenant — a spoofable header, say — Chronicle will faithfully write to the wrong namespace. The security boundary lives in how the tenant is resolved, so choose that resolver deliberately.
:::

## Configure how the tenant is resolved

Chronicle consumes Arc's tenant resolution rather than defining its own, so the interesting decision — where the tenant comes from — is an Arc-level one: a claim, a header, a subdomain, or a custom resolver.

See [Tenancy](../tenancy/index.md) for the resolver choices and configuration, and [Namespaces](/chronicle/namespaces/) for what a Chronicle namespace is and how it partitions the event store.
