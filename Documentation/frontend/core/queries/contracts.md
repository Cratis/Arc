# Query Contracts

Core query support in `@cratis/arc` is built on typed query classes and generated proxies.

## IQuery

The base query interface holds cross-cutting query concerns:

```typescript
interface IQuery extends ICanBeConfigured {
    get sorting(): Sorting;
    set sorting(value: Sorting);
    get paging(): Paging;
    set paging(value: Paging);
}
```

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
