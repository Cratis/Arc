# Query Proxy Generation

The proxy generator creates TypeScript query classes that provide type-safe query execution with React hook integration.

## Supported Approaches

Queries can be implemented using two approaches, both of which are supported by the proxy generator:

- **Controller-based**: Queries in ASP.NET Core controllers using `[HttpGet]` attributes
- **Model-bound**: Simplified approach where a type represents the query directly

For detailed information on implementing queries, see the [Queries documentation](../queries/index.md).

## Query Types

The generator supports three types of queries:

| Type | Description | Use Case |
|------|-------------|----------|
| **Single model** | Returns a single object | Fetching a specific entity by ID |
| **Enumerable** | Returns an array of objects | Listing or searching entities |
| **Observable** | Real-time updates via WebSockets | Live data feeds, dashboards |

## How Queries are Discovered

### Controller-based Queries

The generator discovers controller-based queries by looking for:

- Methods marked with `[HttpGet]`
- Return types that indicate the query type:
  - Single object → Single model query
  - `IEnumerable<T>`, `List<T>`, etc. → Enumerable query
  - `IObservable<T>` → Observable query

See [Controller-based Queries](../queries/controller-based/index.md) for implementation details.

### Model-bound Queries

The generator discovers model-bound queries by finding types that:

- Are decorated with the `[ReadModel]` attribute
- Have static methods that constitute query operations

Each static method on the read model becomes a separate query. The method name becomes the query name, and method parameters (excluding injected dependencies) become the query parameters in the generated TypeScript.

See [Model-bound Queries](../queries/model-bound/index.md) for implementation details.

## Generated Query Structure

Generated query classes provide:

- Type-safe parameter handling through an interface
- React hooks for integration (`useQuery` or `useObservableQuery`)
- The proper route based on the configuration

## Generated Artifacts

For each query, the generator creates:

1. **Parameters Interface**: An `IQueryNameParameters` interface (if the query has parameters)
2. **Query Class**: Extends `QueryFor<TResult>` or `ObservableQueryFor<TResult>`
3. **Route**: The HTTP route derived from the controller route or model-bound configuration

## Query Base Classes

Depending on the query type, the generated class extends:

| Query Type | Base Class |
|------------|------------|
| Single model | `QueryFor<TModel>` |
| Enumerable | `QueryFor<TModel[]>` |
| Observable | `ObservableQueryFor<TModel>` |

## Excluding Queries from Generation

To exclude specific controller-based queries from proxy generation, mark them with the `[AspNetResult]` attribute. This is useful when you want to handle the response manually or when the query returns a non-standard result.

## Route Configuration

The generated route is affected by the `CratisProxiesSkipQueryNameInRoute` configuration option:

- When `false` (default): The query type name is included in the route
- When `true`: The query type name is excluded from the route

See [Configuration](configuration.md) for more details on route configuration options.

## Frontend Usage

The generated query proxies integrate with React through the `use()` static method, which returns a query result object containing:

- `data`: The query result (typed according to the return type)
- `isLoading`: Loading state indicator
- `error`: Any error that occurred
- Additional state depending on query type

For observable queries, the result also includes connection state information.

For frontend usage patterns, see the [@cratis/arc documentation](https://www.npmjs.com/package/@cratis/arc).
