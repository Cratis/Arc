# Provider Flow

Arc identity enrichment is implemented as part of ingress so authorization and detail composition happen in one request.

## Endpoint Mapping

Map the identity provider endpoint in your application:

```csharp
app.MapIdentityProvider();
```

The well-known route is:

- `/.cratis/me`

## Identity Details Provider

Implement `IProvideIdentityDetails` from `Cratis.Arc.Identity`. Arc discovers implementations automatically and invokes them when the identity endpoint is called.

Your provider can:

1. Check whether the user is authorized to access the application
2. Compose domain-specific identity details
3. Return a consolidated identity response

If the user is authorized, Arc writes the result as a base64-encoded JSON payload to the `.cratis-identity` cookie.

## Implementation Example

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

## Frontend Integration

Frontend identity support can consume the `.cratis-identity` cookie directly, avoiding a follow-up request for user details.

For frontend details, see [React identity integration](../../frontend/react/identity.md).
