---
title: "Cratis Arc authorization: protect commands and queries"
description: Protect Cratis Arc commands and queries with role or policy attributes, and use the command and query pipelines to enforce access checks.
---

Cratis Arc checks role and policy attributes when a command or query runs through its pipeline. Use them when an action or a read needs an access rule, such as allowing only librarians to register authors. Direct C# calls to `Handle()` or a static query bypass these checks.

Authorization says who may do something. To decide whether a part of the application is there at all for a tenant or user, use [availability](availability.md), which is evaluated before authorization and reports a different reason.

## Authorize at the boundary, not in the logic

Put the access rule on the command record or query method, using attributes from `Cratis.Arc.Authorization`. `[Roles]` requires an authenticated caller with at least one listed role, so your handler does not need to repeat that role check.

For a named policy, use `[Authorize(Policy = "...")]` and [register the policy](core/authorization.md#register-a-named-policy). It requires authentication unless the registration explicitly opts into guest evaluation. Named `AuthenticationSchemes` work only on the [ASP.NET Core host](asp-net-core/authorization.md); the standalone Arc host fails at startup rather than ignoring them.

## Protect a command

Put `[Roles(...)]` on the `[Command]` record to check the caller before the command runs. This fragment uses application-owned `AuthorId`, `AuthorName`, and `UserRole` types with the optional MongoDB integration. It does not require Chronicle:

```csharp
[Command]
[Roles(nameof(UserRole.Librarian))]
public record RegisterAuthor(AuthorId Id, AuthorName Name)
{
    public Task Handle(IMongoCollection<Author> authors) =>
        authors.InsertOneAsync(new Author(Id, Name));
}
```

:::tip[Keep authorization at the command boundary]
The direct insert remains supported. For inline writes that can be declared before execution, prefer a pure `Handle()` returning a [command operation](./commands/operations/index.md), with the write performed by `Execute()`. Keep `[Roles]` on the command; operations do not replace pipeline authorization.
:::

## Protect a query

Apply the same attribute to a query method to restrict who can read its result:

```csharp
[ReadModel]
public record Author([property: Key] AuthorId Id, AuthorName Name)
{
    [Roles(nameof(UserRole.Librarian))]
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> collection) =>
        collection.Observe();
}
```

## Who the user is

The evaluator reads roles from the authenticated `ClaimsPrincipal`. Arc integrates with ASP.NET Core authentication, while its lightweight host has its own handlers. Read the principal through `ICurrentPrincipalAccessor` from `Cratis.Arc.Authorization`.

Identity details serve a different purpose: `IProvideIdentityDetails` supplies application-specific presentation data, not trusted claims or permission to run an operation. Continue with [Identity](./identity/) to configure authentication or supply a principal during local development.

## Notes

Selecting a tenant ID is not permission to access that tenant's data. A multi-tenant application also needs membership authorization and integration-specific storage isolation; [tenant selection](./tenancy/index.md) covers only the selection step.

Generated TypeScript proxies call the same protected endpoints as other clients. Access checks happen on the server, not in the proxy.

## Security defaults today

These defaults are open until you change them:

| Area | Default | Effect |
| --- | --- | --- |
| Commands and queries without an authorization attribute | No pipeline authentication or role requirement | Any caller that can reach the endpoint can run them, unless the host's default authorization or a fallback evaluator says otherwise. |
| Read protection | `Off` when `<CratisReadProtection>` is unset | Injected read models are plain reads and guard nothing. See [read protection](./chronicle/consistency/index.md). |
| `Cratis:Arc:Query:AllowedWebSocketOrigins` | Not set | Observable query WebSocket routes accept any browser origin. |
| `Cratis:Arc:TrustForwardedIdentityHeaders` | `false` | `AddCratis()` authenticates nobody. This one is closed. |
| Discovery endpoints (`Cratis:Arc:Introspection`) | Enabled; anonymous in Development, authentication required elsewhere; `Roles` not set | Outside Development, any signed-in user can read the discovery catalogs. Set `Cratis:Arc:Introspection:Roles` to limit them to named roles, or turn discovery off. |

## Production checklist

Before you deploy, go through this list:

1. Put `[Authorize]`, `[Roles(...)]` or a policy on every command and query that is not public, or add `[AllowAnonymous]` where you mean it. Do not rely on the absence of an attribute.
2. Register a real authentication scheme, or set `Cratis:Arc:TrustForwardedIdentityHeaders` to `true` only behind an ingress that authenticates callers and strips identity headers. See [Microsoft Identity](./asp-net-core/microsoft-identity.md#trusting-the-forwarded-headers).
3. Set `Cratis:Arc:Query:AllowedWebSocketOrigins` to your front-end origins. See [Restricting WebSocket origins](./queries/observable-query-demultiplexer.md#restricting-websocket-origins).
4. Choose a read-protection mode with `<CratisReadProtection>` per project. Use `Measure` first, then `On`. See [Adopting read protection](./chronicle/consistency/adopting.md).
5. Leave `ExposeExceptionDetails` `false`. Check that the host runs with `ASPNETCORE_ENVIRONMENT` set to the value you expect; see [Environment detection](./configuration/keys.md#how-arc-detects-development).
6. Turn off discovery you do not need: set `Cratis:Arc:Introspection:Enabled` and `IdentityDiscovery` to `false`. See [Introspection production access](./introspection/index.md#production-access).
7. Set `Cratis:Arc:Tenancy:StrictValidatorMembership` to `true` if the application is multi-tenant.
8. Keep MongoDB `LogCommandBodies` off and keep secrets in secret configuration.
9. Check every key in [Configuration keys](./configuration/keys.md) that your application uses.

## See also

- [Identity](./identity/) — authentication, identity details, and local-dev principals.
- [Tenancy](./tenancy/index.md) — isolating data per tenant.
- [Commands](./commands/) and [Queries](./queries/) — the full model.
