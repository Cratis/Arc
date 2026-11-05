---
title: Use HTTP QUERY for Cratis Arc queries
description: Send Cratis Arc query arguments in a JSON request body, configure generated clients, and choose whether unsupported QUERY requests may fall back to GET.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

HTTP `QUERY` lets a Cratis Arc model-bound query read its arguments from a JSON body instead of a URL. Use it for long search text or inputs you do not want in a query string. It returns the same query result contract as GET and remains a read, not a command.

If an argument must stay out of URLs, choose explicit `QueryHttpMethod.Query`. `Auto` and the length-based resolver can retry with GET, putting the arguments in the URL. A body can still be logged, so configure logging redaction and use HTTPS. Keep authentication credentials in your normal authorization headers or cookies, not in query arguments.

## Prerequisites

Start with a generated model-bound endpoint with QUERY enabled, and check that your proxy or gateway accepts the verb. Arc does not automatically add QUERY handling to MVC GET actions. The body reader supports [scalar arguments and supported collections](model-bound/query-arguments.md), not arbitrary nested DTO deserialization.

## Declare the transport in CSharp

This product search runs in an existing Arc host with a registered MongoDB collection. The attribute tells its generated client to use QUERY by default:

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

You can put the attribute on the read-model type to set a default for its queries; a method-level choice takes precedence. This is a client default, not a server-side ban on GET. Both generated verbs remain available while QUERY is enabled. Before exposing private data, add [input validation](validation.md) and [query authorization](model-bound/authorization.md). Changing the verb does not add access checks.

## Opt in from the client

To choose QUERY application-wide, set the global default at startup. It applies when a query has no explicit override and no global resolver selects another method:

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

When URL length is the concern, the exported `lengthBasedQueryHttpMethod({ threshold: 2000 })` resolver uses GET for short URLs and Auto for longer ones. Configure it at application startup:

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

`paging` and `sorting` are optional; page numbers start at zero. Arguments bind by parameter name. Supported collection parameters can receive JSON arrays, while ordinary scalar parameters use scalar conversion. Putting an object in `arguments` does not make an arbitrary DTO parameter bindable. Model-bound readers do not bind route placeholders, so use the explicit path and named arguments shown here.

## Call it with cURL

With the example endpoint running, send the envelope as JSON. Replace the origin and supply the application's normal credentials if the query is protected:

```bash
curl --include --request QUERY 'https://localhost:5001/api/products/search' \
  --header 'Content-Type: application/json' \
  --data '{"arguments":{"searchText":"widgets"},"paging":{"page":0,"pageSize":20}}'
```

Read the response using the same [QueryResult fields](query-pipeline.md#query-result-metadata) as GET. An invalid argument or validation verdict returns 400; a query authorization denial returns 403. With explicit QUERY, a 405 or 501 is a failure to diagnose in the server or network path, not a reason for the client to retry with GET.

Generated QUERY responses set `Cache-Control: no-store`. This controls response caching; it does not prevent intermediaries or application logs from recording the request body.

## Cross-origin and server configuration

For browser calls, configure your actual allowed origins, headers/credentials, and methods to include QUERY; preflight must succeed. GET calls with authorization headers or cross-origin credentials may need CORS configuration too. Do not use a blanket wildcard policy as a substitute for an application's access policy.

The server option `ArcOptions.GeneratedApis.EnableQueryHttpMethod` controls registration of generated QUERY endpoints and defaults to enabled. Set it to false to keep only GET. That setting does not change MVC verb declarations.

See [configuration](../configuration/index.md) for host options and [cURL observable workflows](using-observable-queries-with-curl.md) for snapshot versus streaming reads.
