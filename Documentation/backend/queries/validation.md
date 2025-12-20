# Query Validation

Query parameters can be validated using Arc's validation infrastructure.
The validation happens in the query pipeline through validation filters before query performers are executed.

> **💡 Client-Side Validation**: When using FluentValidation, validation rules are automatically extracted by the [ProxyGenerator](../proxy-generation/validation.md) and run on the client before server calls. This provides immediate feedback to users and reduces unnecessary server requests.

## Validation Filters

Arc provides two validation filters that automatically validate query parameters:

### DataAnnotationValidationFilter

Automatically validates query parameters using System.ComponentModel.DataAnnotations attributes:

```csharp
// Query performer method
[ReadModel]
public class Accounts
{
    public static IEnumerable<DebitAccount> SearchAccounts(
        [Required][StringLength(50)] string name,
        [Range(0, double.MaxValue)] decimal? minBalance,
        [Range(0, double.MaxValue)] decimal? maxBalance,
        IMongoCollection<DebitAccount> collection)
    {
        // Validation happens automatically in the query pipeline
        // This method only executes if validation passes
        
        var filterBuilder = Builders<DebitAccount>.Filter;
        var filters = new List<FilterDefinition<DebitAccount>>();

        filters.Add(filterBuilder.Regex(a => a.Name, new BsonRegularExpression(name, "i")));

        if (minBalance.HasValue)
            filters.Add(filterBuilder.Gte(a => a.Balance, minBalance.Value));

        if (maxBalance.HasValue)
            filters.Add(filterBuilder.Lte(a => a.Balance, maxBalance.Value));

        var combinedFilter = filterBuilder.And(filters);
        return collection.Find(combinedFilter).ToList();
    }
}
```

### FluentValidationFilter

Automatically validates query parameters using FluentValidation validators. Create validators by inheriting from `QueryValidator<T>`:

```csharp
// Define a concept for the query parameter type
public record AccountSearchParams(string Name, decimal? MinBalance, decimal? MaxBalance);

// Create a validator for the parameter type
public class AccountSearchParamsValidator : QueryValidator<AccountSearchParams>
{
    public AccountSearchParamsValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Account name is required and must be less than 50 characters");

        RuleFor(x => x.MinBalance)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinBalance.HasValue)
            .WithMessage("Minimum balance must be greater than or equal to 0");

        RuleFor(x => x.MaxBalance)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxBalance.HasValue)
            .WithMessage("Maximum balance must be greater than or equal to 0");

        RuleFor(x => x.MinBalance)
            .LessThanOrEqualTo(x => x.MaxBalance)
            .When(x => x.MinBalance.HasValue && x.MaxBalance.HasValue)
            .WithMessage("Minimum balance must be less than or equal to maximum balance");
    }
}

// Use the validated parameter type in query performer
[ReadModel]
public class Accounts
{
    public static IEnumerable<DebitAccount> SearchAccountsWithValidation(
        AccountSearchParams searchParams,
        IMongoCollection<DebitAccount> collection)
    {
        // FluentValidation happens automatically in the query pipeline
        var filterBuilder = Builders<DebitAccount>.Filter;
        var filters = new List<FilterDefinition<DebitAccount>>();

        if (!string.IsNullOrEmpty(searchParams.Name))
            filters.Add(filterBuilder.Regex(a => a.Name, new BsonRegularExpression(searchParams.Name, "i")));

        if (searchParams.MinBalance.HasValue)
            filters.Add(filterBuilder.Gte(a => a.Balance, searchParams.MinBalance.Value));

        if (searchParams.MaxBalance.HasValue)
            filters.Add(filterBuilder.Lte(a => a.Balance, searchParams.MaxBalance.Value));

        var combinedFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;
        return collection.Find(combinedFilter).ToList();
    }
}
```

## How Validation Filters Work

The validation filters operate in the query pipeline:

1. **Parameter Discovery**: Filters use `IQueryPerformerProviders` to discover query parameters and their types
2. **Individual Parameter Validation**: Each parameter value from `QueryArguments` is validated against its type
3. **Validation Results**: Failed validations return a `QueryResult` with validation errors
4. **Pipeline Continuation**: Only successful validations allow the query performer to execute

## Automatic Model Validation

## Controller-Based Query Validation

For controller-based queries (using `[HttpGet]` endpoints), validation works with the standard ASP.NET Core model validation:

```csharp
public record AccountSearchQuery(
    [Required]
    [StringLength(50)]
    string Name,

    [Range(0, double.MaxValue)]
    decimal? MinBalance,

    [Range(0, double.MaxValue)]
    decimal? MaxBalance);

[HttpGet("search")]
public IEnumerable<DebitAccount> SearchAccounts([FromQuery] AccountSearchQuery query)
{
    // If validation fails, a 400 Bad Request is returned automatically
    // This code only executes if validation passes
    
    var filterBuilder = Builders<DebitAccount>.Filter;
    var filters = new List<FilterDefinition<DebitAccount>>();

    filters.Add(filterBuilder.Regex(a => a.Name, new BsonRegularExpression(query.Name, "i")));

    if (query.MinBalance.HasValue)
        filters.Add(filterBuilder.Gte(a => a.Balance, query.MinBalance.Value));

    if (query.MaxBalance.HasValue)
        filters.Add(filterBuilder.Lte(a => a.Balance, query.MaxBalance.Value));

    var combinedFilter = filterBuilder.And(filters);
    return _collection.Find(combinedFilter).ToList();
}
```

## Standard Data Annotations

Arc supports all standard validation attributes:

```csharp
public record ProductSearchQuery(
    [Required]
    [StringLength(100, MinimumLength = 3)]
    string Name,

    [Range(0.01, 999999.99)]
    decimal? MinPrice,

    [Range(0.01, 999999.99)]
    decimal? MaxPrice,

    [RegularExpression(@"^[A-Z]{2,4}$")]
    string? Category,

    [EmailAddress]
    string? ContactEmail,

    [Url]
    string? Website);
```

## Custom Validators

For complex validation logic, create custom validators by inheriting from `QueryValidator<T>`:

```csharp
public class AccountSearchQueryValidator : QueryValidator<AccountSearchQuery>
{
    public AccountSearchQueryValidator()
    {
        RuleFor(x => x.MinBalance)
            .LessThanOrEqualTo(x => x.MaxBalance)
            .When(x => x.MinBalance.HasValue && x.MaxBalance.HasValue)
            .WithMessage("Minimum balance must be less than or equal to maximum balance");

        RuleFor(x => x.Name)
            .Must(BeValidAccountName)
            .WithMessage("Account name contains invalid characters");

        RuleFor(x => x)
            .Must(HaveAtLeastOneSearchCriteria)
            .WithMessage("At least one search criteria must be provided");
    }

    bool BeValidAccountName(string name)
    {
        // Custom validation logic
        return !string.IsNullOrEmpty(name) && 
               name.All(char.IsLetterOrDigit) || 
               name.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c));
    }

    bool HaveAtLeastOneSearchCriteria(AccountSearchQuery query)
    {
        return !string.IsNullOrEmpty(query.Name) ||
               query.MinBalance.HasValue ||
               query.MaxBalance.HasValue;
    }
}
```

## FluentValidation Support

Arc uses FluentValidation internally, giving you access to powerful validation rules:

```csharp
public class CustomerQueryValidator : QueryValidator<CustomerQuery>
{
    public CustomerQueryValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Phone number must be in international format");

        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(150)
            .When(x => x.Age.HasValue);

        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 10)
            .When(x => x.Tags != null)
            .WithMessage("Maximum 10 tags allowed");
    }
}
```

## Cross-Field Validation

Validate relationships between multiple fields:

```csharp
public class DateRangeQueryValidator : QueryValidator<DateRangeQuery>
{
    public DateRangeQueryValidator()
    {
        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Start date must be before or equal to end date");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(DateTime.Today.AddDays(-365))
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date cannot be more than one year in the past");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || 
                      (x.EndDate.Value - x.StartDate.Value).Days <= 90)
            .WithMessage("Date range cannot exceed 90 days");
    }
}
```

## Async Validation

For validation that requires database lookups or external services:

```csharp
public class AccountExistsQueryValidator : QueryValidator<AccountExistsQuery>
{
    readonly IMongoCollection<DebitAccount> _collection;

    public AccountExistsQueryValidator(IMongoCollection<DebitAccount> collection)
    {
        _collection = collection;

        RuleFor(x => x.AccountId)
            .MustAsync(AccountExists)
            .WithMessage("Account does not exist");

        RuleFor(x => x.OwnerEmail)
            .MustAsync(OwnerEmailIsValid)
            .When(x => !string.IsNullOrEmpty(x.OwnerEmail))
            .WithMessage("Owner email is not registered");
    }

    async Task<bool> AccountExists(AccountId accountId, CancellationToken cancellationToken)
    {
        var count = await _collection.CountDocumentsAsync(
            a => a.Id == accountId, 
            cancellationToken: cancellationToken);
        return count > 0;
    }

    async Task<bool> OwnerEmailIsValid(string email, CancellationToken cancellationToken)
    {
        // Call external service or database to validate email
        // This is just an example
        await Task.Delay(100, cancellationToken);
        return email.Contains("@") && email.Contains(".");
    }
}
```

## Model-Bound Query Validation

For model-bound queries with `[ReadModel]`, validation can be applied to the method parameters:

```csharp
public record GetAccountsByOwnerQuery(
    [Required]
    CustomerId OwnerId,
    
    [Range(1, 1000)]
    int MaxResults = 100);

[ReadModel]
public class Accounts
{
    public static IEnumerable<DebitAccount> GetAccountsByOwner(
        GetAccountsByOwnerQuery query,
        IMongoCollection<DebitAccount> collection)
    {
        return collection
            .Find(a => a.Owner == query.OwnerId)
            .Limit(query.MaxResults)
            .ToList();
    }
}

// Custom validator for the query
public class GetAccountsByOwnerQueryValidator : QueryValidator<GetAccountsByOwnerQuery>
{
    public GetAccountsByOwnerQueryValidator()
    {
        RuleFor(x => x.OwnerId)
            .NotNull()
            .NotEmpty()
            .WithMessage("Owner ID is required");

        RuleFor(x => x.MaxResults)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Max results must be between 1 and 1000");
    }
}
```

## Validation Error Responses

### Filter-Based Validation Errors

When validation fails in the query pipeline (using validation filters), the query returns a `QueryResult` with detailed error information:

```json
{
  "data": null,
  "paging": {
    "page": 0,
    "pageSize": 0,
    "totalItems": 0
  },
  "correlationId": "12345678-1234-1234-1234-123456789012",
  "isSuccess": false,
  "isAuthorized": true,
  "isValid": false,
  "hasExceptions": false,
  "validationResults": [
    {
      "severity": "Error",
      "message": "Account name is required and must be less than 50 characters",
      "members": ["name"]
    },
    {
      "severity": "Error", 
      "message": "Minimum balance must be greater than or equal to 0",
      "members": ["minBalance"]
    }
  ],
  "exceptionMessages": [],
  "exceptionStackTrace": ""
}
```

### Controller-Based Validation Errors

When validation fails on controller-based queries, the response returns a 400 Bad Request with detailed error information:

```json
{
  "data": null,
  "paging": {
    "page": 0,
    "pageSize": 0,
    "totalItems": 0
  },
  "correlationId": "12345678-1234-1234-1234-123456789012",
  "isSuccess": false,
  "isAuthorized": true,
  "isValid": false,
  "hasExceptions": false,
  "validationResults": [
    {
      "severity": "Error",
      "message": "Account name is required",
      "members": ["name"]
    },
    {
      "severity": "Error", 
      "message": "Minimum balance must be greater than or equal to 0",
      "members": ["minBalance"]
    }
  ],
  "exceptionMessages": [],
  "exceptionStackTrace": ""
}
```

## Ignoring Validation

In some cases, you may want to bypass validation (useful for administrative queries):

```csharp
[HttpGet("admin/all-data")]
[IgnoreValidation] // Skip validation for this endpoint
public IEnumerable<DebitAccount> GetAllDataForAdmin([FromQuery] AdminQuery query)
{
    // This will execute without validation
    return _collection.Find(_ => true).ToList();
}
```

## Conditional Validation

Apply validation rules conditionally:

```csharp
public class ConditionalQueryValidator : QueryValidator<ConditionalQuery>
{
    public ConditionalQueryValidator()
    {
        // Only validate email if contact method is email
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => x.ContactMethod == ContactMethod.Email);

        // Only validate phone when contact method is phone
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => x.ContactMethod == ContactMethod.Phone);

        // Require at least one contact method
        RuleFor(x => x)
            .Must(x => x.ContactMethod != ContactMethod.None)
            .WithMessage("A contact method must be specified");
    }
}
```

## Complex Object Validation

Validate nested objects and collections:

```csharp
public record OrderSearchQuery(
    string? CustomerName,
    DateRangeQuery? DateRange,
    List<string>? ProductCategories,
    AddressQuery? ShippingAddress);

public class OrderSearchQueryValidator : QueryValidator<OrderSearchQuery>
{
    public OrderSearchQueryValidator()
    {
        RuleFor(x => x.DateRange)
            .SetValidator(new DateRangeQueryValidator())
            .When(x => x.DateRange != null);

        RuleFor(x => x.ShippingAddress)
            .SetValidator(new AddressQueryValidator())
            .When(x => x.ShippingAddress != null);

        RuleForEach(x => x.ProductCategories)
            .NotEmpty()
            .Length(2, 50)
            .When(x => x.ProductCategories != null);
    }
}
```

## When to Use Each Validation Approach

### Use Filter-Based Validation (Recommended)

- **Model-bound queries**: Using `[ReadModel]` classes with static methods
- **Query pipeline**: Working with the query pipeline infrastructure  
- **Parameter-level validation**: Need to validate individual query parameters
- **Consistent validation**: Want validation behavior consistent with commands
- **Complex parameter types**: Using concepts or complex objects as parameters

### Use Controller-Based Validation

- **HTTP endpoints**: Creating traditional REST API endpoints with `[HttpGet]`
- **ASP.NET Core integration**: Leveraging existing ASP.NET Core validation infrastructure
- **Simple query objects**: Working with simple DTOs as query parameters
- **Web API consistency**: Maintaining consistency with other ASP.NET Core controllers

## Best Practices

1. **Prefer filter-based validation** for model-bound queries using the query pipeline
2. **Use data annotations** for simple validation rules on individual parameters
3. **Create custom validators** for complex business logic and cross-parameter validation
4. **Validate early** to prevent unnecessary database queries and improve performance
5. **Provide clear error messages** that help users understand how to fix their input
6. **Use async validation sparingly** as it can impact query performance significantly
7. **Test validation rules thoroughly** with edge cases and boundary conditions
8. **Consider performance impact** of validation, especially for high-frequency queries
9. **Document validation requirements** clearly for API consumers
10. **Use conditional validation** to avoid unnecessary validation overhead
11. **Group related validation rules** logically for better maintainability
12. **Validate parameter types** that are concepts or complex objects rather than individual parameters when possible
