---
title: Connection resolution contract
description: The language-neutral REST contract an HTTP endpoint implements to tell Arc and Chronicle where to connect and which identity to use, for MongoDB and SQL storage alike.
---

This page specifies the endpoint behind the `Http` connection resolver. When Arc or Chronicle needs a storage connection, it calls your endpoint and asks where to connect and who to connect as. The contract is the same for every storage, MongoDB as well as SQL Server, PostgreSQL and SQLite, and it is plain JSON over HTTP, so you can implement the endpoint in any language or platform.

For how the resolver is configured on the caller's side, see [connection resolution](/arc/backend/csharp/tenancy/connection-resolution/). The calling host decides which values it sends in the request `scope`. Arc's are listed under [Scope values](#scope-values); Chronicle documents its own in [storage connection resolution](/chronicle/hosting/configuration/storage-connection-resolution/).

## Operation

| | |
| --- | --- |
| Method | `POST` |
| URL | The configured endpoint, used exactly as configured. The caller adds no path or query string. |
| Request body | `application/json`, UTF-8. See [Request](#request). |
| Request headers | `Content-Type: application/json; charset=utf-8`, plus every header configured for the resolver, such as an API key or a bearer token. |
| Response body | `application/json` on `200 OK`. See [Response](#response). |
| Timeout | 10 seconds unless configured otherwise. A timeout is a failure. |

The caller sends one request per storage, purpose and scope, and caches the answer (see [Caching](#caching)). Calls happen when a connection is about to be used, so your endpoint sits on the path of the first data access after a cache entry expires. Keep it fast.

## Request

```json
{
  "storage": "SqlServer",
  "purpose": "Runtime",
  "scope": {
    "tenant": "acme"
  }
}
```

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `storage` | string | Yes | The kind of storage being connected to: `MongoDB`, `SqlServer`, `PostgreSql` or `Sqlite`. Treat it as an open set and answer an unknown value with `204` or an error. |
| `purpose` | string | Yes | `Runtime` for reading and writing data, `Schema` for schema changes such as migrations or creating tables. Use it to hand out a more privileged identity for `Schema` only. |
| `scope` | object | Yes | String keys to string values identifying what is being connected, such as a tenant. Never `null`, possibly empty. The keys depend on the calling host. |

Ignore properties you do not recognize. New ones may be added without notice.

### Scope values

When Arc calls the endpoint, the scope holds one key:

| Key | Value |
| --- | --- |
| `tenant` | The tenant of the current request. It is `Default` when no tenant is set. |

Chronicle sends a different set, describing the cluster, event store or namespace it connects for. See [storage connection resolution](/chronicle/hosting/configuration/storage-connection-resolution/).

## Response

### Status codes

| Status | Meaning |
| --- | --- |
| `200 OK` | The body is a resolution, described below. Parts present in it are applied; parts left out keep their configured value. `{}` changes nothing. |
| `204 No Content` | Keep the configured connection unchanged. Use it when you have nothing to say about this request. |
| Any other status | Resolution fails, and so does the connection that needed it. The caller does not read the body and does not retry immediately; the next connection attempt calls you again. |

Redirects are followed. A `200 OK` whose body is not a JSON object is a failure.

### Body

Every property is optional and may be left out or set to `null`.

```json
{
  "connectionString": "Server=sql-eu-2.example;Encrypt=true",
  "database": "tenant-acme",
  "schema": "acme",
  "identity": {
    "accessToken": "eyJ0eXAiOiJKV1Qi..."
  },
  "expiresAt": "2026-10-16T12:00:00+00:00"
}
```

| Property | Type | Description |
| --- | --- | --- |
| `connectionString` | string | Replaces the configured connection string, in the native format of the storage: a `mongodb://` or `mongodb+srv://` URI for MongoDB, an ADO.NET style string for SQL Server, PostgreSQL and SQLite. |
| `database` | string | The database to use. Replaces the database in the connection string. For SQLite this is the database file. |
| `schema` | string | The schema to use, for storage that has schemas. Ignored where the calling host does not support placing data in a schema. |
| `identity` | object | Who to connect as. Replaces any identity resolved before it as a whole. See [Identity](#identity). |
| `expiresAt` | string | When this answer stops being valid, as an ISO 8601 date and time with an offset. Set it whenever you return a short-lived credential. |

Property names are matched without regard to case, and unknown properties are ignored.

### Identity

| Property | Type | Description |
| --- | --- | --- |
| `userName` | string | User name for password authentication. |
| `password` | string | Password for password authentication. |
| `accessToken` | string | A bearer access token, such as one issued by Microsoft Entra ID or another OpenID Connect provider. |

Return either a user name and password, or an access token. How each storage uses them:

| Storage | `userName` + `password` | `accessToken` |
| --- | --- | --- |
| MongoDB | SCRAM authentication against the connection string's `authSource`, else its database, else `admin` | `MONGODB-OIDC` authentication. The caller asks you again when the driver needs a fresh token. |
| SQL Server | SQL authentication | Entra ID token authentication. The token must be issued for `https://database.windows.net/`. |
| PostgreSQL | Password authentication | Sent as the password, as Azure Database for PostgreSQL and Amazon RDS IAM authentication expect. |
| SQLite | Ignored | Ignored |

## Two slots, one endpoint or two

The caller has two slots, `Connection` and `Identity`, and each can point to its own endpoint. Both endpoints receive the same request and implement this same contract. The caller applies the answer from the `Connection` slot first and the answer from the `Identity` slot on top of it, so an answer only needs to contain the parts its endpoint is responsible for. An identity-only endpoint returns only `identity` and `expiresAt`.

## Caching

The caller keeps each answer **in memory**, keyed by `storage`, `purpose` and `scope`, never by credential:

- With `expiresAt`, the answer is used until one minute before that time, and then resolved again. Return a later `expiresAt` with every fresh credential.
- Without `expiresAt`, the answer is used for the configured cache duration, five minutes by default.
- When a connection fails to authenticate, the caller drops the cached answer, and the next connection asks you again. Rotating a credential therefore takes effect at the latest on the next failed sign-in.

A `204` is cached like an answer that changes nothing.

## Security

- Serve the endpoint over HTTPS only. Answers contain credentials.
- Authenticate the caller. Configure an API key or a bearer token as a resolver header, and reject requests without it with `401` or `403`; the caller then fails instead of connecting.
- Authorize the scope. Only return a connection that the caller may use for the tenant or namespace it asked about. Resolution chooses a destination; it is not an authorization check on the end user.
- Prefer short-lived access tokens with an `expiresAt` over long-lived passwords.
- Do not log response bodies. The caller never logs credentials it receives.

## OpenAPI description

```yaml
openapi: 3.1.0
info:
  title: Connection resolution
  version: "1.0"
paths:
  /:
    post:
      summary: Resolve where and as whom to connect to a storage.
      description: The path is whatever the resolver is configured with.
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/ConnectionRequest"
      responses:
        "200":
          description: Apply the parts that are present.
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/ResolvedConnection"
        "204":
          description: Keep the configured connection unchanged.
        default:
          description: Resolution failed. The connection is not opened.
components:
  schemas:
    ConnectionRequest:
      type: object
      required: [storage, purpose, scope]
      properties:
        storage:
          type: string
          description: The kind of storage. An open set.
          examples: [MongoDB, SqlServer, PostgreSql, Sqlite]
        purpose:
          type: string
          enum: [Runtime, Schema]
        scope:
          type: object
          additionalProperties:
            type: string
    ResolvedConnection:
      type: object
      properties:
        connectionString:
          type: [string, "null"]
        database:
          type: [string, "null"]
        schema:
          type: [string, "null"]
        identity:
          oneOf:
            - $ref: "#/components/schemas/ConnectionIdentity"
            - type: "null"
        expiresAt:
          type: [string, "null"]
          format: date-time
    ConnectionIdentity:
      type: object
      properties:
        userName:
          type: [string, "null"]
        password:
          type: [string, "null"]
        accessToken:
          type: [string, "null"]
```
