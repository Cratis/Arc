# Paging

When a controller action returns `IQueryable<T>`, the query pipeline automatically applies server-side paging and sorting. This means you write a simple method that returns a queryable, and the framework handles the rest.

## Why IQueryable matters

The key to automatic paging is returning `IQueryable<T>` instead of `IEnumerable<T>` or `List<T>`. When the pipeline sees an `IQueryable`, it can append `.Skip()` and `.Take()` *before* the database executes the query — so only the requested page of data travels over the wire.

If you return a materialized collection like `List<T>`, the pipeline has no way to apply paging at the database level. All rows are fetched first, defeating the purpose.

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    readonly IMongoCollection<DebitAccount> _collection;

    public Accounts(IMongoCollection<DebitAccount> collection) => _collection = collection;

    // ✅ Returns IQueryable — paging and sorting are applied automatically
    [HttpGet]
    public IQueryable<DebitAccount> AllAccounts() => _collection.AsQueryable();
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
GET /api/accounts?page=0&pageSize=25
GET /api/accounts?page=2&pageSize=10&sortby=name&sortDirection=asc
```

When no paging parameters are provided, the full result set is returned without paging.

## Complete example with filtering

Paging works alongside query arguments. The pipeline applies paging *after* your method returns the filtered `IQueryable`:

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    readonly IMongoCollection<DebitAccount> _collection;

    public Accounts(IMongoCollection<DebitAccount> collection) => _collection = collection;

    [HttpGet]
    public IQueryable<DebitAccount> AllAccounts() => _collection.AsQueryable();

    [HttpGet("by-owner/{ownerId}")]
    public IQueryable<DebitAccount> AccountsByOwner(CustomerId ownerId)
        => _collection.AsQueryable().Where(a => a.Owner == ownerId);
}
```

Both endpoints support paging automatically because they return `IQueryable<T>`.

## Return type comparison

| Return type | Paging | Sorting | DB-level optimization |
| ----------- | ------ | ------- | --------------------- |
| `IQueryable<T>` | ✅ Automatic | ✅ Automatic | ✅ Skip/Take pushed to DB |
| `IEnumerable<T>` | ❌ | ❌ | ❌ All rows loaded |
| `List<T>` | ❌ | ❌ | ❌ All rows loaded |
| `T[]` | ❌ | ❌ | ❌ All rows loaded |

## Manual paging with IQueryContextManager

If you need full control over how paging is applied — for example, when using the MongoDB driver directly instead of LINQ — inject `IQueryContextManager` and read the paging context manually:

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    readonly IMongoCollection<DebitAccount> _collection;
    readonly IQueryContextManager _queryContextManager;

    public Accounts(
        IMongoCollection<DebitAccount> collection,
        IQueryContextManager queryContextManager)
    {
        _collection = collection;
        _queryContextManager = queryContextManager;
    }

    [HttpGet("manual")]
    public QueryResult ManualPaging()
    {
        var context = _queryContextManager.Current;
        var query = _collection.Find(_ => true);

        if (context.Sorting != Sorting.None)
        {
            query = context.Sorting.Direction == SortDirection.Ascending
                ? query.SortBy(context.Sorting.Field)
                : query.SortByDescending(context.Sorting.Field);
        }

        var totalItems = (int)query.CountDocuments();

        if (context.Paging.IsPaged)
        {
            query = query.Skip(context.Paging.Skip).Limit(context.Paging.Size);
        }

        var data = query.ToList();

        return new QueryResult
        {
            Data = data,
            Paging = new PagingInfo(context.Paging.Page, context.Paging.Size, totalItems)
        };
    }
}
```

> Manual paging is rarely needed. Prefer returning `IQueryable<T>` and letting the pipeline handle it.
