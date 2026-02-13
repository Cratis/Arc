---
uid: Arc.Chronicle.Tenancy
---

# Tenancy in Chronicle

Chronicle uses tenant context to keep event streams and projections isolated between tenants. When you enable the Chronicle extension for Arc, a tenant-aware namespace resolver is automatically registered so each tenant writes to its own event store [namespace](xref:Chronicle.Namespaces).

## TenantNamespaceResolver

`TenantNamespaceResolver` maps the resolved tenant ID to the Chronicle event store [namespace](xref:Chronicle.Namespaces):

- When a tenant is resolved, the [namespace](xref:Chronicle.Namespaces) is set to the tenant ID.
- When no tenant is resolved, the default [namespace](xref:Chronicle.Namespaces) is used.

This keeps event data separated without requiring manual namespace management in your application code.

## How It Is Wired

When you add the Chronicle extension for Arc, the tenant namespace resolver is automatically hooked up as part of the Chronicle integration. You do not need to register it manually.

## Configure Core Tenancy

Chronicle relies on the core tenancy resolution from Arc. For resolver choices and configuration options, see the main tenancy documentation:

- [Tenancy](../tenancy/index.md)

