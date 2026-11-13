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

## See also

- [Identity](./identity/) — authentication, identity details, and local-dev principals.
- [Tenancy](./tenancy/index.md) — isolating data per tenant.
- [Commands](./commands/) and [Queries](./queries/) — the full model.
