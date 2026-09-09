---
title: Database isolation
description: Distinguish MongoDB tenant naming from application-owned EF Core isolation and optional Chronicle namespaces.
---

Resolving a tenant ID selects a context. Whether that context selects separate storage depends on the integration; it does not prove that the caller may access the tenant.

## MongoDB database naming

With the optional Arc MongoDB integration, `DefaultMongoDatabaseNameResolver` uses:

```text
{BaseDatabaseName}             // TenantId.NotSet or TenantId.Default
{BaseDatabaseName}+{TenantId}  // any other tenant
```

For a base name `MyDatabase`, both `TenantId.NotSet` and `TenantId.Default` select `MyDatabase`; tenant `acme-corp` selects `MyDatabase+acme-corp`. `TenantId.IsDefault` covers both default aliases. Preserve that behavior in custom resolvers unless you intentionally migrate to different naming.

Database naming separates destinations; it does not check membership or database permissions. Reject unauthorized tenant selections before accessing tenant-scoped services, and avoid singleton capture of tenant-scoped collections. See [MongoDB tenancy](../mongodb/tenancy.md).

## Entity Framework Core database naming

Arc's EF Core registration uses the configured connection string, including in pooled context registration. It does **not** automatically append the tenant ID or create a database per tenant.

Choose and implement relational isolation in the application: separate connections/databases, schemas, or appropriately enforced tenant filtering. Verify tenant selection, all read/write paths, and pooled-context reuse for that design. A tenant context by itself does not make an ordinary `DbContext` tenant-aware. See [EF Core configuration](../entity-framework/automatic-database-hookup.md); this page does not prescribe an untested pooled-context isolation recipe.

## Chronicle event store namespace

Only when you enable the optional [Chronicle integration](../chronicle/tenancy.md) does Arc's tenant context select a Chronicle event-store namespace. That is separate from MongoDB/EF Core storage behavior and is not a standalone Arc requirement.

## Verify the boundary

Test unset, named default, and two named tenants, including concurrent requests and reused scopes/pools. Verify both permission rejection and the actual storage destination. Test caches and background execution too; storage naming alone cannot prevent cross-tenant data from a wrongly reused service instance.
