# Use the HTTP QUERY method

By default Arc exposes every query over `GET`, with arguments in the URL query string. That is perfect until a query needs to carry **a lot** of arguments, or **sensitive** ones — a long free-text search, a nested filter, an access token. URLs have length limits, and everything in a URL leaks into server and proxy access logs, browser history, and `Referer` headers.

The HTTP `QUERY` method ([RFC 10008](https://www.rfc-editor.org/info/rfc10008/)) solves this: it is a safe, idempotent request — like `GET` — but it carries its arguments in a JSON **request body** instead of the URL.

Arc registers **both** verbs for every query endpoint, so `QUERY` is available whenever you want it. `GET` stays the default; you opt into `QUERY` on the client.

Use this when you want to:

- send query arguments that are too large for a URL
- keep sensitive arguments out of URLs, logs, and history
- move a growing filter object into a structured body

## Prerequisites

- Your application is running and exposes at least one query.
- You are calling the query from the generated TypeScript proxy, or from a plain HTTP tool.

## Opt in from the client

The generated proxies default to `GET`. Switch the default for every query by setting `Globals.queryHttpMethod`:

```typescript
import { Globals } from '@cratis/arc';
import { QueryHttpMethod } from '@cratis/arc/queries';

Globals.queryHttpMethod = QueryHttpMethod.Query;
```

To switch a single query without changing the global default, call `setHttpMethod` on the query instance:

```typescript
query.setHttpMethod(QueryHttpMethod.Query);
```

Everything else — arguments, paging, sorting, the shape of the result — stays exactly the same. Only the transport changes.

## Let the framework choose with `Auto`

If you're not sure every deployment's network path supports `QUERY` (some corporate proxies, WAFs and gateways don't recognize it yet), use `QueryHttpMethod.Auto`:

```typescript
import { Globals } from '@cratis/arc';
import { QueryHttpMethod } from '@cratis/arc/queries';

Globals.queryHttpMethod = QueryHttpMethod.Auto;
```

Arc sends `QUERY` on the first query. If the server or an intermediary rejects the verb — a `405`/`501` response, or a network/CORS error from `fetch` — it transparently retries the query with `GET` and remembers the outcome **per backend** (origin + API base path) for the rest of the session, so subsequent queries go straight to the working transport and one backend's lack of support never downgrades another. This also covers the cross-origin case: if the CORS policy doesn't allow `QUERY`, `Auto` simply settles on `GET`.

`Auto` only falls back on **transport-level** failures — an application error (a normal failed `QueryResult`) is never retried as `GET`. Call `resetQueryHttpMethodResolution()` (from `@cratis/arc/queries`) to make the next `Auto` query probe again, for example after a network change.

## The request body

With `QUERY`, route parameters stay in the path (they identify the resource); every other argument, plus paging and sorting, moves into a JSON body:

```json
{
  "arguments": { "searchText": "a very long search expression", "status": "active" },
  "paging": { "page": 0, "pageSize": 20 },
  "sorting": { "field": "name", "direction": "asc" }
}
```

`paging` and `sorting` are optional — omit them for an unpaged, unsorted query.

## Call it with cURL

You can exercise a `QUERY` endpoint with any HTTP client. The `Content-Type` must be `application/json` — Arc rejects a request without it.

```bash
curl -X QUERY "https://localhost:5001/api/orders/search" \
  -H "Content-Type: application/json" \
  -d '{ "arguments": { "searchText": "widgets" }, "paging": { "page": 0, "pageSize": 20 } }'
```

The response is the same `QueryResult` JSON you get from the `GET` form of the query.

## Cross-origin calls need CORS to allow QUERY

`QUERY` is not a [simple method](https://developer.mozilla.org/docs/Web/HTTP/CORS#simple_requests), so a browser sends a preflight `OPTIONS` request first. If you call queries from another origin, add `QUERY` to your allowed methods:

```csharp
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithMethods("GET", "POST", "QUERY").AllowAnyHeader().AllowAnyOrigin()));
```

The `GET` default needs no CORS change, so this only matters once you opt into `QUERY`.

## Turn it off on the server

`QUERY` endpoints are registered by default. To restrict query endpoints to `GET` only — for example behind infrastructure that rejects unknown HTTP verbs — disable it:

```csharp
builder.Services.Configure<ArcOptions>(options =>
    options.GeneratedApis.EnableQueryHttpMethod = false);
```

`GET` is unaffected.

## See also

- [Use Observable Queries with cURL](./using-observable-queries-with-curl.md)
- [Configuration](../configuration/index.md)
- [Query Pipeline](./query-pipeline.md)
