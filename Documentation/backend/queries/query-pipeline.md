# Query Pipeline

The query pipeline provides automatic handling of sorting, paging, and advanced query processing
through query renderers. This allows you to write simple query methods while getting advanced
functionality for free.

## Automatic Sorting and Paging

Arc automatically processes query string parameters for sorting and paging:

- `sortby` - Field to sort by
- `sortDirection` - `asc` or `desc`
- `page` - Page number (0-based)
- `pageSize` - Number of items per page

### Query String Examples

```http
GET /api/accounts?sortby=name&sortDirection=asc&page=0&pageSize=50
GET /api/accounts?sortby=balance&sortDirection=desc&page=2&pageSize=25
```

### In Controller Actions

Your controller actions automatically receive sorting and paging context:

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    readonly IMongoCollection<DebitAccount> _collection;

    public Accounts(IMongoCollection<DebitAccount> collection) => _collection = collection;

    [HttpGet]
    public IQueryable<DebitAccount> GetAccounts()
    {
        // Return IQueryable to enable automatic sorting and paging
        return _collection.AsQueryable();
    }
}
```

When you return `IQueryable<T>`, the query pipeline automatically:

1. Applies sorting based on the `sortby` and `sortDirection` parameters
2. Applies paging based on the `page` and `pageSize` parameters
3. Wraps the result in a `QueryResult<T>` with paging metadata

## Query Context

The current query context is available through `IQueryContextManager`:

```csharp
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
    public QueryResult<IEnumerable<DebitAccount>> GetAccountsManual()
    {
        var context = _queryContextManager.Current;
        var query = _collection.Find(_ => true);

        // Manual sorting
        if (context.Sorting != Sorting.None)
        {
            query = context.Sorting.Direction == SortDirection.Ascending
                ? query.SortBy(context.Sorting.Field)
                : query.SortByDescending(context.Sorting.Field);
        }

        // Manual paging
        var totalItems = (int)query.CountDocuments();
        if (context.Paging.IsPaged)
        {
            query = query.Skip(context.Paging.Skip).Limit(context.Paging.Size);
        }

        var data = query.ToList();

        return new QueryResult<IEnumerable<DebitAccount>>
        {
            Data = data,
            Paging = new PagingInfo(
                context.Paging.Page,
                context.Paging.Size,
                totalItems)
        };
    }
}
```

## Query Renderers

Query renderers provide a way to implement custom processing for specific data types.
They implement the `IQueryRendererFor<T>` interface:

```csharp
public class DebitAccountQueryRenderer : IQueryRendererFor<IQueryable<DebitAccount>>
{
    public QueryRendererResult Execute(IQueryable<DebitAccount> query, QueryContext queryContext)
    {
        var totalItems = query.Count();

        // Apply custom business logic
        query = query.Where(account => account.Balance >= 0); // Only show non-negative balances

        // Apply sorting
        if (queryContext.Sorting != Sorting.None)
        {
            query = queryContext.Sorting.Field.ToLowerInvariant() switch
            {
                "name" => ApplySorting(query, a => a.Name.ToString(), queryContext.Sorting.Direction),
                "balance" => ApplySorting(query, a => a.Balance, queryContext.Sorting.Direction),
                "owner" => ApplySorting(query, a => a.Owner.ToString(), queryContext.Sorting.Direction),
                _ => query
            };
        }

        // Apply paging
        if (queryContext.Paging.IsPaged)
        {
            query = query.Skip(queryContext.Paging.Skip).Take(queryContext.Paging.Size);
        }

        return new QueryRendererResult(totalItems, query.ToList());
    }

    static IQueryable<DebitAccount> ApplySorting<TKey>(
        IQueryable<DebitAccount> query,
        Expression<Func<DebitAccount, TKey>> keySelector,
        SortDirection direction)
    {
        return direction == SortDirection.Ascending
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector);
    }
}
```

### Built-in Renderers

Arc includes built-in query renderers:

#### QueryableQueryRenderer

Automatically handles `IQueryable<T>` return types:

```csharp
[HttpGet]
public IQueryable<DebitAccount> GetAccountsQueryable()
{
    return _collection.AsQueryable();
}
```

This automatically gets:

- Sorting by any field
- Paging with proper metadata
- Optimized database queries

## MongoDB Extensions

Arc provides MongoDB-specific extensions for observable queries:

### Observe() Extension

The `.Observe()` extension method on `IMongoCollection<T>` automatically handles:

- Initial data loading
- Change stream monitoring
- Sorting and filtering
- Client disconnection cleanup

```csharp
[HttpGet("observable")]
public ISubject<IEnumerable<DebitAccount>> GetAccountsObservable()
{
    // Automatic sorting and filtering based on query context
    return _collection.Observe();
}

[HttpGet("observable-filtered")]
public ISubject<IEnumerable<DebitAccount>> GetActiveAccountsObservable()
{
    return _collection.Observe(account => account.Balance > 0);
}
```

### Advanced MongoDB Observe

```csharp
[HttpGet("observable-advanced")]
public ISubject<IEnumerable<DebitAccount>> GetAccountsObservableAdvanced()
{
    var filter = Builders<DebitAccount>.Filter.And(
        Builders<DebitAccount>.Filter.Gt(a => a.Balance, 0),
        Builders<DebitAccount>.Filter.Lt(a => a.Balance, 100000)
    );

    return _collection.Observe(filter);
}
```

## Custom Query Providers

For complex scenarios, you can create custom query providers that implement `IQueryRendererFor<T>`:

```csharp
public class AccountSummaryRenderer : IQueryRendererFor<AccountSummary>
{
    readonly IMongoCollection<DebitAccount> _collection;

    public AccountSummaryRenderer(IMongoCollection<DebitAccount> collection)
    {
        _collection = collection;
    }

    public QueryRendererResult Execute(AccountSummary query, QueryContext queryContext)
    {
        // Custom aggregation logic
        var pipeline = new BsonDocument[]
        {
            new("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "totalAccounts", new BsonDocument("$sum", 1) },
                { "totalBalance", new BsonDocument("$sum", "$balance") },
                { "averageBalance", new BsonDocument("$avg", "$balance") }
            })
        };

        var result = _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefault();
        
        if (result is not null)
        {
            var summary = new AccountSummary(
                result["totalAccounts"].AsInt32,
                result["totalBalance"].AsDecimal(),
                result["averageBalance"].AsDecimal()
            );
            return new QueryRendererResult(1, summary);
        }

        return new QueryRendererResult(0, null);
    }
}
```

## Query Filters

Query filters execute before query renderers and can perform validation, authorization, logging, and other cross-cutting concerns. They implement the `IQueryFilter` interface:

```csharp
public class AccountSecurityFilter : IQueryFilter
{
    readonly ICurrentUser _currentUser;

    public AccountSecurityFilter(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<QueryResult> OnPerform(QueryContext context)
    {
        // Check if user has permission to execute this query
        if (!await _currentUser.HasPermissionAsync("accounts.read"))
        {
            return QueryResult.Unauthorized(context.CorrelationId, "Access denied to accounts");
        }

        return QueryResult.Success(context.CorrelationId);
    }
}
```

### Built-in Query Filters

Arc includes several built-in query filters that provide essential functionality:

| Filter | Description |
|--------|-------------|
| `DataAnnotationValidationFilter` | Validates query parameters using data annotations (e.g., `[Required]`, `[Range]`, etc.) applied to query properties |
| `FluentValidationFilter` | Validates queries using FluentValidation validators, supporting complex validation scenarios |
| `AuthorizationFilter` | Provides authorization for queries using `[Authorize]` and `[Roles]` attributes |

#### DataAnnotation Validation Filter

Automatically validates query parameters using DataAnnotations attributes:

```csharp
public class GetAccountByIdQuery
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [MaxLength(100)]
    public string? Filter { get; set; }
}

[HttpGet("{id}")]
public Task<DebitAccount?> GetAccountById([FromQuery] GetAccountByIdQuery query)
{
    // Validation happens automatically via DataAnnotationValidationFilter
    return _collection.Find(a => a.Id == query.Id).FirstOrDefaultAsync();
}
```

#### FluentValidation Filter

For complex validation scenarios using FluentValidation:

```csharp
public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
{
    public GetAccountByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Account ID must be greater than 0");

        RuleFor(x => x.Filter)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Filter))
            .WithMessage("Filter cannot exceed 50 characters");
    }
}
```

#### Authorization Filter

This filter provides query-level authorization using ASP.NET Core authorization attributes.

You can use the standard `[Authorize]` attribute:

```csharp
[HttpGet("secure-accounts")]
[Authorize(Roles = "Admin,Manager")]
public Task<IEnumerable<DebitAccount>> GetSecureAccounts()
{
    return _collection.Find(_ => true).ToListAsync();
}
```

Or the convenience `[Roles]` attribute provided by Cratis Arc:

```csharp
[HttpGet("admin-accounts")]
[Roles("Admin")]
public Task<IEnumerable<DebitAccount>> GetAdminAccounts()
{
    return _collection.Find(_ => true).ToListAsync();
}

[HttpGet("manager-accounts")]
[Roles("Manager", "TeamLead")] // User needs any one of these roles
public Task<IEnumerable<DebitAccount>> GetManagerAccounts()
{
    return _collection.Find(_ => true).ToListAsync();
}
```

The authorization filter automatically checks:

- User authentication
- Required roles (if specified)
- Returns `QueryResult.Unauthorized` if authorization fails

### Custom Query Filters

You can create custom filters for cross-cutting concerns:

```csharp
public class QueryLoggingFilter : IQueryFilter
{
    readonly ILogger<QueryLoggingFilter> _logger;

    public QueryLoggingFilter(ILogger<QueryLoggingFilter> logger)
    {
        _logger = logger;
    }

    public Task<QueryResult> OnPerform(QueryContext context)
    {
        _logger.LogInformation("Executing query {QueryName} with correlation {CorrelationId}",
            context.Name, context.CorrelationId);

        return Task.FromResult(QueryResult.Success(context.CorrelationId));
    }
}
```

### Namespace-Based Authorization Filters

For cross-cutting authorization, apply one `IQueryFilter` to an entire namespace:

```csharp
using Cratis.Arc.Http;
using Cratis.Arc.Queries;

namespace MyApp.Features.Security;

public class NamespaceAuthorizationQueryFilter(IHttpRequestContextAccessor requestContextAccessor) : IQueryFilter
{
    const string ProtectedNamespace = "MyApp.Features.Payments";
    const string RequiredRole = "Payments";

    public Task<QueryResult> OnPerform(QueryContext context)
    {
        var isProtectedQuery = context.Name.Value.StartsWith(ProtectedNamespace, StringComparison.Ordinal);
        if (!isProtectedQuery)
        {
            return Task.FromResult(QueryResult.Success(context.CorrelationId));
        }

        var hasRole = requestContextAccessor.Current?.User.IsInRole(RequiredRole) ?? false;
        return Task.FromResult(
            hasRole
                ? QueryResult.Success(context.CorrelationId)
                : QueryResult.Unauthorized(context.CorrelationId));
    }
}
```

This lets you keep query methods clean while still enforcing authorization consistently across an entire feature area.

All filters are automatically discovered and executed by the query pipeline. They run in registration order, and if any filter returns an unsuccessful result, the query execution stops.

## Query Result Metadata

All queries automatically include metadata in the response:

```json
{
  "data": [...],
  "paging": {
    "page": 0,
    "pageSize": 50,
    "totalItems": 1337,
    "hasPrevious": false,
    "hasNext": true
  },
  "correlationId": "12345678-1234-1234-1234-123456789012",
  "isSuccess": true,
  "isAuthorized": true,
  "isValid": true,
  "hasExceptions": false,
  "validationResults": [],
  "exceptionMessages": [],
  "exceptionStackTrace": ""
}
```

## Performance Considerations

### Efficient Sorting

Use indexed fields for sorting to ensure good performance:

```csharp
// Good - if 'name' is indexed
GET /api/accounts?sortby=name&sortDirection=asc

// Potentially slow - if 'balance' is not indexed
GET /api/accounts?sortby=balance&sortDirection=desc
```

### Efficient Paging

Use reasonable page sizes to balance performance and user experience:

```csharp
// Good
GET /api/accounts?page=0&pageSize=50

// Potentially problematic
GET /api/accounts?page=0&pageSize=10000
```

### Query Optimization

Return `IQueryable<T>` when possible to enable database-level optimizations:

```csharp
// Good - enables database-level sorting and paging
[HttpGet]
public IQueryable<DebitAccount> GetAccounts()
{
    return _collection.AsQueryable();
}

// Less efficient - loads all data into memory first
[HttpGet]
public IEnumerable<DebitAccount> GetAccountsList()
{
    return _collection.Find(_ => true).ToList();
}
```

## Best Practices

1. **Return `IQueryable<T>`** when possible for automatic sorting and paging
2. **Use indexed fields** for sorting to ensure good performance
3. **Implement custom renderers** for complex business logic
4. **Keep page sizes reasonable** (typically 10-100 items)
5. **Use query filters** for cross-cutting concerns like security
6. **Monitor query performance** and optimize slow queries
7. **Test sorting and paging** with realistic data volumes
