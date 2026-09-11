---
title: Authorizing commands and queries
description: Restrict who can run a command or read a query with role-based authorization, applied at the boundary.
---

**Goal:** only certain users may perform an action or see certain data — "only a librarian can register an author," "only an admin sees the audit list." You want that enforced declaratively, not with `if` checks scattered through your logic.

## Authorize at the boundary, not in the logic

Authorization is a cross-cutting concern: it belongs at the edge, applied as an attribute, so your `Handle()` methods and read models stay focused on behavior. Arc enforces role attributes on **both** commands and query methods when invoked through its pipelines. Direct C# calls to `Handle()` or a static query bypass these checks. Use attributes from `Cratis.Arc.Authorization`; `[Roles]` requires authentication and at least one listed role. Arc does not enforce its attribute's `Policy` or `AuthenticationSchemes` properties.

## Protect a command

Put `[Roles(...)]` on the `[Command]` record. Arc checks the caller's roles before the command runs. These are illustrative type fragments using application-owned `AuthorId`, `AuthorName`, `UserRole`, and the optional MongoDB integration; they do not require Chronicle:

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

Query methods on a read model take the same attribute, so the read side is gated too:

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

Roles used by the evaluator come from the authenticated `ClaimsPrincipal`, not the identity cookie. Arc integrates with ASP.NET Core authentication; its lightweight host has its own handlers. `IProvideIdentityDetails` supplies application-specific presentation details but does not add trusted claims or authorize every operation. Read the principal through `ICurrentPrincipalAccessor` from `Cratis.Arc.Authorization`. See the [Identity](./identity/) section for setting that up, and for generating a principal during local development so you can exercise authorized endpoints without a full login.

## Notes

- **Multi-tenancy** requires three separate controls: [tenant selection](./tenancy/index.md), application membership authorization, and integration-specific storage isolation. Selecting a tenant ID alone does not restrict a user to their own data.
- The generated TypeScript proxies respect the same rules — an unauthorized call fails the same way it would from any client.

## See also

- [Identity](./identity/) — authentication, identity details, and local-dev principals.
- [Tenancy](./tenancy/index.md) — isolating data per tenant.
- [Commands](./commands/) and [Queries](./queries/) — the full model.
