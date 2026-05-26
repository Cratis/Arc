# Identity

Arc identity support lets you enrich provider tokens with domain-specific user details, perform application-level authorization at ingress, and propagate a consistent identity payload to frontend clients.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Overview](./overview.md) | What the identity system provides and why to use it. |
| [How It Works](./how-it-works.md) | Request flow from provider token to Arc identity cookie. |
| [Identity Details Provider](./identity-details-provider.md) | Implementing `IProvideIdentityDetails` and mapping the endpoint. |
| [Implementation Example](./implementation-example.md) | Minimal provider implementation example. |
| [Identity Provider Context](./identity-provider-context.md) | Available context properties from the incoming identity token. |
| [Identity Details](./identity-details.md) | Authorization and detail payload returned by your provider. |
| [Identity Endpoint](./identity-endpoint.md) | The well-known identity endpoint route. |
| [Development Users and Tenants Endpoints](./development-users-and-tenants-endpoints.md) | Development-only endpoints for suggested users and tenants. |
| [Frontend Integration](./frontend-integration.md) | How frontend clients consume the Arc identity cookie. |
| [IdentityProvider Service](./identity-provider-service.md) | Advanced runtime identity retrieval and modification with `IIdentityProvider`. |
| [Multi-Service Considerations](./multi-service-considerations.md) | Patterns for single-service and microservice identity composition. |
