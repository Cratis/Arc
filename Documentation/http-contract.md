---
title: HTTP contract
description: The language-neutral Arc wire contract - routes, headers, envelopes, statuses, identity, and validation values - plus where the C# and JVM implementations differ.
---

Arc ships two backend implementations: [C# on ASP.NET Core](/arc/backend/csharp/) and
[Kotlin and Java on Spring Boot](/arc/backend/kotlin/). They are one product because
they speak one wire protocol. A TypeScript client generated from a C# backend has to
work against a JVM backend, and the same `fetch` call has to mean the same thing on
both.

This page is that protocol. Every statement here is true of both implementations. Where
behavior genuinely differs, it is not on this page - it is in
[Where the implementations differ](#where-the-implementations-differ), which names both
behaviors so a client author can see the edge before hitting it.

:::note[This is the contract, not a host guide]
Configuration keys, framework registration, and runtime tuning are implementation
concerns. See the [C# backend documentation](/arc/backend/csharp/) and the
[Kotlin and Java backend documentation](/arc/backend/kotlin/) for those, and the
[JVM conformance notes](/arc/backend/kotlin/reference/http-contract/) for the paired
HTTP evidence that backs the JVM side.
:::

## Routes and methods

| Endpoint | Method | Contract |
| --- | --- | --- |
| Command route | `POST` | JSON command body; returns a command result envelope. |
| `<command-route>/validate` | `POST` | Runs authorization and validation without invoking the handler. |
| Query route | `GET` | Arguments in query-string parameters; returns a query result envelope. |
| Query route | `QUERY` | Structured JSON request body; enabled by default; responds with `Cache-Control: no-store`. |
| Observable query route | `GET` | HTTP snapshot, direct SSE when the request accepts `text/event-stream`, or a direct WebSocket upgrade. |
| `/.cratis/queries/ws` | WebSocket | Multiplexed observable-query hub. |
| `/.cratis/queries/sse` | `GET` | Multiplexed SSE hub; the stream opens with a `Connected` message carrying the connection ID. |
| `/.cratis/queries/sse/subscribe` | `POST` | Adds or revision-replaces a subscription on an established SSE connection. |
| `/.cratis/queries/sse/unsubscribe` | `POST` | Cancels a subscription or records its revision tombstone. |
| `/.cratis/commands` | `GET` | Anonymous command introspection metadata. |
| `/.cratis/queries` | `GET` | Anonymous query introspection metadata. |
| `/.cratis/queries/health` | `GET`, `QUERY` | Current observable connection and subscription health, served as an observable query. |
| `/.cratis/me` | `GET` | Registered only when an identity details provider is registered. |
| `/.cratis/identity-details/schema` | `GET` | Always registered; returns `{}` when no provider is registered. |
| `/.cratis/users` | `GET` | Anonymous development-user discovery; returns an array, or `[]` when no provider contributes. |
| `/.cratis/tenants` | `GET` | Anonymous development-tenant discovery; returns an array, or `[]` when no provider contributes. |

Conventional routes are built from the configured route prefix (`api` by default),
the artifact's namespace with the configured number of leading segments skipped,
kebab-cased, and the artifact name. An explicitly declared path is preserved exactly.
The QUERY method is registered alongside GET for every query route, including
observable ones, and can be switched off so query routes accept GET only.

## Request headers

| Header | Behavior |
| --- | --- |
| `X-Correlation-ID` | The correlation header, configurable. A valid UUID is reused; a missing or invalid value is replaced. The effective UUID is echoed on the response under the configured name. See [Correlation](#correlation). |
| `X-Allowed-Severity` | The maximum nonblocking validation severity for the request. See [the divergence note](#x-allowed-severity-parsing-and-reach) before relying on it. |
| `x-cratis-tenant-id` | The tenant header, configurable. See [Tenant resolution](#tenant-resolution). |

Header lookup is case-insensitive, as HTTP requires.

## Correlation

Correlation is diagnostic identity. It never carries authority, and no part of the
contract grants a caller anything on the strength of a correlation value.

- An inbound correlation header value is reused only when it is a syntactically valid
  UUID. Anything else - including an empty value or arbitrary text - is replaced by a
  freshly generated identifier.
- The effective value is always emitted in canonical UUID form, so client-supplied text
  never reaches a response header.
- Every Arc response carries the configured correlation header.
- The same value appears as `correlationId` in the command and query result envelopes,
  so a caller can correlate a response body with its transport header without parsing
  anything.

## QUERY body

The QUERY request body carries what a GET would put in the query string:

```json
{
  "arguments": { "name": "Ada" },
  "paging": { "page": 0, "pageSize": 25 },
  "sorting": { "field": "name", "direction": "desc" }
}
```

`arguments`, `paging`, and `sorting` are all optional. `paging` takes `page` (zero-based)
and `pageSize`; `sorting` takes `field` and `direction`. A QUERY response always carries
`Cache-Control: no-store`.

Argument names are matched case-insensitively against the query's declared parameters,
and a matched value is converted to the parameter's declared type. `desc` selects
descending order and `asc` selects ascending, case-insensitively - see
[the divergence note](#sorting-direction-vocabulary) for the longer spellings.

Over GET the same request is expressed with reserved query-string parameters `page`,
`pageSize`, `sortBy`, and `sortDirection`; every other parameter is an argument.

## Unknown fields

A command body is deserialized into the command type by the host's configured JSON
reader. Neither implementation turns on strict unknown-property rejection for that
reader, so a command body carrying a field the command does not declare executes the
command with the extra field discarded. `{"value":"hello","unexpected":"extra"}` is
accepted, and `unexpected` is dropped.

This holds for the command route and its `/validate` sibling alike. The QUERY envelope
is stricter on one implementation only - see
[Unknown fields in the QUERY envelope and in query arguments](#unknown-fields-in-the-query-envelope-and-in-query-arguments).

## Observable query transport

An observable query is a query whose result is a stream rather than a value. The same
route serves three transports, selected by the request.

### HTTP snapshot

A plain GET returns the stream's current value as an ordinary query result with 200 when
one can be read. When the stream has not yet produced a value, the response is a
not-ready query result - `isReady: false`, no exception - with `202 Accepted`. A caller
that would rather wait sends `waitForFirstResult=true`, optionally with
`waitForFirstResultTimeout` in seconds, and the host waits for the first result within a
bounded budget. Both parameters are reserved: they are never treated as query arguments.

While waiting, expiry of that budget returns `408 Request Timeout` with an error envelope,
and a stream that completes without ever producing a value returns 500.

Not-ready is a transient state, not a failure. `hasExceptions` stays false so a client
does not read a pending stream as a crash.

### Direct SSE

A GET whose `Accept` header includes `text/event-stream` upgrades the same route to
Server-Sent Events. Each emission is written as exactly:

```text
data: {query result envelope}

```

That is the `data:` field prefix, a space, the JSON envelope, and two newlines. There is
no event name and no `id:` field.

### Direct WebSocket

A WebSocket upgrade on the same route carries a message envelope with a string `type` of
`Data`, `Ping`, or `Pong`. A `Data` message carries the query result envelope in `data`;
`Ping` and `Pong` carry a Unix-millisecond `timestamp`.

### Multiplexed hubs

`/.cratis/queries/ws` and `/.cratis/queries/sse` multiplex many subscriptions over one
connection. Hub messages use a shared envelope with exact PascalCase `type` values:
`Subscribe`, `Unsubscribe`, `QueryResult`, `Unauthorized`, `Error`, `Ping`, `Pong`, and
`Connected`, alongside `queryId`, `revision`, `payload`, `timestamp`,
`keepAliveIntervalMs`, and `supportsSubscriptionRevisions`.

A hub connection opens with a `Connected` message whose `payload` is the server-assigned
connection ID, and which carries `keepAliveIntervalMs` and
`supportsSubscriptionRevisions: true`. SSE clients pass that connection ID on the
subscribe and unsubscribe POST bodies to correlate them with the stream.

A subscribe payload is flat:

```json
{
  "connectionId": "…",
  "queryId": "client-assigned",
  "revision": 1,
  "request": {
    "queryName": "MyApp.Authors.Listing.AllAuthors",
    "arguments": { "name": "Ada" },
    "page": 0,
    "pageSize": 25,
    "sortBy": "name",
    "sortDirection": "desc",
    "transferMode": "delta"
  }
}
```

`arguments` is a map of string values. `revision` is optional; when present it must be a
positive integer no greater than 9007199254740991, the JavaScript safe-integer limit,
and it orders operations for one `queryId` so a duplicate is idempotent, a stale message
is ignored, and an unsubscribe tombstone can arrive before a delayed subscribe. Result
and terminal messages echo `queryId` and `revision`.

`transferMode` is matched case-insensitively. `full` sends each snapshot without a change
set; `delta` sends a full first snapshot and then only a change set. It is a preference
about how much of each snapshot travels, not part of what the subscription means, so a
textual value outside the known set is served exactly as one that sent no `transferMode`
at all: snapshot plus change set on every result.

Each subscription is authorized independently by the ordinary query pipeline. A
subscription the caller may not have terminates with an `Unauthorized` message for that
`queryId` without disturbing the connection's other subscriptions. An unknown SSE
connection ID returns 404.

## Tenant resolution

A tenant is resolved per request from configured sources - most commonly the
`x-cratis-tenant-id` header, with a query parameter, the request host, a principal claim,
or a fixed value as alternatives. The resolved tenant is established for the request
before the command or query runs, and applies to HTTP requests, SSE subscriptions, and
WebSocket handshakes alike.

## Authentication and introspection

When authentication handlers are registered, they run in order and the first handler that
recognizes the request decides the outcome:

- An **authenticated** result supplies the request principal, and the chain stops.
- A **failed** result is terminal. No later handler runs, and no later handler can
  override the rejection with a success. The failure surfaces only as a generic 401.
- An **anonymous** result means the handler did not recognize the request and lets later
  handlers try. Anonymous is the final outcome only when no handler recognized the
  request at all.

An artifact marked to allow anonymous access proceeds without an authenticated result.
The introspection, development-user, development-tenant, and identity-schema routes are
anonymous endpoints.

`/.cratis/commands` returns, for each command, its name, namespace, route, type, a
single-line documentation summary taken from the source language's doc comments (empty
when the artifact carries none), and a JSON schema for the payload. `/.cratis/queries`
returns the same for each query plus its fully qualified query name and an arguments
schema whose `required` list holds the parameters the query actually requires.

:::caution[Introspection is anonymous on both implementations]
These endpoints expose operation names, types, routes, and schemas to any caller,
regardless of whether that caller may execute the operations. If that metadata is
sensitive, restrict these exact paths at trusted ingress.
:::

## Command result envelope

| Field | Type | Notes |
| --- | --- | --- |
| `correlationId` | UUID | Always present. |
| `isAuthorized` | Boolean | Authorization outcome. |
| `validationResults` | Array | Always present, including when empty. |
| `exceptionMessages` | Array | Always present; production redaction replaces detail. |
| `exceptionStackTrace` | String | Empty when redacted or absent. |
| `authorizationFailureReason` | String | Empty when absent. |
| `isValid` | Boolean | True only when `validationResults` is empty, regardless of severity. |
| `hasExceptions` | Boolean | True when `exceptionMessages` is nonempty. |
| `isSuccess` | Boolean | Authorized, valid, and exception-free. |
| `response` | Any | Omitted when null. A failed result never carries a response. |

`isValid`, `hasExceptions`, and `isSuccess` are derived, not independent inputs. A
response produced by a handler is taken back before serialization when the command did
not ultimately succeed, so a client never receives a value it must not act on alongside
a failure.

Outside Development, exception messages and stack traces are replaced by a generic
message before serialization. The full detail is logged server-side first, and the
correlation ID is retained, so `hasExceptions` - and therefore the status code - is
unchanged by redaction.

## Query result envelope

| Field | Type | Notes |
| --- | --- | --- |
| `correlationId` | UUID | Always present. |
| `data` | Any | The query data; omitted when null. |
| `isReady` | Boolean | False for an observable query that has not produced its first result. |
| `isAuthorized` | Boolean | Authorization outcome. |
| `validationResults` | Array | Always present, including when empty. |
| `exceptionMessages` | Array | Always present; production redaction replaces detail. |
| `exceptionStackTrace` | String | Empty when redacted or absent. |
| `paging` | Object | Always present. |
| `changeSet` | Object | Present only for a delta-mode observable emission. |
| `isValid` | Boolean | True only when `validationResults` is empty. |
| `hasExceptions` | Boolean | True when `exceptionMessages` is nonempty. |
| `isSuccess` | Boolean | Ready, authorized, valid, and exception-free. |

`paging` carries `page` (zero-based), `size`, `totalItems`, and a calculated `totalPages`
that is zero when `size` is zero and otherwise `totalItems` divided by `size`, rounded up.

When `changeSet` is absent, `data` is the complete current snapshot.

## HTTP statuses

| Condition | Status |
| --- | --- |
| Successful command or query | 200 |
| Query not ready | 202 |
| Validation failure, malformed request, or an unresolvable command dependency | 400 |
| Authentication failure | 401 |
| Command or query authorization failure | 403 |
| Observable wait for the first result timed out | 408 |
| Pipeline or host exception | 500 |

Status selection is ordered, and the order matters when a result carries more than one
kind of failure: success first, then authorization failure (403), then validation
failure (400), then not-ready (202), then exception (500). A failed result never exposes
a response value.

A malformed or wrong-typed request body is a client error, not a server fault: it becomes
a validation failure carrying the `malformedRequest` reason and returns 400 without
echoing the underlying parser message. The same applies to a value that cannot be bound
to the member it was sent for.

## Identity contract

`/.cratis/me` returns 401 for an unauthenticated principal, 403 when the details provider
rejects the caller, and 200 on success with:

```json
{
  "id": "…",
  "name": "…",
  "isAuthenticated": true,
  "isAuthorized": true,
  "roles": ["…"],
  "details": { }
}
```

`details` is application-specific and its shape is the one described by
`/.cratis/identity-details/schema`.

A successful response also sets a `.cratis-identity` cookie holding the Base64-encoded
response JSON. The cookie is deliberately client-readable (`HttpOnly=false`) so frontend
code can read the current identity without a round trip; it is `SameSite=Lax` with
`Path=/`. Its `Secure` attribute is set for an HTTPS request on both implementations; the
JVM additionally exposes a policy that can force or suppress it, documented in the
[JVM conformance notes](/arc/backend/kotlin/reference/http-contract/).

## Validation result

Every validation result carries:

| Field | Type | Notes |
| --- | --- | --- |
| `severity` | Number | See the severity table below. |
| `message` | String | A developer-facing diagnostic, free to change. |
| `members` | Array of string | The member names the result applies to. |
| `state` | Any | Optional, application-supplied. |
| `reason` | String | What composed the result. Defaults to `rule`. |
| `reasonDetail` | String | Optional finer identity within `reason`. |

| Severity | Wire value |
| --- | --- |
| `Unknown` | 0 |
| `Information` | 1 |
| `Warning` | 2 |
| `Error` | 3 |

`reason` is an **open set**, not an enum. Rejections are composed in Arc, in Chronicle,
and in application code, and a closed set would make every new kind a breaking change for
whoever switches over it. A client must tolerate values added later. The current values
are:

| Reason | Meaning |
| --- | --- |
| `rule` | An application-authored rule rejected the input. The message is the author's; show it. |
| `concurrencyViolation` | The target moved on since it was read. Retryable: re-read and resubmit. |
| `constraintViolation` | A store-level constraint rejected the write. |
| `validatorFailed` | A validator threw, and the framework substituted a result. Nothing of the authored rules survives. |
| `dependencyUnavailable` | Something the command needed was not available, so no rule was ever evaluated. |
| `malformedRequest` | The request itself could not be read or bound. No rule was reached. |

`reason` is the machine-readable counterpart to `message`, and `reasonDetail` says which
specific thing within that category produced the result - which constraint, for example -
so a client can branch on identity instead of matching prose.

`isValid` is false whenever `validationResults` is nonempty, **regardless of severity**.
Severity filtering happens before results reach the envelope, driven by the
`X-Allowed-Severity` header and the artifact's own settings; anything that survives that
filter is blocking.

`message`, `members`, `state`, `reason`, and `reasonDetail` are all client-visible and are
not redacted the way exception detail is. Keep secrets out of every field.

## Where the implementations differ

Everything above is common. The following is not, and each entry matters to someone
writing a client or reasoning about security. Each one names what the C# implementation
does and what the JVM implementation does.

### SSE connection ownership is compared on a different set of values

Both implementations verify that the caller of an SSE control POST is the caller that
opened the GET stream, and both answer a mismatch with 404 - the same status as an unknown
connection, so neither endpoint confirms that someone else's connection ID exists. What
they compare is not the same.

The SSE control endpoints take a `connectionId` in the POST body and act on the stream it
names.

- **C#**: the identity that opened the GET stream is captured once, while that request is
  still in flight, and each subsequent POST is compared against it on the identity
  identifier claim and authentication state. The display name and the resolved tenant are
  not compared: the name is mutable and a control POST is authorized with its own tenant
  rather than the connection's.
- **JVM**: the handshake captured when the GET stream opened is compared with the
  handshake of the POST request on principal ID, principal name, authentication state, and
  resolved tenant.

A connection opened by an unauthenticated caller carries no identity to bind to on either
implementation, so ownership cannot be distinguished between anonymous callers.

**Why it matters**: a client may not assume a control POST issued under a different
identity will be honored, and may not treat 404 as proof that a connection does not exist.
A connection whose caller has been re-authenticated under the same identity - a token
refresh while the stream stays open - keeps working on C#; on the JVM a change to the
principal name or the resolved tenant ends it. Do not treat a connection ID as a
bearer-style secret on either implementation.

### Query health is anonymous on .NET

`/.cratis/queries/health` reports connection and subscription identifiers, remote IP
addresses, user agents, and user identities.

- **C#**: the health read model is declared `[AllowAnonymous]`, so the endpoint is
  reachable without credentials.
- **JVM**: the route requires an authenticated caller whenever authentication handlers are
  configured.

**Why it matters**: a client may not assume the endpoint is protected. On a C# host,
restrict the path at ingress if the diagnostics are sensitive.

### Observable HTTP snapshot readiness

Both implementations return 200 when a current value can be read and 202 when it cannot,
but what counts as readable differs with the stream type each runtime uses.

- **C#**: the host reflects for a `Value` property on the streaming result. A
  `BehaviorSubject`-shaped observable exposes one and returns 200; a MongoDB-backed
  subject does not, so a GET returns 202 even when data exists in the collection.
- **JVM**: a `StateFlow` already holds a value and returns 200; a cold `Flow` or a JDK
  `Flow.Publisher` has nothing to serve yet and returns 202.

Arc's own `/.cratis/queries/health` is a worked example: on C# it is an observable query
whose returned subject exposes no `Value` property, so a plain GET answers 202 with a
not-ready envelope even though health data exists. Accept `text/event-stream`, upgrade to
WebSocket, or pass `waitForFirstResult=true` to read it over HTTP.

**Why it matters**: a client that treats 202 as an error will break against perfectly
healthy backends. Treat 202 as "subscribe, or retry with `waitForFirstResult=true`".

### Introspection metadata depth

- **C#**: command metadata is `name`, `namespace`, `route`, `type`,
  `documentationSummary`, and `payloadSchema`. Query metadata is the same plus
  `fullyQualifiedName` and `argumentsSchema`. Authorization, per-property validation
  metadata, transport, paging support, and HTTP-method preference are not reported.
- **JVM**: reports the above and additionally authorization, properties, and validation
  metadata for commands, and transport, paging support, and HTTP preference for queries.
  Query parameter metadata reports `hasDefault` for a defaulted Kotlin parameter, and the
  arguments schema excludes defaulted parameters from `required`. UUID and textual
  `java.time` terminals - `LocalDate`, `LocalTime`, `LocalDateTime`, `Instant`,
  `OffsetDateTime`, `ZonedDateTime`, `OffsetTime`, `Duration`, and `Period` - appear as
  scalar `string` schemas, including as collection elements, rather than object schemas.

The reported `route` is also not uniformly callable. C# query introspection builds the
conventional route and does not apply an explicit `[Path]`, so a query with a custom path
is introspected under a route that is not the one it answers on.

**Why it matters**: tooling built against the JVM's richer metadata degrades rather than
fails on a C# host, but tooling that *requires* `hasDefault` or authorization metadata
will find it absent, and tooling that treats an introspected `route` as a callable URL
will miss C# queries with custom paths. Neither implementation exposes a default
expression or an invented default value.

### Sorting direction vocabulary

- **C#**: only `desc` (case-insensitive) selects descending. Every other value - including
  `descending` - falls through to ascending, silently.
- **JVM**: `asc`, `ascending`, `desc`, and `descending` are all accepted
  case-insensitively, and anything else is rejected as `malformedRequest` with 400.

**Why it matters**: `sortDirection=descending` sorts the wrong way on a C# host and is
rejected outright on a JVM host. Send `asc` and `desc`; they are the only two values that
mean the same thing everywhere.

### Unknown fields in the QUERY envelope and in query arguments

- **C#**: the QUERY envelope is deserialized permissively. An undeclared envelope field is
  ignored, and a query argument whose name matches no declared parameter is carried
  through as a raw string rather than rejected.
- **JVM**: Arc validates the envelope's field set itself - only `arguments`, `paging`, and
  `sorting`, with only `page`/`pageSize` and `field`/`direction` inside them - and rejects
  an undeclared field with 400 and `malformedRequest`. An argument name matching no
  declared parameter, and two argument names colliding after case folding, are rejected
  the same way. Page values must be nonnegative.

**Why it matters**: a client typo in the envelope or an argument name fails loudly on a
JVM host and silently on a C# host. Do not rely on either behavior; send exactly the
fields the query declares.

### `X-Allowed-Severity` parsing and reach

- **C#**: the header is read on the command route only, and only as an integer. A
  non-numeric value such as `Warning` is silently ignored, and so is an out-of-range
  number.
- **JVM**: the header is accepted as a case-insensitive severity name or as the numeric
  wire value, on commands, queries, and hub subscriptions. A value that parses as neither
  is rejected as `malformedRequest`.

**Why it matters**: send the numeric wire value - `0`, `1`, `2`, or `3` - if the header
has to work on both. Do not send a name, and do not assume it affects queries.

### Admission control and request limits

- **C#**: there is no connection or subscription admission control and no request-body
  size gate in Arc itself. 429, 503, `Retry-After`, and 413 are not produced by Arc; any
  such response comes from the surrounding host or infrastructure.
- **JVM**: the host bounds concurrent Arc operations, physical observable connections,
  subscriptions per connection, outbound frames, inbound WebSocket message bytes,
  connection lifetime, and retained revision tombstones. Exhaustion fails closed: 503 with
  `Retry-After` for connection or request admission, 429 for SSE subscription exhaustion.
  A command or QUERY body exceeding the configured byte limit returns 413, counted while
  streaming even when `Content-Length` is absent.

**Why it matters**: a client that handles 429 and 503 with backoff works on both; a client
that does *not* will appear to work against C# and then fail under load against a JVM host.

### Unsupported HTTP methods

- **C#**: under ASP.NET Core hosting, a matched route with an unsupported method returns
  405 with an `Allow` header, from ASP.NET Core routing. Under Arc's self-hosted
  `HttpListener` host, routes are keyed by method and path together, so an unsupported
  method falls through to 404.
- **JVM**: an unsupported method returns 405 with an `Allow` header.

**Why it matters**: do not use 405 versus 404 to probe whether a route exists.

### Tenant resolution strategy and enforcement

- **C#**: one resolver strategy is selected by configuration - header, query parameter,
  subdomain, claim, fixed, or development. There is no option to require a resolved
  tenant, so an unresolved tenant does not itself fail the request.
- **JVM**: a configured resolver *chain* runs once per HTTP request, SSE subscription, or
  WebSocket handshake, over headers, query parameters, the server host, and captured
  principal claims. With `tenancy.required=true` an unresolved request returns 400, or
  fails the WebSocket handshake. An authenticated caller with tenant-membership claims
  who selects another tenant receives a generic 403.

**Why it matters**: a client cannot infer tenancy enforcement from the protocol. Send the
tenant header explicitly rather than relying on host-side inference.

### Authentication handler chain hosting

- **C#**: Arc's own `IAuthenticationHandler` chain is invoked by the self-hosted
  `HttpListener` host. Under ASP.NET Core hosting, authentication is ASP.NET Core's own,
  and Arc's anonymous metadata maps to ASP.NET Core's allow-anonymous metadata.
- **JVM**: Arc's authentication filter runs the registered `AuthenticationHandler` and
  `AsyncAuthenticationHandler` beans for Arc routes, ordered ahead of the rest of the Arc
  pipeline and behind Spring Security's chain.

**Why it matters**: the 401 a client sees is the same either way, but *where* credentials
are validated - and therefore which configuration governs them - is not.

### Correlation identifier edge cases

- **C#**: the all-zero UUID is treated as not-set and replaced with a generated
  identifier. Correlation is established by the Arc endpoint handlers, so it covers Arc
  routes.
- **JVM**: a servlet filter registered on `/*` establishes one identifier for every
  request reaching the host, whether Arc owns the route or not, ordered ahead of Spring
  Security and Arc authentication. The all-zero UUID parses as a UUID and is reused.

**Why it matters**: do not send the zero UUID and expect it back. The identifier you
observe on a C# host may differ from the one you sent in exactly that case.

### Development user and tenant discovery

- **C#**: `/.cratis/users` and `/.cratis/tenants` concatenate the results of every
  registered provider in discovery order, without deduplication.
- **JVM**: results are aggregated from ordered provider beans and deduplicated by
  identifier - the first item for each identifier wins.

**Why it matters**: a client rendering a development user picker may see duplicate
identifiers on a C# host if two providers contribute the same one.

## Related

- [C# backend documentation](/arc/backend/csharp/)
- [Kotlin and Java backend documentation](/arc/backend/kotlin/)
- [JVM conformance notes](/arc/backend/kotlin/reference/http-contract/)
- [Glossary](glossary.md)
- [Understanding the proxy boundary](understanding-the-proxy-boundary.mdx)
- [Understanding identity and access](understanding-identity-and-access.mdx)
