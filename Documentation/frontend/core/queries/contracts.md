---
title: Query Contracts
description: Reference for the IQuery, IQueryFor, and observable query contracts in @cratis/arc that generated query proxies implement.
---

Core query support in `@cratis/arc` is built on typed query classes and generated proxies.

## IQuery

The base query interface holds cross-cutting query concerns:

```typescript
interface IQuery extends ICanBeConfigured {
    setHttpMethod(method: QueryHttpMethod): void;
    get sorting(): Sorting;
    set sorting(value: Sorting);
    get paging(): Paging;
    set paging(value: Paging);
}
```

`setHttpMethod` overrides the global `Globals.queryHttpMethod` for one query instance. `QueryHttpMethod` is `Get` (the default), `Query` (RFC QUERY with a JSON body), or `Auto` (try QUERY, fall back to GET for the rest of the session). See [using the HTTP QUERY method](../../../backend/csharp/queries/using-the-http-query-method.md) before sending arguments that must stay out of URLs.

## IQueryFor

`IQueryFor` adds route, typed parameters, default value, and execution:

```typescript
interface IQueryFor<TDataType, TParameters = object>
    extends IQuery, IHaveParameters {
    readonly route: string;
    readonly requiredRequestParameters: string[];
    readonly defaultValue: TDataType;
    readonly roles: string[];
    get parameters(): TParameters | undefined;
    set parameters(value: TParameters);
    perform(args?: TParameters): Promise<QueryResult<TDataType>>;
}
```

## Built-in Concerns

Query contracts include:

- Typed parameters and responses
- Required route/request parameter metadata
- Sorting and paging metadata
- Required roles for UI decisions
- Default values for predictable initialization
- Execution through `perform()`

## See Also

- [Validation And Behavior](./validation-and-behavior.md)
- [React Queries](../../react/queries/index.md)
