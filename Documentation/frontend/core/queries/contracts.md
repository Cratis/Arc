# Query Contracts

Core query support in `@cratis/arc` is built on typed query classes and generated proxies.

## IQuery

The base query interface holds cross-cutting query concerns:

```typescript
interface IQuery {
    sorting: Sorting;
    paging: Paging;
}
```

## IQueryFor

`IQueryFor` adds route, typed parameters, default value, and execution:

```typescript
interface IQueryFor<TDataType, TParameters = object> extends IQuery {
    readonly route: string;
    defaultValue: TDataType;
    parameters: TParameters | undefined;
    perform(args?: TParameters): Promise<QueryResult<TDataType>>;
}
```

## Built-in Concerns

Query contracts include:

- Typed parameters and responses
- Sorting and paging metadata
- Default values for predictable initialization
- Execution through `perform()`

## See Also

- [Validation And Behavior](./validation-and-behavior.md)
- [React Queries](../../react/queries/index.md)
