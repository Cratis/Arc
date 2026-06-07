# Authorization

Arc.Core provides authorization capabilities through attributes that protect your commands and queries. This allows you to control access based on authentication status and user roles.

## Overview

Authorization in Arc.Core is attribute-based and supports:

- **Authentication Requirements** - Require users to be authenticated
- **Role-Based Authorization** - Restrict access to specific roles
- **Anonymous Access** - Explicitly allow unauthenticated access
- **Flexible Application** - Apply at class or method level

> [!NOTE]
> **Model-bound is the default.** Arc applies these attributes directly to your `[Command]` records and `[ReadModel]` query methods — the [vertical-slice](/arc/vertical-slices/) style used throughout these docs, and what the examples below lead with. Some deeper sections show claims-based and custom checks for illustration; the part to take away is *where* the `[Authorize]`/`[Roles]` attributes go. For how access control fits together end to end, see [Understanding identity and access](/arc/understanding-identity-and-access/).

## Authorization Attributes

Arc.Core provides authorization through attributes:

- **`[Authorize]`** - Requires authentication and optionally specifies roles or policies
- **`[Roles]`** - Convenience attribute for role-based authorization
- **`[AllowAnonymous]`** - Explicitly allows unauthenticated access

## Basic Usage

### Requiring Authentication

Require users to be authenticated without specifying roles — put the attribute on the `[Command]` record, and its `Handle()` only runs for an authenticated user:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Authorization;

[Authorize]
[Command]
public record UpdateProfile(ProfileId Id, ProfileName Name)
{
    public ProfileRenamed Handle() => new(Name);
}
```

### Role-Based Authorization

Restrict access to specific roles:

```csharp
// Using the Authorize attribute
[Authorize(Roles = "Admin")]
[Command]
public record DeleteUser(UserId Id)
{
    public UserDeleted Handle() => new();
}

// Using the Roles attribute (more readable for multiple roles)
[Roles("Admin", "Manager")]
[Command]
public record ApproveRequest(RequestId Id)
{
    public RequestApproved Handle() => new();
}
```

### Anonymous Access

Explicitly allow anonymous access (useful when you have a fallback policy requiring authentication) — on a model-bound query, the attribute goes on the static query method:

```csharp
[ReadModel]
public record PublicData(DataId Id, string Value)
{
    [AllowAnonymous]
    public static IEnumerable<PublicData> All(IMongoCollection<PublicData> collection) =>
        collection.Find(_ => true).ToList();
}
```

## Applying Authorization

Authorization attributes can be applied at different levels:

### Class-Level Authorization

Apply to all commands or queries in a type:

```csharp
[Authorize]
[Command]
public record UpdateSettings(SettingKey Key, string Value)
{
    public SettingChanged Handle() => new(Key, Value);
}

[Roles("Admin")]
[Command]
public record DeleteAccount(AccountId Id)
{
    public AccountDeleted Handle() => new();
}
```

### Method-Level Authorization

Apply an attribute to a single operation rather than the whole type. On a model-bound command the attribute goes on the `[Command]` record itself, and its `Handle()` only runs once the attribute's requirements are met:

```csharp
[Authorize]
[Command]
public record SecureCommand(string Data)
{
    public SecureOperationCompleted Handle() => new(Data);
}
```

## Role-Based Scenarios

### Single Role Requirement

```csharp
// User must have the "Admin" role
[Roles("Admin")]
[Command]
public record CreateAdmin(string Username)
{
    public AdminCreated Handle() => new(Username);
}
```

### Multiple Role Requirement (OR Logic)

Users need **at least one** of the specified roles:

```csharp
// User must have either "Admin" OR "Manager" role
[Roles("Admin", "Manager")]
[ReadModel]
public record AuditLogEntry(AuditLogId Id, string Action)
{
    public static IEnumerable<AuditLogEntry> All(IMongoCollection<AuditLogEntry> collection) =>
        collection.Find(_ => true).ToList();
}
```

### Combining with Standard Authorize

You can mix `[Authorize]` and `[Roles]` if needed:

```csharp
// Requires authentication via specific scheme AND a role
[Authorize(AuthenticationSchemes = "Bearer")]
[Roles("Admin")]
[Command]
public record SecureAdminCommand(string Data)
{
    public SecureOperationCompleted Handle() => new(Data);
}
```

## AllowAnonymous Attribute

The `[AllowAnonymous]` attribute explicitly allows unauthenticated access. On a model-bound query the attribute goes on the static query method:

```csharp
[ReadModel]
public record CatalogItem(CatalogItemId Id, string Name)
{
    // Anyone can access this, even without authentication
    [AllowAnonymous]
    public static IEnumerable<CatalogItem> All(IMongoCollection<CatalogItem> collection) =>
        collection.Find(_ => true).ToList();
}
```

## Authorization Inheritance Rules

Authorization attributes follow specific inheritance rules:

| Scenario | Result |
|----------|--------|
| `[Authorize]` on class | Requires authentication for all operations |
| `[Roles]` on class | Requires specified roles for all operations |
| `[AllowAnonymous]` on class | Allows anonymous access for all operations |
| Method attribute overrides class | Method-level attribute takes precedence |
| Both `[Authorize]` and `[AllowAnonymous]` on same target | **Error** - throws `AmbiguousAuthorizationLevel` |

### Examples

A read model inherits authorization from the type, and a query method can override it:

```csharp
// Authentication required for every query method on the read model
[Authorize]
[ReadModel]
public record Profile(ProfileId Id, ProfileName Name)
{
    // Inherits [Authorize] from the record — requires authentication
    public static IEnumerable<Profile> All(IMongoCollection<Profile> collection) =>
        collection.Find(_ => true).ToList();

    // Overrides with a more specific role requirement
    [Roles("Admin")]
    public static IEnumerable<Profile> AllForAdmins(IMongoCollection<Profile> collection) =>
        collection.Find(_ => true).ToList();
}
```

A command record can override an `[AllowAnonymous]` default by requiring authentication on a single command:

```csharp
// Anonymous access by default
[AllowAnonymous]
[Command]
public record TrackPageView(string Path)
{
    public PageViewTracked Handle() => new(Path);
}

// Authentication required for this command
[Authorize]
[Command]
public record SubmitFeedback(string Message)
{
    public FeedbackSubmitted Handle() => new(Message);
}
```

Applying both `[Authorize]` and `[AllowAnonymous]` to the same target is an error:

```csharp
// ERROR: This will throw AmbiguousAuthorizationLevel at startup
[AllowAnonymous]
[Authorize]  // Cannot have both!
[Command]
public record InvalidCommand(string Data)
{
    public OperationCompleted Handle() => new(Data);
}
```

## Working with Claims

When a request is authenticated, the `ClaimsPrincipal` is available from the current `HttpContext`. Inject `IHttpContextAccessor` into a model-bound query method (or a command's `Handle()`) and read `HttpContext?.User`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

[ReadModel]
public record UserProfile(UserId Id, string DisplayName)
{
    // Returns the profile for whichever user is currently authenticated
    public static UserProfile? Mine(
        IHttpContextAccessor httpContextAccessor,
        IMongoCollection<UserProfile> collection)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return collection.Find(profile => profile.Id == userId).FirstOrDefault();
    }
}
```

The same `httpContextAccessor.HttpContext?.User` gives you a `ClaimsPrincipal` — use `FindFirstValue(ClaimTypes.NameIdentifier)` for the user id, `FindFirstValue(ClaimTypes.Name)` for the name, and `IsInRole("Admin")` for role checks.

## Authorization Results

When authorization fails, Arc.Core automatically returns appropriate HTTP status codes:

| Scenario | HTTP Status Code | Description |
|----------|-----------------|-------------|
| **Not Authenticated** | 401 Unauthorized | User is not authenticated |
| **Not Authorized** | 403 Forbidden | User is authenticated but doesn't have required permissions |

## Custom Authorization Logic

The `[Authorize]` and `[Roles]` attributes handle coarse-grained access — whether the user is authenticated and in the right role. For row-level checks (for example, "you can only update your own orders") put the logic inside `Handle()`: inject `IHttpContextAccessor`, read the current user's claims, and return a `ValidationResult.Error(...)` when the guard fails. Make the return type `Result<ValidationResult, TEvent>` so the framework knows the command can fail validation:

```csharp
using System.Security.Claims;
using Cratis.Arc.Validation;
using Microsoft.AspNetCore.Http;

[Authorize]
[Command]
public record UpdateOrder(OrderId Id, string Data)
{
    public Result<ValidationResult, OrderUpdated> Handle(
        IHttpContextAccessor httpContextAccessor,
        IOrderRepository orders)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = orders.GetById(Id);

        // Custom authorization: a user can only update their own orders
        if (order.OwnerId != userId)
        {
            return ValidationResult.Error("You can only update your own orders.");
        }

        return new OrderUpdated(Data);
    }
}
```

A returned `ValidationResult.Error` surfaces to the caller as a failed `CommandResult` with validation errors — the command does not append its event.

## Policy-Based Authorization

While Arc.Core focuses on attribute-based authorization, you can implement policy-based logic inside `Handle()` by inspecting the current user's roles and returning a `ValidationResult.Error(...)` when the policy fails:

```csharp
using System.Security.Claims;
using Cratis.Arc.Validation;
using Microsoft.AspNetCore.Http;

[Authorize]
[Command]
public record ApproveExpense(ExpenseId Id)
{
    public Result<ValidationResult, ExpenseApproved> Handle(
        IHttpContextAccessor httpContextAccessor,
        IExpenseRepository expenses)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var expense = expenses.GetById(Id);

        // Custom policy: managers can approve up to $1000, directors unlimited
        var isManager = user?.IsInRole("Manager") ?? false;
        var isDirector = user?.IsInRole("Director") ?? false;

        if (!isManager && !isDirector)
        {
            return ValidationResult.Error("Only managers and directors can approve expenses.");
        }

        if (expense.Amount > 1000 && !isDirector)
        {
            return ValidationResult.Error("Only directors can approve expenses over $1000.");
        }

        return new ExpenseApproved(user?.FindFirstValue(ClaimTypes.Name) ?? "Unknown");
    }
}
```

## Best Practices

### Secure by Default

Apply authorization at the broadest scope possible and override only when necessary. Put `[Authorize]` on the read model so every query method requires authentication, then opt specific methods out with `[AllowAnonymous]`:

```csharp
// Good: secure by default, explicit opt-out
[Authorize]
[ReadModel]
public record Resource(ResourceId Id, string Name)
{
    // Inherits [Authorize] — requires authentication
    public static IEnumerable<Resource> Mine(IMongoCollection<Resource> collection) =>
        collection.Find(_ => true).ToList();

    // Explicitly allow anonymous access for this method
    [AllowAnonymous]
    public static IEnumerable<Resource> Public(IMongoCollection<Resource> collection) =>
        collection.Find(_ => true).ToList();
}
```

### Use Roles Attribute for Clarity

Use `[Roles]` for better readability when specifying multiple roles:

```csharp
// More readable
[Roles("Admin", "Manager", "Supervisor")]
[Command]
public record ReviewApplication(ApplicationId Id)
{
    public ApplicationReviewed Handle() => new();
}

// Less readable
[Authorize(Roles = "Admin,Manager,Supervisor")]
[Command]
public record ReviewApplication(ApplicationId Id)
{
    public ApplicationReviewed Handle() => new();
}
```

### Avoid Ambiguous Authorization

Never apply both `[Authorize]` and `[AllowAnonymous]` to the same target:

```csharp
// ERROR: Will throw AmbiguousAuthorizationLevel
[Authorize]
[AllowAnonymous]
[Command]
public record AmbiguousCommand(string Data)
{
    public OperationCompleted Handle() => new(Data);
}
```

### Document Authorization Requirements

Add XML documentation to clarify authorization requirements:

```csharp
/// <summary>
/// Deletes a user account. Requires Admin role.
/// </summary>
[Roles("Admin")]
[Command]
public record DeleteUser(UserId Id)
{
    public UserDeleted Handle() => new();
}
```

### Validate Claims in Handlers

For complex authorization logic, validate claims inside `Handle()` by injecting `IHttpContextAccessor` and returning a `ValidationResult.Error(...)` when a required claim is missing:

```csharp
using System.Security.Claims;
using Cratis.Arc.Validation;
using Microsoft.AspNetCore.Http;

[Authorize]
[Command]
public record SecureCommand(string Data)
{
    public Result<ValidationResult, SecureOperationCompleted> Handle(
        IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ValidationResult.Error("A user identifier claim is required.");
        }

        // Continue with business logic
        return new SecureOperationCompleted(Data);
    }
}
```

## Integration with Authentication

Authorization works hand-in-hand with authentication. See the [Authentication](authentication.md) documentation for how to implement custom authentication handlers that provide the claims used by authorization.

## Testing Authorization

Authorization runs as part of the real command pipeline, so you test it with [`CommandScenario<TCommand>`](../testing/command-scenario.md) — the same class used for testing validation and handler behavior. Instantiate the scenario, run the command with `Execute`, and assert on the resulting `CommandResult`:

- `ShouldNotBeAuthorized()` — the command was rejected by an `[Authorize]` or `[Roles]` attribute.
- `ShouldBeAuthorized()` — the command passed authorization.
- `ShouldHaveValidationErrors()` / `ShouldHaveValidationErrorFor("message")` — an in-`Handle` guard returned a `ValidationResult.Error`.

For the full scenario API, the assertion reference, and a worked authorization example, see [Command Scenarios](../testing/command-scenario.md) and the wider [Testing](../testing/index.md) guide.

## Next Steps

- [Authentication](authentication.md) - Implement custom authentication handlers
- [Getting Started](getting-started.md) - Learn more about Arc.Core basics
- [Identity](../identity/index.md) - Integrate with Arc's identity system
- [Commands](../commands/index.md) - Learn about command patterns and authorization
- [Queries](../queries/index.md) - Discover query features with authorization
