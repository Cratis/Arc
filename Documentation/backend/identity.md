# Identity

Cratis Arc provides a powerful identity system that allows you to provide additional details for logged-in users beyond what's available in identity provider tokens. This system is designed to work seamlessly with your application's ingress flow and can be used with any identity provider.

## Overview

Identity tokens from providers typically contain limited information. The Arc's identity system allows you to:

- Add domain-specific information to user identities
- Perform application-level authorization checks
- Compose identity details at the ingress level
- Provide consistent identity information across microservices

## How It Works

The identity system works by allowing you to implement a provider that enriches the basic identity information with application-specific details. This provider is called during the authentication flow and can:

1. Verify if the user is authorized to access your application
2. Add custom properties and details specific to your domain
3. Return a consolidated identity object that your application can use

## Identity Details Provider

As part of your ingress flow, you can provide additional details for logged in users. On the tokens coming from your identity provider you only have
limited amounts of information and sometimes you want to have more domain specific information.

There are also cases were you need to ask the application if the user is at all authorized to enter the application.

Typically, you would like your ingress to do the composition of this information and create the HTTP headers and cookies needed for this in a single
request without having the frontend do a second request to the server to get more details. And also for the authorization part, you'd like that to happen
before you enter your application and return not authorized if your application is not allowing entry.

If the user is authorized, the Arc Identity Provider endpoint will put the result as a base64 encoded JSON string on a cookie called `.cratis-identity`. This cookie is then
automatically picked up by the frontend, read more about [frontend identity integration](../frontend/react/identity.md). The frontend will then use the cookie if present.

To leverage this mechanism, simply map the endpoint to your application:

```csharp
app.MapIdentityProvider();
```

Once this is done you would simply add code to one of your microservices in your application that provides the additional identity details. You simply
implement the `IProvideIdentityDetails` interface found in the `Cratis.Arc.Identity` namespace. It will automatically be discovered and
called when needed.

This is unwrapped by Arc and encapsulates it into what is called a `IdentityProviderContext` for you as a developer to consume in a type-safe
manner.

> Note: If your application has just one microservice, you let it implement the `IProvideIdentityDetails` interface.
> For multiple microservices you might want to consider letting your ingress / reverse proxy call all your microservices and merge the results together
> in one single JSON structure.

## Implementation Example

Below is an example of an implementation:

```csharp
public class IdentityDetailsProvider : IProvideIdentityDetails
{
    public Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var result = new IdentityDetails(true, new { Hello = "World" });
        return Task.FromResult(result);
    }
}
```

## IdentityProviderContext

The `IdentityProviderContext` holds the following properties:

| Property | Description |
| -------- | ----------- |
| Id | The identity identifier specific from from the identity provider |
| Name | The name of the identity |
| Token | Parsed principal data definition represented as a `JsonObject`|
| Claims | Collection of `KeyValuePair<string, string>` of the claims found in the token |

## IdentityDetails

The code then returns `IdentityDetails` which holds the following properties:

| Property | Description |
| -------- | ----------- |
| IsUserAuthorized | Whether or not the user is authorized into your application or not |
| Details | The actual details in the form of an object, letting you create your own structure |

If the `IsUserAuthorized` property is set to false the return from this will be an HTTP 403. While if it is authorized, a regular HTTP 200.

> Note: Dependency inversion works for this, so your provider can take any dependencies it wants on its constructor.

## Endpoint

Your provider will be exposed on a well known route: `/.cratis/me`.

## Integration with Frontend

The identity system seamlessly integrates with the frontend by setting a cookie that can be automatically consumed by your client-side application. This eliminates the need for separate API calls to retrieve user details after authentication.

## IdentityProvider

The `IIdentityProvider` interface provides advanced control over how identity information is processed and modified during HTTP requests. This is particularly useful for stateless applications where you need to associate user-specific data with requests.

### Purpose

The `IIdentityProvider` allows you to:

- Get identity results from the identity cookie or current HTTP context
- Write identity information to the response (as cookies and JSON)
- **Modify identity details during a request** - perfect for stateless applications that need to associate selections or preferences with the current user

### Key Methods

| Method | Description |
| ------ | ----------- |
| `Get()` | Gets an `IdentityProviderResult` from the identity cookie when available, otherwise from the current HTTP context |
| `Get<TDetails>()` | Gets an `IdentityProviderResult<TDetails>` with strongly-typed details |
| `SetCookieForHttpResponse(IdentityProviderResult)` | Writes the identity result to the response as both a cookie and JSON |
| `ModifyDetails<TDetails>(Func<TDetails, TDetails>)` | Modifies the identity details stored in the identity cookie |

### Use Cases

#### Modifying User Preferences in Stateless Applications

In stateless applications, you often need to associate user selections or preferences with the current request. The `ModifyDetails` method allows you to update the identity cookie with new information:

```csharp
public class UserPreferencesController : ControllerBase
{
    private readonly IIdentityProvider _identityHandler;

    public UserPreferencesController(IIdentityProvider identityHandler)
    {
        _identityHandler = identityHandler;
    }

    [HttpPost("set-department")]
    public async Task<IActionResult> SetDepartment([FromBody] string department)
    {
        // Modify the identity details to include the selected department
        await _identityHandler.ModifyDetails<UserDetails>(details =>
            details with { SelectedDepartment = department });

        return Ok();
    }
}
```

This approach is particularly valuable for stateless applications where you need to maintain user-specific context across requests without server-side sessions.

## Multi-Service Considerations

In a microservices architecture, you have several options for implementing identity details:

1. **Single Service**: Implement `IProvideIdentityDetails` directly in your main service
2. **Multiple Services**: Have your ingress/reverse proxy call multiple services and merge the results
3. **Dedicated Identity Service**: Create a specialized service that aggregates identity information from various sources

Choose the approach that best fits your application's architecture and requirements.
