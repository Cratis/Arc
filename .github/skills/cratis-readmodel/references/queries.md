# Queries — Reference

## Query endpoint patterns

### Collection query (with automatic paging)

Return `IQueryable<T>` to enable automatic server-side paging and sorting. The query pipeline appends `.Skip()` and `.Take()` before the database query executes, so only the requested page of data is loaded.

```csharp
[HttpGet]
public IQueryable<AccountSummary> AllAccounts()
    => collection.AsQueryable();
```

Returning `IEnumerable<T>`, `List<T>`, or `T[]` **disables** automatic paging — all rows are loaded into memory.

### Single item query

```csharp
[HttpGet("{id}")]
public AccountSummary? GetAccount(Guid id)
    => collection.Find(a => a.Id == id).FirstOrDefault();
```

### Filtered query (with proxy parameter)

```csharp
[HttpGet("search")]
public IQueryable<AccountSummary> Search([FromQuery] string? filter)
    => collection.AsQueryable().Where(a => a.Name.StartsWith(filter ?? string.Empty));
```

The `[FromQuery]` parameter is included in the generated TypeScript proxy.

### Observable (real-time) query

Return `ISubject<T>` to push data to clients over WebSocket:

```csharp
[HttpGet("live")]
public ISubject<IEnumerable<AccountSummary>> AllAccountsLive()
{
    var observable = new ClientObservable<IEnumerable<AccountSummary>>();
    observable.OnNext(collection.Find(_ => true).ToList());

    var changeStream = collection.Watch();
    observable.ClientDisconnected += () => changeStream.Dispose();
    Task.Run(async () =>
    {
        await foreach (var _ in changeStream.ToAsyncEnumerable())
            observable.OnNext(collection.Find(_ => true).ToList());
    });

    return observable;
}
```

The proxy generator produces an `ObservableQuery` TypeScript class for `ISubject<T>` return types. The React hook `useObservableQuery()` is used automatically.

---

## QueryResult shape (frontend)

```ts
interface QueryResultWithState<T> {
    data: T;
    isSuccess: boolean;
    isAuthorized: boolean;
    isValid: boolean;
    validationResults: ValidationResult[];
    hasExceptions: boolean;
    exceptionMessages: string[];
    paging: { page: number; pageSize: number; totalItems: number; totalPages: number };

    // React-specific:
    hasData: boolean;       // non-null and non-empty
    isPerforming: boolean;  // request in flight
}
```

---

## React usage

```tsx
// Standard query — returns [result, requery]
const [accounts, refresh] = AllAccounts.use();

// With parameters
const [results] = Search.use({ filter: searchText });

// Observable query — returns [result] only (no manual refresh)
const [liveAccounts] = AllAccountsLive.use();
```

### Paging in React

When the backend returns `IQueryable<T>`, the generated proxy includes `useWithPaging` and `useSuspenseWithPaging` methods. Pass the page size to enable paging:

```tsx
// Standard query with paging
const [result, perform, setSorting, setPage, setPageSize] = AllAccounts.useWithPaging(25);

// Navigate pages
await setPage(result.paging.page + 1);

// Change page size
await setPageSize(50);

// Access paging metadata
const { page, size, totalItems, totalPages } = result.paging;
```

```tsx
// Suspense query with paging
const [result, perform, setSorting, setPage, setPageSize] =
    AllAccounts.useSuspenseWithPaging(25);
```

```tsx
// Observable query with paging
const [result, setSorting, setPage, setPageSize] = AllAccountsLive.useWithPaging(25);
```

The paging hooks automatically send `page` and `pageSize` query string parameters to the backend. When the user calls `setPage` or `setPageSize`, the hook re-runs the query with the updated parameters.

For full page layouts with tables and menu actions, see the `cratis-react-page` skill.

---

## Naming conventions

The **method name** on the controller becomes the TypeScript proxy class name. Make it descriptive.

| ✅ Good | ❌ Avoid |
| ------- | ------- |
| `AllAccounts` | `Get`, `GetAll`, `List` |
| `AccountsByOwner` | `Query`, `Fetch` |
| `AllAccountsLive` | `Observable`, `Live` |
