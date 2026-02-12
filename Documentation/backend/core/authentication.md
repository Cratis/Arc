# Authentication

Arc.Core provides a flexible authentication system that allows you to implement custom authentication handlers for your application. This is particularly useful for scenarios where you need to authenticate requests based on custom headers, tokens, or other mechanisms without relying on ASP.NET Core's authentication middleware.

## Overview

The authentication system in Arc.Core is built around the `IAuthenticationHandler` interface. Multiple authentication handlers can be registered, and they're executed in sequence until one successfully authenticates the request or returns a failure.

## Authentication Flow

The authentication system processes handlers in sequence:

1. Each registered `IAuthenticationHandler` is called in order
2. If a handler returns an authenticated result, the process stops and that result is used
3. If a handler returns a failure, the process stops and the failure is returned
4. If a handler returns anonymous, the next handler is tried
5. If all handlers return anonymous, the request is considered anonymous

## Authentication Results

Authentication handlers return an `AuthenticationResult` with one of three possible outcomes:

| Outcome | Description | Usage |
|---------|-------------|-------|
| **Succeeded** | Authentication was successful | Return `AuthenticationResult.Succeeded(principal)` with a `ClaimsPrincipal` |
| **Failed** | Authentication failed with a reason | Return `AuthenticationResult.Failed(reason)` with a failure reason |
| **Anonymous** | Handler cannot authenticate this request | Return `AuthenticationResult.Anonymous` to let other handlers try |

## Implementing an Authentication Handler

Here's a basic example of implementing a custom authentication handler:

```csharp
using System.Security.Claims;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;

public class ApiKeyAuthenticationHandler : IAuthenticationHandler
{
    const string ApiKeyHeader = "X-API-Key";
    
    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        // Check if the API key header is present
        if (!context.Headers.TryGetValue(ApiKeyHeader, out var apiKey))
        {
            // No API key present, let other handlers try
            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        // Validate the API key
        if (!IsValidApiKey(apiKey))
        {
            // Invalid API key, fail authentication
            return Task.FromResult(
                AuthenticationResult.Failed(
                    new AuthenticationFailureReason("Invalid API key")));
        }

        // Create a claims principal for the authenticated user
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "API User"),
            new Claim(ClaimTypes.NameIdentifier, "api-user-123"),
            new Claim("api_key", apiKey)
        };

        var identity = new ClaimsIdentity(claims, "ApiKey");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticationResult.Succeeded(principal));
    }

    bool IsValidApiKey(string apiKey)
    {
        // Your API key validation logic
        return apiKey == "your-secret-api-key";
    }
}
```

## Common Authentication Patterns

### Bearer Token Authentication

```csharp
public class BearerTokenAuthenticationHandler : IAuthenticationHandler
{
    const string AuthorizationHeader = "Authorization";
    const string BearerPrefix = "Bearer ";

    public async Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        if (!context.Headers.TryGetValue(AuthorizationHeader, out var authHeader))
        {
            return AuthenticationResult.Anonymous;
        }

        if (!authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticationResult.Anonymous;
        }

        var token = authHeader[BearerPrefix.Length..].Trim();

        try
        {
            var principal = await ValidateAndDecodeToken(token);
            return AuthenticationResult.Succeeded(principal);
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failed(
                new AuthenticationFailureReason($"Token validation failed: {ex.Message}"));
        }
    }

    async Task<ClaimsPrincipal> ValidateAndDecodeToken(string token)
    {
        // Your token validation logic (e.g., JWT validation)
        // This is a simplified example
        await Task.CompletedTask;
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-id"),
            new Claim(ClaimTypes.Name, "User Name")
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }
}
```

### Basic Authentication

```csharp
public class BasicAuthenticationHandler : IAuthenticationHandler
{
    const string AuthorizationHeader = "Authorization";
    const string BasicPrefix = "Basic ";

    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        if (!context.Headers.TryGetValue(AuthorizationHeader, out var authHeader))
        {
            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        if (!authHeader.StartsWith(BasicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        var encodedCredentials = authHeader[BasicPrefix.Length..].Trim();
        var credentials = Encoding.UTF8.GetString(
            Convert.FromBase64String(encodedCredentials));
        
        var parts = credentials.Split(':', 2);
        if (parts.Length != 2)
        {
            return Task.FromResult(
                AuthenticationResult.Failed(
                    new AuthenticationFailureReason("Invalid credentials format")));
        }

        var username = parts[0];
        var password = parts[1];

        if (!ValidateCredentials(username, password))
        {
            return Task.FromResult(
                AuthenticationResult.Failed(
                    new AuthenticationFailureReason("Invalid username or password")));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, username)
        };

        var identity = new ClaimsIdentity(claims, "Basic");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticationResult.Succeeded(principal));
    }

    bool ValidateCredentials(string username, string password)
    {
        // Your credential validation logic
        return username == "admin" && password == "secret";
    }
}
```

### Custom Header Authentication

```csharp
public class CustomHeaderAuthenticationHandler : IAuthenticationHandler
{
    const string UserIdHeader = "X-User-ID";
    const string UserRoleHeader = "X-User-Role";

    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        if (!context.Headers.TryGetValue(UserIdHeader, out var userId))
        {
            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        var role = context.Headers.TryGetValue(UserRoleHeader, out var roleValue) 
            ? roleValue 
            : "User";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "CustomHeader");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticationResult.Succeeded(principal));
    }
}
```

## Registering Authentication Handlers

Authentication handlers are automatically discovered and registered by Arc.Core. Simply ensure your handler implements `IAuthenticationHandler` and is in a discoverable location:

```csharp
// The handler will be automatically registered
public class MyAuthenticationHandler : IAuthenticationHandler
{
    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        // Implementation
    }
}
```

If you need manual registration:

```csharp
builder.Services.AddSingleton<IAuthenticationHandler, MyAuthenticationHandler>();
```

## Multiple Authentication Handlers

You can register multiple authentication handlers, and they'll be executed in sequence:

```csharp
public class ApiKeyHandler : IAuthenticationHandler { /* ... */ }
public class BearerTokenHandler : IAuthenticationHandler { /* ... */ }
public class BasicAuthHandler : IAuthenticationHandler { /* ... */ }
```

The handlers are tried in order until one returns either:
- A successful authentication result
- A failed authentication result

If all handlers return `Anonymous`, the request is considered anonymous.

## Handler Execution Order

Handlers are executed in the order they're discovered or registered. To control order, you can use explicit registration:

```csharp
// Register in specific order
builder.Services.AddSingleton<IAuthenticationHandler, PrimaryAuthHandler>();
builder.Services.AddSingleton<IAuthenticationHandler, FallbackAuthHandler>();
```

## Working with Request Context

Authentication handlers receive an `IHttpRequestContext` that provides access to request information including headers, query parameters, URL, and HTTP method.

## Best Practices

### Return Anonymous for Non-Applicable Requests

If your handler doesn't apply to a request, return `Anonymous` to let other handlers try:

```csharp
if (!context.Headers.ContainsKey("X-My-Auth-Header"))
{
    return Task.FromResult(AuthenticationResult.Anonymous);
}
```

### Provide Clear Failure Reasons

When authentication fails, provide clear, actionable error messages:

```csharp
return AuthenticationResult.Failed(
    new AuthenticationFailureReason("API key is expired. Please generate a new key."));
```

### Use Dependency Injection

Handlers can use dependency injection for services they need:

```csharp
public class JwtAuthenticationHandler(
    ILogger<JwtAuthenticationHandler> logger,
    ITokenValidator tokenValidator) : IAuthenticationHandler
{
    public async Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        logger.LogDebug("Validating JWT token");
        // Use injected services
    }
}
```

### Handle Exceptions Gracefully

Catch and handle exceptions within your handler:

```csharp
try
{
    var principal = await ValidateToken(token);
    return AuthenticationResult.Succeeded(principal);
}
catch (SecurityTokenException ex)
{
    return AuthenticationResult.Failed(
        new AuthenticationFailureReason($"Token validation failed: {ex.Message}"));
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error during authentication");
    return AuthenticationResult.Failed(
        new AuthenticationFailureReason("Authentication error occurred"));
}
```

## Integration with Authorization

Once a request is authenticated, the `ClaimsPrincipal` is available for authorization checks. See the [Authorization](authorization.md) documentation for how to protect endpoints using the `[Authorize]` and `[Roles]` attributes.

## Testing Authentication Handlers

When testing authentication handlers, use the `IHttpRequestContext` interface:

```csharp
public class ApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task should_authenticate_with_valid_api_key()
    {
        var handler = new ApiKeyAuthenticationHandler();
        var context = new TestHttpRequestContext
        {
            Headers = new Dictionary<string, string>
            {
                ["X-API-Key"] = "valid-key"
            }
        };

        var result = await handler.HandleAuthentication(context);

        result.IsAuthenticated.ShouldBeTrue();
    }
}
```

## Next Steps

- [Authorization](authorization.md) - Learn how to protect endpoints with authorization attributes
- [Identity](../identity.md) - Integrate with Arc's identity system
- [Commands](../commands/index.md) - Protect commands with authentication and authorization
- [Queries](../queries/index.md) - Protect queries with authentication and authorization
