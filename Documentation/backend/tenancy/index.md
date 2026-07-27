---
uid: Arc.Tenancy
---

# Tenancy Overview

Tenancy means your application serves multiple customers or organizational units while keeping their data and operations isolated. A tenant could be a customer, a business unit, or any logical boundary that must remain separate from others.

## Why Tenancy Matters

Tenancy keeps data and behavior isolated between customers or organizational units while still sharing the same application deployment. Arc provides tenant resolution, tenant context access, and tenant-aware integrations to help you maintain strict separation and predictable behavior.

- **Data isolation**: Each tenant should only see its own data, even when sharing infrastructure.
- **Compliance**: Many regulatory requirements demand strict separation and auditable access.
- **Operational safety**: Isolation reduces the blast radius of mistakes, queries, and deployments.
- **Scalability**: Tenancy enables predictable scaling by segmenting traffic and storage by tenant.

Arc helps you establish a clear tenant boundary by resolving a tenant ID for each request, keeping that context available across the application, and wiring tenant-aware integrations to data stores and event streams.

## Best Practices

- Choose a resolver that aligns with your authentication and request flow.
- Validate that the requester is authorized to access the resolved tenant.
- Include the tenant ID in cache keys, logs, and telemetry.
- Keep tenant IDs stable and opaque to avoid enumeration.
- Prefer tenant-aware data stores and avoid cross-tenant queries.
- Use the development resolver only in local or test environments.

## Security Considerations

- Ensure tenant ID resolution happens only after authentication.
- Enforce tenant membership checks in application services and policies.
- Log tenant access for audits and incident investigation.
- Prevent tenant ID spoofing by validating headers, claims, and parameters.
- Treat tenant ID as sensitive metadata and avoid exposing it unnecessarily.

## Topics

- [Tenancy overview](./overview.md)
- [Resolving tenant IDs](./resolvers.md)
- [Configuration](./configuration.md)
- [Tenant context access](./tenant-context.md)
- [Database isolation](./database-resolvers.md)

