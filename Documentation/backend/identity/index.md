# Identity

Arc identity support composes domain-specific details from an authenticated request principal and presents them to frontend clients. It does not modify or validate the provider's token. Authentication, pipeline authorization, and identity presentation are separate concerns.

## Overview

Provider identities often lack application-specific display information. Arc lets you compose it in the backend and publish a consistent frontend payload. A details provider can make an application-entry decision during fresh enrichment, but cached cookie results bypass recomputation. Protect commands and queries independently with [pipeline authorization](../core/authorization.md).

Key capabilities:

- Enrich provider identity with domain-specific details
- Supply an application-entry decision during fresh enrichment
- Return a single consolidated identity payload
- Present cached details to frontend clients

```mermaid
flowchart LR
    A[Authenticated Request Principal] --> B[Fresh Identity Enrichment]
    B --> C{Authorized?}
    C -- No --> D[HTTP 403]
    C -- Yes --> E[Identity Details JSON]
    E --> F[.cratis-identity Cookie]
    F --> G[Frontend Identity Consumption]
```

> [!WARNING]
> The `.cratis-identity` cookie is unsigned, client-controlled presentation data. Do not trust its flags, roles, or details for backend authorization. See [provider flow](provider-flow.md) and [cookie trust and caching](identity-provider-service.md).

## Topics

| Topic | Description |
| ------- | ----------- |
| [Provider Flow](./provider-flow.md) | Endpoint mapping, provider implementation, request flow, and frontend cookie integration. |
| [Identity Contracts](./contracts.md) | `IdentityProviderContext` and `IdentityDetails` structures used by providers. |
| [IdentityProvider Service](./identity-provider-service.md) | Advanced runtime identity retrieval and mutation with `IIdentityProvider`. |
| [Development and Topologies](./development-and-topologies.md) | Development endpoints plus single-service and multi-service composition patterns. |
