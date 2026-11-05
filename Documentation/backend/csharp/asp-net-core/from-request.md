---
title: Combine request values with Cratis Arc FromRequest
description: Bind JSON, route, and query values into one MVC request model with Cratis Arc, then validate the merged input with explicit fallback rules.
---

Use Cratis Arc's `[FromRequest]` when an MVC action needs one input model assembled from JSON, route values, and query parameters. The action and its validator receive the merged object, so rules can check related values together.

This attribute belongs to the ASP.NET Core MVC integration. Model-bound Arc commands and queries use their own binding contracts. Before applying it to an existing action, check the precedence below: merging can change which client-supplied value wins.

## Overview

Arc binds the JSON body first, then uses MVC's other request sources to fill properties that still hold their type's default value. Non-default body values win.

See [default values and fallback](#default-values-and-fallback) when zero, false, or property initializers matter.

## Usage Examples

### Basic Usage

The following fragments belong in an existing ASP.NET Core Arc application with MVC controllers registered and mapped. Declare the request model, then use it as the controller action's parameter:

```csharp
using Cratis.Arc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

public class UserUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [FromRoute] public int UserId { get; set; }
    [FromQuery] public bool NotifyUser { get; set; }
}

[HttpPut("users/{userId}")]
public IActionResult UpdateUser([FromRequest] UserUpdateRequest request)
{
    // Apply the update through your application service after validation.
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

The action receives `UserId = 123`, `NotifyUser = true`, and the name and email from JSON. The example shows binding; `Ok()` alone does not persist an update.

> [!IMPORTANT]
> A non-default body value can also override a merged route ID. When the URL must identify the resource being changed, bind that ID separately with `[FromRoute]` and use it in the authorization and update—not the merged property.

### Fallback Scenarios

Property initializers can prevent URL values from filling in missing JSON fields. In this controller fragment, `Page`, `PageSize`, and `Category` all start with non-default values:

```csharp
public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    [FromRoute] public string Category { get; set; } = string.Empty;
}

[HttpPost("search/{category}")]
public IActionResult Search([FromRequest] SearchRequest request)
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
- `Page` = 1 (initializer survives; it is not CLR default `0`)
- `PageSize` = 10 (initializer survives)
- `Category` = `""` (initializer survives; it is not CLR default `null`)

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
- `Category` = `""` (initializer survives)

To make the category eligible for route fallback, declare it as `[FromRoute] public string? Category { get; set; }` and validate it after binding. To allow paging fallback, do not initialize the numeric properties to nonzero values; apply business defaults after merging. `[FromRequest]` does not track field presence, so it cannot distinguish an omitted integer from an explicitly supplied zero.

<a id="how-it-works"></a>

## Default values and fallback

Fallback compares a property's value with its type's default; it does not track whether the JSON field was present.

| Body-bound value                                   | Eligible for fallback? |
| -------------------------------------------------- | ---------------------- |
| `0` for an integer                                 | Yes                    |
| `false` for a Boolean                              | Yes                    |
| `null` for a reference                             | Yes                    |
| An empty string                                    | No                     |
| A nonzero property initializer, such as `Page = 1` | No                     |

An explicit JSON zero or false can therefore be replaced by a route/query value. Apply application defaults after binding when you want missing values to remain eligible for fallback.

## Benefits

A merged model lets a validator check values that arrived from different sources. For example, a notification flag from the query string can require an email address from JSON. The controller no longer has to assemble those values before applying the rule.

Test those rules directly with request objects. Keep separate HTTP tests for binding, because a validator test cannot tell you whether a route value or body value reached the model.

### Validation Scenarios

With one request model, a validator can check the name, email, and notification preference together.

**Traditional approach (multiple parameters):**

```csharp
[HttpPut("users/{userId}")]
public IActionResult UpdateUser(
    [FromRoute] int userId,
    [FromQuery] bool notifyUser,
    [FromBody] UserUpdateData data)
{
    // Combine the inputs before applying cross-property rules.
    return Ok();
}
```

**With [FromRequest]:**

```csharp
using Cratis.Arc.ModelBinding;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

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
public IActionResult UpdateUser([FromRequest] UserUpdateRequest request)
{
    // Validate the merged request before applying the update.
    return Ok();
}
```

Register and invoke the validator through your MVC validation setup. `[FromRequest]` performs binding; declaring an `AbstractValidator<T>` alone does not establish when it runs. Test the validator directly with merged request objects, and use an HTTP test to check binding.

## Integration with Other Features

### Swagger/OpenAPI

Arc's `FromRequestOperationFilter` describes `[FromRequest]` parameters as body schemas. Check the generated operation's route and query parameters too; see [Swagger integration](./swagger.md) for its configuration and limits.

### Proxy Generation

Model binding and client generation have separate requirements. Check the [proxy generator's supported endpoint shapes](../proxy-generation/index.md) when choosing the client for a mixed-source MVC action.

## Best Practices

Keep properties at their type's default value when they need URL fallback, and apply application defaults after merging. Document the permitted sources and precedence for clients. Before writing, validate the merged input and authorize the resource using the authoritative identifier, bound separately when the URL must decide which resource changes.

## Limitations

Merging compares values, not JSON field presence. For partial updates that must distinguish “omitted” from an explicit zero, false, or null, use a request contract that represents that distinction. Test nested models and conflicting input sources through HTTP; a direct validator test does not exercise binding.
