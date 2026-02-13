# FromRequest Attribute

The `[FromRequest]` attribute is a powerful model binding feature that allows you to combine data from multiple sources of an HTTP request into a single model.
Unlike the standard ASP.NET Core model binding attributes that bind from a single source, `[FromRequest]` intelligently merges data from the request body with
data from other parts of the request (route parameters, query strings, etc.). This creates a unified object that's perfect for comprehensive validation with
frameworks like FluentValidation, enables cross-parameter validation rules, and simplifies controller methods by reducing multiple parameters into a single,
well-structured request model.

## Overview

When you decorate a parameter with `[FromRequest]`, the Arc will:

1. First attempt to bind the entire model from the request body (JSON)
2. Then attempt to bind the same model from other request sources (route, query, headers)
3. Merge the results, with request body taking precedence for non-default values
4. Use values from other sources only when the corresponding property in the body-bound model has a default value

This enables powerful scenarios where you can have a JSON payload that gets enhanced with additional data from the URL or query parameters.

## How It Works

The `FromRequestModelBinder` performs the following steps:

1. **Body Binding**: Uses the standard body model binder to deserialize the request body into your model
2. **Complex Binding**: Uses the complex model binder to bind from route, query, and other sources
3. **Intelligent Merging**: For each property:
   - If the body-bound value is not the default value for that type, it keeps the body value
   - If the body-bound value is the default value but the complex-bound value is not, it uses the complex value
   - This allows selective overriding of JSON properties with URL/query parameters

## Usage Examples

### Basic Usage

```csharp
public class UserUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [FromRoute] public int UserId { get; set; }
    [FromQuery] public bool NotifyUser { get; set; }
}

[HttpPut("users/{userId}")]
public async Task<IActionResult> UpdateUser([FromRequest] UserUpdateRequest request)
{
    // request.UserId will be populated from the route
    // request.NotifyUser will be populated from query string
    // request.Name and request.Email will come from JSON body
    // If Name or Email are not provided in JSON (or are empty/null), 
    // they could be overridden by query parameters if present
    
    return Ok();
}
```

**Request Example:**

```http
PUT /users/123?notifyUser=true
Content-Type: application/json

{
    "name": "John Doe",
    "email": "john@example.com"
}
```

In this case:

- `UserId` = 123 (from route)
- `NotifyUser` = true (from query)
- `Name` = "John Doe" (from JSON body)
- `Email` = `"john@example.com"` (from JSON body)

### Fallback Scenarios

```csharp
public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    [FromRoute] public string Category { get; set; } = string.Empty;
}

[HttpPost("search/{category}")]
public async Task<IActionResult> Search([FromRequest] SearchRequest request)
{
    return Ok();
}
```

**Request with partial JSON:**

```http
POST /search/electronics?page=2&pageSize=20
Content-Type: application/json

{
    "query": "laptop"
}
```

Result:

- `Query` = "laptop" (from JSON body)
- `Page` = 2 (from query, since JSON didn't provide it and default is 1)
- `PageSize` = 20 (from query, since JSON didn't provide it and default is 10)
- `Category` = "electronics" (from route)

**Request with complete JSON:**

```http
POST /search/electronics?page=2&pageSize=20
Content-Type: application/json

{
    "query": "laptop",
    "page": 5,
    "pageSize": 50
}
```

Result:

- `Query` = "laptop" (from JSON body)
- `Page` = 5 (from JSON body, overrides query parameter)
- `PageSize` = 50 (from JSON body, overrides query parameter)
- `Category` = "electronics" (from route)

## Benefits

1. **Flexible API Design**: Allows clients to provide data in the most convenient way
2. **Backward Compatibility**: Existing APIs can be enhanced without breaking changes
3. **RESTful Patterns**: Supports having resource identifiers in the URL while allowing detailed data in the body
4. **Progressive Enhancement**: Start with simple query parameters and optionally move to JSON for complex scenarios
5. **Unified Object for Validation**: Creates a single, complete object that can be validated using frameworks like FluentValidation

### Validation Scenarios

One of the key advantages of `[FromRequest]` is that it creates a complete object that contains all the data from your request, regardless of where it came from. This is particularly valuable when using validation frameworks like FluentValidation, which work with objects rather than individual parameters.

**Traditional approach (multiple parameters):**

```csharp
[HttpPut("users/{userId}")]
public async Task<IActionResult> UpdateUser(
    [FromRoute] int userId,
    [FromQuery] bool notifyUser,
    [FromBody] UserUpdateData data)
{
    // Validation is fragmented across multiple objects
    // FluentValidation can't easily validate cross-parameter rules
    // Manual validation logic becomes complex
}
```

**With [FromRequest]:**

```csharp
public class UserUpdateRequest
{
    [FromRoute] public int UserId { get; set; }
    [FromQuery] public bool NotifyUser { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserUpdateRequestValidator : AbstractValidator<UserUpdateRequest>
{
    public UserUpdateRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress();
        
        // Cross-property validation is now possible
        RuleFor(x => x.NotifyUser)
            .Equal(false)
            .When(x => string.IsNullOrEmpty(x.Email))
            .WithMessage("Cannot notify user without email address");
    }
}

[HttpPut("users/{userId}")]
public async Task<IActionResult> UpdateUser([FromRequest] UserUpdateRequest request)
{
    // Single object validation with complete context
    // FluentValidation can validate the entire request as one unit
    // Cross-parameter validation rules are straightforward
}
```

This approach enables:

- **Cross-parameter validation**: Rules that depend on multiple values from different request sources
- **Unified validation logic**: All validation rules in one place for the complete request
- **Cleaner controllers**: Single parameter instead of multiple individual parameters
- **Better testability**: Test validators with complete request objects rather than parameter combinations

## Integration with Other Features

### Swagger/OpenAPI

The Arc includes a `FromRequestOperationFilter` that automatically updates your Swagger documentation to correctly represent `[FromRequest]` parameters as request body schemas rather than individual parameters.

### Proxy Generation

The TypeScript proxy generator understands `[FromRequest]` parameters and generates appropriate client code that handles the combination of route, query, and body parameters correctly.

## Best Practices

1. **Use for Hybrid Scenarios**: Best suited when you need both URL-based parameters (IDs, filters) and complex body data
2. **Default Values**: Ensure your model properties have appropriate default values to enable the fallback behavior
3. **Documentation**: Clearly document which properties can come from which sources for API consumers
4. **Validation**: Apply validation attributes as needed - they work seamlessly with `[FromRequest]`

## Limitations

- Properties must have appropriate default values for the merging logic to work correctly
- The merging is based on default value comparison, so be mindful of what constitutes a "default" for your types
- Complex nested scenarios may require careful consideration of the binding order
