# Tenancy Best Practices

- Choose a resolver that aligns with your authentication and request flow.
- Validate that the requester is authorized to access the resolved tenant.
- Include the tenant ID in cache keys, logs, and telemetry.
- Keep tenant IDs stable and opaque to avoid enumeration.
- Prefer tenant-aware data stores and avoid cross-tenant queries.
- Use the development resolver only in local or test environments.

