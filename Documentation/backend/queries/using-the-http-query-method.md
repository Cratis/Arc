---
title: Use the HTTP QUERY method
description: Move scalar query arguments into a request body without silently falling back to URLs.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Long search text can exceed practical URL limits. Arc's generated model-bound query endpoints support HTTP `QUERY` with an arguments body as an alternative to GET. It returns the same query result contract; it does not turn a read into a command.

> [!WARNING]
> If an argument must stay out of URLs, choose **explicit `QueryHttpMethod.Query`**, not `Auto` or the length-based fallback resolver. Auto can retry with GET and put those arguments in the URL. Bodies can also be logged: configure logging/redaction and use HTTPS. Authentication credentials belong in your normal authorization headers or cookies, not query arguments.

## Prerequisites

Use a generated **model-bound** endpoint with QUERY enabled and a network path that accepts the verb. Arc does not automatically add QUERY handling to every MVC GET action. Built-in QUERY binding remains [scalar argument conversion](model-bound/query-arguments.md), not arbitrary nested DTO/array deserialization.

## Declare the transport in CSharp

Complete type example for an existing Arc host with a registered MongoDB collection:

```csharp
using System.Linq;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Catalog;

[ReadModel]
public record Product(string Id, string Name)
{
    [Path("/api/products/search")]
    [QueryHttpMethod(QueryHttpMethod.Query)]
    public static IQueryable<Product> Search(string searchText, IMongoCollection<Product> collection) =>
        collection.AsQueryable().Where(product => product.Name.Contains(searchText));
}
```

The generated proxy defaults to QUERY. The attribute can also be put on the read-model type, with method-level choice taking precedence. It sets a **client default**, not a server-side ban on GET; both generated verbs remain available. Add [input validation](validation.md) and authorization appropriate to the data before deployment.

## Opt in from the client

Application-startup fragment for all generated queries without an explicit per-query override:

```typescript
import { Globals } from '@cratis/arc';
import { QueryHttpMethod } from '@cratis/arc/queries';

Globals.queryHttpMethod = QueryHttpMethod.Query;
```

For a single generated query instance, call `query.setHttpMethod(QueryHttpMethod.Query)` before performing it. Here `query` means an instance of your generated proxy, not a framework singleton. An explicit per-query setting takes precedence over a global resolver and global default.

## Let the framework choose with Auto

Use `QueryHttpMethod.Auto` only when the arguments are safe in either transport. It tries QUERY, then retries GET for a 405/501 response or a non-abort fetch/network failure (including CORS failure). It remembers a GET downgrade per backend origin plus API base path for the session. Other HTTP error statuses are not a fallback signal.

`resetQueryHttpMethodResolution()` from `@cratis/arc/queries` clears learned transport choices. Explicit QUERY never falls back to GET.

## Choose the transport per query

The exported `lengthBasedQueryHttpMethod({ threshold: 2000 })` resolver uses GET for short URLs and Auto for longer ones. It is a compatibility/length heuristic, **not a privacy rule**.

Application-startup fragment:

```typescript
import { Globals } from '@cratis/arc';
import { lengthBasedQueryHttpMethod } from '@cratis/arc/queries';

Globals.queryHttpMethodResolver = lengthBasedQueryHttpMethod({ threshold: 2000 });
```

If URLs are prohibited for particular inputs, give those proxies an explicit QUERY setting rather than relying on length.

## The request body

For the declared product query, this is the complete request envelope:

```json
{
    "arguments": { "searchText": "widgets" },
    "paging": { "page": 0, "pageSize": 20 },
    "sorting": { "field": "name", "direction": "asc" }
}
```

`paging` and `sorting` are optional. Each argument is converted through the scalar converter; putting an object or array inside `arguments` does not make it bind as a complex parameter. Model-bound readers do not bind route placeholders; use the explicit path and arguments shown here.

## Call it with cURL

Runnable against a host exposing the example path, with its actual origin and normal credentials supplied:

```bash
curl --include --request QUERY 'https://localhost:5001/api/products/search' \
  --header 'Content-Type: application/json' \
  --data '{"arguments":{"searchText":"widgets"},"paging":{"page":0,"pageSize":20}}'
```

The response has the same [QueryResult fields](query-pipeline.md#query-result-metadata) as GET. Generated QUERY responses set `Cache-Control: no-store`; that is not a guarantee that intermediaries or application logs never record the request body.

## Cross-origin and server configuration

For browser calls, configure your actual allowed origins, headers/credentials, and methods to include QUERY; preflight must succeed. GET calls with authorization headers or cross-origin credentials may need CORS configuration too. Do not use a blanket wildcard policy as a substitute for an application's access policy.

The server option `ArcOptions.GeneratedApis.EnableQueryHttpMethod` controls registration of generated QUERY endpoints and defaults to enabled. Set it to false to keep only GET. That setting does not change MVC verb declarations.

See [configuration](../configuration/index.md) for host options and [cURL observable workflows](using-observable-queries-with-curl.md) for snapshot versus streaming reads.
