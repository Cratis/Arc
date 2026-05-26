# Identity Details Provider

As part of your ingress flow, you can provide additional details for logged-in users. Provider tokens often include limited information, and you may need domain-specific details or a server-side authorization decision before allowing access.

If the user is authorized, the Arc Identity Provider endpoint stores the result as a base64-encoded JSON string in a cookie named `.cratis-identity`. The frontend can then consume this cookie directly. For frontend details, see [React identity integration](../../frontend/react/identity.md).

To enable the mechanism, map the endpoint in your application:

```csharp
app.MapIdentityProvider();
```

Implement `IProvideIdentityDetails` from `Cratis.Arc.Identity`. Arc discovers implementations automatically and calls them as needed.

Arc unwraps the incoming identity information and provides it as an `IdentityProviderContext` for type-safe access.

> Note: If your application has one microservice, let it implement `IProvideIdentityDetails` directly. For multiple microservices, you might let ingress or reverse proxy aggregate identity details from several services into one JSON payload.
