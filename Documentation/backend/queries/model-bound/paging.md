# Paging

When a static query method on a `[ReadModel]` returns `IQueryable<T>`, the query pipeline automatically applies server-side paging and sorting. You write a simple method that returns a queryable, and the framework handles the rest.

## Why IQueryable matters

The key to automatic paging is returning `IQueryable<T>` instead of `IEnumerable<T>` or `List<T>`. When the pipeline sees an `IQueryable`, it appends `.Skip()` and `.Take()` *before* the database executes the query — so only the requested page of data travels over the wire.

If you return a materialized collection, all rows are fetched first and paging cannot be applied at the database level.

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    // ✅ Returns IQueryable — paging and sorting are applied automatically
    public static IQueryable<DebitAccount> AllAccounts(IMongoCollection<DebitAccount> collection)
        => collection.AsQueryable();
}
```

## How it works

When a client sends paging parameters in the query string, the `QueryableQueryRenderer` intercepts the `IQueryable` result and:

1. Counts the total number of matching items
2. Applies sorting based on `sortby` and `sortDirection`
3. Applies `.Skip(page * pageSize)` and `.Take(pageSize)`
4. Returns the page of data wrapped in a `QueryResult` with a `PagingInfo` containing `page`, `size`, `totalItems`, and `totalPages`

The client controls paging with these query string parameters:

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `page` | `int` | Zero-based page number |
| `pageSize` | `int` | Number of items per page |
| `sortby` | `string` | Field name to sort by |
| `sortDirection` | `asc` or `desc` | Sort direction |

### Example requests

```http
GET /api/debitaccount/allaccounts?page=0&pageSize=25
GET /api/debitaccount/allaccounts?page=1&pageSize=10&sortby=name&sortDirection=asc
```

When no paging parameters are provided, the full result set is returned without paging.

## Complete example with filtering

Paging works alongside query arguments. The pipeline applies paging *after* your method returns the filtered `IQueryable`:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IQueryable<DebitAccount> AllAccounts(IMongoCollection<DebitAccount> collection)
        => collection.AsQueryable();

    public static IQueryable<DebitAccount> AccountsByOwner(
        CustomerId ownerId,
        IMongoCollection<DebitAccount> collection)
        => collection.AsQueryable().Where(a => a.Owner == ownerId);
}
```

Both query methods support paging automatically because they return `IQueryable<T>`.

## Return type comparison

| Return type | Paging | Sorting | DB-level optimization |
| ----------- | ------ | ------- | --------------------- |
| `IQueryable<T>` | ✅ Automatic | ✅ Automatic | ✅ Skip/Take pushed to DB |
| `IEnumerable<T>` | ❌ | ❌ | ❌ All rows loaded |
| `List<T>` | ❌ | ❌ | ❌ All rows loaded |
| `T[]` | ❌ | ❌ | ❌ All rows loaded |

## Observable queries with paging

Observable queries that return `ISubject<IQueryable<T>>` also support automatic paging. The pipeline applies paging to each update pushed through the observable:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IQueryable<DebitAccount>> ObserveAllAccounts(
        IMongoCollection<DebitAccount> collection)
        => collection.ObserveAsQueryable();
}
```

## Frontend integration

The generated TypeScript proxy includes a `useWithPaging` method when the backend query supports paging. See [Queries](../../../frontend/react/queries/index.md) for details on using paging in React components.

```tsx
const [result, perform, setSorting, setPage, setPageSize] = AllAccounts.useWithPaging(25);

// Navigate pages
await setPage(result.paging.page + 1);

// Change page size
await setPageSize(50);

// Access paging metadata
const { page, size, totalItems, totalPages } = result.paging;
```
