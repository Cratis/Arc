# Query Scope

If you want to know whether any queries or observable queries are currently in-flight — typically to show a
loading indicator or disable parts of the UI — the query scope provides a React context for this.

Using a toolbar as an example: at the top level you can wrap everything in the `<QueryScope>` component.
This establishes a React context for that part of the hierarchy and tracks the performing state of any
queries or observable queries used by any descendant component.

```typescript
import { QueryScope } from '@cratis/arc.react/queries';

export const MyComposition = () => {
    const [isPerforming, setIsPerforming] = useState(false);

    return (
        <QueryScope setIsPerforming={setIsPerforming}>
            <Toolbar isLoading={isPerforming} />
            <DataPanel />
            <SidePanel />
        </QueryScope>
    );
};
```

## How Performing State Is Tracked

- **Regular queries** — `isPerforming` is `true` while the HTTP request is in-flight and becomes `false`
  when the response is received.
- **Observable queries** — `isPerforming` is `true` from the moment a subscription is opened until the
  first result is pushed from the server.

## Hierarchical Scopes

Query scopes can be nested to create a hierarchy. When you add a `<QueryScope>` inside another one, the
inner scope automatically registers itself with the nearest outer scope. Outer scopes aggregate state
across all inner scopes, giving you both local and global views of the performing state.

```typescript
export const MyPage = () => {
    const [isPerforming, setIsPerforming] = useState(false);

    return (
        <QueryScope setIsPerforming={setIsPerforming}>
            {/* PageToolbar sees aggregate state for the whole page */}
            <PageToolbar isLoading={isPerforming} />
            <Section1>
                <QueryScope>
                    {/* SectionLoader only sees state for queries in Section1 */}
                    <SectionLoader />
                    <SectionContent />
                </QueryScope>
            </Section1>
        </QueryScope>
    );
};
```

In this example:
- Queries inside `<Section1>` bind to the inner `<QueryScope>`.
- The outer `<QueryScope>` reports `isPerforming` as `true` whenever any inner scope has an in-flight query.

## Query Scope API

| Name | Type | Description |
|------|------|-------------|
| `isPerforming` | `boolean` | Whether any queries in this scope or child scopes are currently in-flight. |
| `parent` | `IQueryScope \| undefined` | The parent scope, if this scope is nested. |
| `addChildScope(scope)` | `void` | Register a child scope for aggregate state propagation (done automatically). |
| `notifyPerformingStarted()` | `void` | Signal that a query has started performing (called automatically by query hooks). |
| `notifyPerformingCompleted()` | `void` | Signal that a query has finished performing (called automatically by query hooks). |

## Using Query Scope in React

To read the performing state imperatively from inside the scope, use the `useQueryScope` hook:

```typescript
import { useQueryScope } from '@cratis/arc.react/queries';

export const Toolbar = () => {
    const queryScope = useQueryScope();

    return (
        <div>
            {queryScope.isPerforming && <Spinner />}
        </div>
    );
};
```

You can also consume the context directly:

```typescript
import { QueryScopeContext } from '@cratis/arc.react/queries';

export const Toolbar = () => {
    return (
        <QueryScopeContext.Consumer>
            {scope => (
                <div>
                    {scope.isPerforming && <Spinner />}
                </div>
            )}
        </QueryScopeContext.Consumer>
    );
};
```

## Using Query Scope in ViewModels

The query scope can be injected into ViewModels through dependency injection. The ViewModel automatically
receives the closest query scope in the component hierarchy.

```typescript
import { IQueryScope } from '@cratis/arc.react/queries';
import { injectable } from 'tsyringe';

@injectable()
export class MyViewModel {
    constructor(private readonly _queryScope: IQueryScope) {
    }

    get isLoading(): boolean {
        return this._queryScope.isPerforming;
    }
}
```

The ViewModel then exposes `isLoading` as an observable property (MobX makes this automatic via
`withViewModel`), which your component can bind to:

```typescript
export const MyPage = withViewModel(MyViewModel, ({ viewModel }) => {
    return (
        <div>
            {viewModel.isLoading && <Spinner />}
            <DataTable />
        </div>
    );
});
```

## Automatic Query Tracking

Any query hook used inside a `<QueryScope>` is automatically tracked — there is nothing extra to wire up:

```typescript
export const DataPanel = () => {
    const [result] = AllAccounts.use();

    return (
        <DataTable value={result.data} />
    );
};
```

When `AllAccounts.use()` fires an HTTP request, `isPerforming` on the nearest enclosing `<QueryScope>`
becomes `true`. It returns to `false` once the response arrives.

Observable queries work the same way:

```typescript
export const LiveFeed = () => {
    const [feed] = FeedItems.use();

    return (
        <ul>
            {feed.data.map(item => <li key={item.id}>{item.message}</li>)}
        </ul>
    );
};
```

`isPerforming` is `true` from the moment the subscription is opened until the first result is received.

## See Also

- [Queries Overview](./index.md)
- [React Usage](./react-usage.md)
- [Command Scope](../commands/scope.md) — Equivalent feature for tracking command execution state.
