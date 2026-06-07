---
title: Authorizing commands and queries
description: Restrict who can run a command or read a query with role-based authorization, applied at the boundary.
---

**Goal:** only certain users may perform an action or see certain data — "only a librarian can register an author," "only an admin sees the audit list." You want that enforced declaratively, not with `if` checks scattered through your logic.

## Authorize at the boundary, not in the logic

Authorization is a cross-cutting concern: it belongs at the edge, applied as an attribute, so your `Handle()` methods and read models stay focused on behavior. Arc enforces role attributes on **both** commands and query methods.

## Protect a command

Put `[Roles(...)]` on the `[Command]` record. Arc checks the caller's roles before the command runs:

```csharp
[Command]
[Roles(nameof(UserRole.Librarian))]
public record RegisterAuthor(AuthorId Id, AuthorName Name)
{
    public Task Handle(IMongoCollection<Author> authors) =>
        authors.InsertOneAsync(new Author(Id, Name));
}
```

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

Roles come from the authenticated identity. Arc integrates with standard ASP.NET Core authentication and can enrich the identity with application-specific details (roles, tenant, preferences) through `IProvideIdentityDetails`. See the [Identity](./identity/) section for setting that up, and for generating a principal during local development so you can exercise authorized endpoints without a full login.

## Notes

- **Multi-tenancy** narrows access further: combine roles with [tenancy](./tenancy/overview) so a user only ever sees their tenant's data.
- The generated TypeScript proxies respect the same rules — an unauthorized call fails the same way it would from any client.

## See also

- [Identity](./identity/) — authentication, identity details, and local-dev principals.
- [Tenancy](./tenancy/overview) — isolating data per tenant.
- [Commands](./commands/) and [Queries](./queries/) — the full model.
