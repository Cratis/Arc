---
title: Identity
description: How Arc composes application identity details from an authenticated principal for the frontend, and how that differs from authorization.
---

Arc identity support composes domain-specific details from an authenticated request principal and presents them to frontend clients. It does not modify or validate the provider's token. Authentication, pipeline authorization, and identity presentation are separate concerns.

## Overview

Provider identities often lack application-specific display information. Arc lets you compose it in the backend and publish a consistent frontend payload. A details provider can make an application-entry decision; it runs for every identity request. Protect commands and queries independently with [pipeline authorization](../core/authorization.md).

Key capabilities:

- Enrich provider identity with domain-specific details
- Supply an application-entry decision on every identity request
- Return a single consolidated identity payload
- Present details to frontend clients through `/.cratis/me`

```mermaid
flowchart LR
    A[Authenticated Request Principal] --> B[Fresh Identity Enrichment]
    B --> C{Authorized?}
    C -- No --> D[HTTP 403]
    C -- Yes --> E[Identity Details JSON]
    E --> F[/.cratis/me response/]
    F --> G[Frontend Identity Consumption]
```

> [!WARNING]
> The identity is presentation data. Do not use its flags, roles, or details in place of backend authorization. See [provider flow](provider-flow.md) and [trust](identity-provider-service.md#trust). Arc no longer uses the `.cratis-identity` cookie; see [migrating from the identity cookie](migrating-from-the-identity-cookie.md).

## Topics

| Topic | Description |
| ------- | ----------- |
| [Provider Flow](./provider-flow.md) | Endpoint mapping, provider implementation, request flow, and frontend integration. |
| [Identity Contracts](./contracts.md) | `IdentityProviderContext` and `IdentityDetails` structures used by providers. |
| [IdentityProvider Service](./identity-provider-service.md) | Advanced runtime identity retrieval and mutation with `IIdentityProvider`. |
| [Development and Topologies](./development-and-topologies.md) | Development endpoints plus single-service and multi-service composition patterns. |
| [Migrating from the Identity Cookie](./migrating-from-the-identity-cookie.md) | What changed when Arc stopped trusting the `.cratis-identity` cookie, and what to change. |
