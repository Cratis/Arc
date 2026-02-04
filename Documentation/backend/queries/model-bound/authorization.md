# Authorization

Model-bound queries support authorization through standard ASP.NET Core authorization attributes as well as the convenient `[Roles]` attribute provided by the Arc.

## Using the Authorize Attribute

You can secure query methods using the standard `[Authorize]` attribute:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [Authorize]
    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();

    [Authorize(Roles = "Admin,Manager")]
    public static IEnumerable<DebitAccount> GetSensitiveAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Balance > 100000).ToList();
}
```

## Using the Roles Attribute

The Arc provides a more convenient `[Roles]` attribute for cleaner syntax when specifying multiple roles:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [Roles("Admin", "Auditor")]
    public static IEnumerable<DebitAccount> GetAdminAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();

    [Roles("Manager")]
    public static IEnumerable<DebitAccount> GetManagerAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Owner != CustomerId.Empty).ToList();
}
```

The user needs to have **at least one** of the specified roles to execute the query.

## Read Model-Level Authorization

You can apply authorization at the read model level to protect all query methods:

```csharp
[ReadModel]
[Roles("User")] // All methods require at least "User" role
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();

    [Roles("Admin")] // Override read model-level authorization
    public static IEnumerable<DebitAccount> GetAdminOnlyAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Balance < 0).ToList();
}
```

## Method-Level Authorization Override

Method-level authorization attributes override class-level ones:

```csharp
[ReadModel]
[Authorize] // Require authentication for all methods
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    // Inherits class-level [Authorize] - requires authentication
    public static IEnumerable<DebitAccount> GetUserAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();

    [Roles("Admin", "Manager")] // Overrides class-level, requires specific roles
    public static IEnumerable<DebitAccount> GetPrivilegedAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Balance > 50000).ToList();

    [AllowAnonymous] // Completely overrides class-level authorization
    public static int GetTotalAccountCount(IMongoCollection<DebitAccount> collection) =>
        (int)collection.CountDocuments(_ => true);
}
```

## Policy-Based Authorization

For more complex authorization scenarios, you can use policy-based authorization:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [Authorize(Policy = "RequireAccountAccess")]
    public static DebitAccount GetAccountById(
        AccountId id, 
        IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Id == id).FirstOrDefault();
        
    [Authorize(Policy = "RequireHighValueAccess")]
    public static IEnumerable<DebitAccount> GetHighValueAccounts(
        IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Balance > 1000000).ToList();
}
```

## Context-Dependent Authorization

Access user context within query methods for dynamic authorization:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [Authorize]
    public static IEnumerable<DebitAccount> GetMyAccounts(
        IMongoCollection<DebitAccount> collection,
        IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Enumerable.Empty<DebitAccount>();

        var customerId = new CustomerId(Guid.Parse(userId));
        return collection.Find(a => a.Owner == customerId).ToList();
    }
    
    [Authorize]
    public static DebitAccount? GetAccountIfOwned(
        AccountId accountId,
        IMongoCollection<DebitAccount> collection,
        IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;

        var customerId = new CustomerId(Guid.Parse(userId));
        return collection.Find(a => a.Id == accountId && a.Owner == customerId).FirstOrDefault();
    }
}
```

## Role Hierarchies

Implement role hierarchies with custom authorization:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [Roles("User")] // Basic users can see their own accounts
    public static IEnumerable<DebitAccount> GetBasicAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Balance >= 0).ToList();

    [Roles("Manager", "Admin")] // Managers and admins can see more
    public static IEnumerable<DebitAccount> GetManagerAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Balance > -1000).ToList();

    [Roles("Admin")] // Only admins can see all accounts including severely overdrawn
    public static IEnumerable<DebitAccount> GetAllAccountsIncludingProblematic(IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();
}
```

## Observable Query Authorization

Authorization also applies to observable queries:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [Authorize]
    public static ISubject<IEnumerable<DebitAccount>> GetAccountsObservable(
        IMongoCollection<DebitAccount> collection) =>
        collection.Observe();

    [Roles("Admin")]
    public static ISubject<IEnumerable<DebitAccount>> GetAdminAccountsObservable(
        IMongoCollection<DebitAccount> collection) =>
        collection.Observe(a => a.Balance < 0);
}
```

## Authorization with Query Parameters

Combine authorization with parameter-based filtering:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [Authorize]
    public static IEnumerable<DebitAccount> GetAccountsByOwner(
        CustomerId ownerId,
        IMongoCollection<DebitAccount> collection,
        IHttpContextAccessor httpContextAccessor)
    {
        var currentUserId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        var isAdmin = httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
        
        // Users can only see their own accounts unless they're admin
        if (!isAdmin && currentUserId != ownerId.Value.ToString())
        {
            return Enumerable.Empty<DebitAccount>();
        }
        
        return collection.Find(a => a.Owner == ownerId).ToList();
    }
}
```

## Custom Authorization Attributes

Create custom authorization attributes for domain-specific logic:

```csharp
public class RequireAccountOwnershipAttribute : AuthorizeAttribute
{
    public RequireAccountOwnershipAttribute() : base("RequireAccountOwnership") { }
}

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [RequireAccountOwnership]
    public static DebitAccount GetAccountDetails(
        AccountId id,
        IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Id == id).FirstOrDefault();
}
```

## Authorization Results

When authorization fails, the query pipeline automatically returns an unauthorized result. The query method will not be executed:

```csharp
// In your policy handler or middleware
public class AccountOwnershipHandler : AuthorizationHandler<AccountOwnershipRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountOwnershipRequirement requirement)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        
        // Check if user owns the account being accessed
        if (/* ownership check logic */)
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}
```

## Anonymous Access

Use `[AllowAnonymous]` to allow public access to specific query methods. This attribute bypasses all authorization requirements, including class-level `[Authorize]` attributes and role requirements.

### How AllowAnonymous Works

The authorization system evaluates attributes in the following order:

1. **Method-level `[AllowAnonymous]`** - If present on the method, allows anonymous access immediately
2. **Method-level `[Authorize]` or `[Roles]`** - If present on the method, these take precedence over class-level attributes
3. **Class-level `[AllowAnonymous]`** - If present on the class (and no method-level authorization), allows anonymous access
4. **Class-level `[Authorize]` or `[Roles]`** - Applied when no method-level attributes are specified

### Method-Level AllowAnonymous

Override class-level authorization for specific query methods:

```csharp
[ReadModel]
[Authorize] // Require authentication by default
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [AllowAnonymous] // Override class-level authorization for public data
    public static int GetTotalAccountCount(IMongoCollection<DebitAccount> collection) =>
        (int)collection.CountDocuments(_ => true);

    [AllowAnonymous]
    public static decimal GetAverageBalance(IMongoCollection<DebitAccount> collection)
    {
        var accounts = collection.Find(_ => true).ToList();
        return accounts.Count > 0 ? accounts.Average(a => a.Balance) : 0;
    }
    
    // This method requires authentication (inherits from class)
    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();
}
```

### Class-Level AllowAnonymous

Apply `[AllowAnonymous]` at the class level to make all query methods publicly accessible by default:

```csharp
[ReadModel]
[AllowAnonymous] // All methods are publicly accessible by default
public record PublicStatistics(string Category, int Count)
{
    public static IEnumerable<PublicStatistics> GetAllStatistics(
        IMongoCollection<PublicStatistics> collection) =>
        collection.Find(_ => true).ToList();

    public static PublicStatistics? GetByCategory(
        string category,
        IMongoCollection<PublicStatistics> collection) =>
        collection.Find(s => s.Category == category).FirstOrDefault();

    [Authorize] // Override class-level: this specific method requires authentication
    public static IEnumerable<PublicStatistics> GetSensitiveStatistics(
        IMongoCollection<PublicStatistics> collection) =>
        collection.Find(s => s.Category.StartsWith("Internal")).ToList();
}
```

### Common Use Cases for AllowAnonymous

- **Public statistics or counts** - Aggregate data that doesn't expose sensitive information
- **Product catalogs** - Public product listings for e-commerce sites
- **Public content** - Blog posts, articles, or documentation
- **Health checks** - System status information for monitoring
- **Search endpoints** - Public search functionality

## Best Practices

1. **Apply authorization at the appropriate level** - Use class-level for broad protection, method-level for specific requirements
2. **Use the `[Roles]` attribute** - More convenient than the standard `[Authorize(Roles = "...")]` syntax
3. **Implement defense in depth** - Combine multiple authorization layers when appropriate
4. **Consider user context** - Use `IHttpContextAccessor` to access current user information for dynamic authorization
5. **Test authorization** - Ensure unauthorized users cannot access protected queries
6. **Use policies for complex logic** - Implement custom authorization policies for domain-specific rules
7. **Be explicit about public access** - Use `[AllowAnonymous]` to clearly indicate intentionally public methods
8. **Log authorization failures** - Monitor and log unauthorized access attempts
9. **Keep authorization simple** - Complex authorization logic should be in services, not query methods

> **Note**: Authorization is evaluated before the query method is called. If authorization fails, the query will not be executed and the result will indicate the authorization failure.

> **Note**: The [proxy generator](../../proxy-generation/) automatically creates TypeScript types that respect your authorization constraints,
> helping prevent unauthorized client-side calls.
