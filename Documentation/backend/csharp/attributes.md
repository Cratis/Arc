---
title: Attribute reference
description: Cratis Arc C# attributes control command and query discovery, routing, authorization and validation, with optional Chronicle integration.
---

Arc discovers what your application exposes from attributes on your own types.
This is the whole set you write, grouped by what it concerns. Attributes that
come from Chronicle or Fundamentals rather than Arc are marked as such, because
they arrive with a different package.

For Kotlin and Java annotations and their contracts, see the
[annotation reference](/arc/backend/kotlin/reference/annotations/).

## Artifacts

| Attribute | Valid on | Effect |
|---|---|---|
| `[Command]` | class | Marks a record or class as a command. Arc exposes it, validates and authorizes it, and calls its `Handle()` method. |
| `[ReadModel]` | class | Marks a type as a read model. Eligible public or internal static methods become queries; private helpers do not. |

```csharp
[Command]
public record RegisterAuthor(AuthorId Id, AuthorName Name)
{
    public Task Handle(IMongoCollection<Author> authors) =>
        authors.InsertOneAsync(new Author(Id, Name));
}
```

## Routing and transport

| Attribute | Valid on | Effect |
|---|---|---|
| `[Path]` (`Cratis.Arc.Queries.ModelBound`) | model-bound command, read model or query method | Overrides the conventional HTTP route. A command's `/validate` endpoint uses the same path plus `/validate`. The explicit path is not prefixed or lowercased. |
| `[QueryHttpMethod]` | read model, query method | Sets the HTTP method the **generated proxy** uses by default (GET, QUERY, or Auto). The server accepts GET and, unless `GeneratedApis.EnableQueryHttpMethod` is `false`, QUERY regardless of this attribute. |
| `[FromRequest]` | parameter, property | Binds a value from several request sources rather than the default one. |
| `[ExplicitOffset]` (`Cratis.Arc.Commands`) | property, field | Requires JSON DateTime/DateTimeOffset input to include an explicit offset or Z. Nullable forms accept null. Use `[property: ExplicitOffset]` on a positional record; see [input binding](commands/input-binding.md). |

A route you do not override is derived, so `[Path]` is for the cases where the
derived route is wrong for you — not something to apply everywhere. A command's explicit
`[Path("/api/orders/create")]` stays stable even if another command is added to its
namespace; its generated TypeScript proxy calls that same path. Import
`Cratis.Arc.Queries.ModelBound` for both commands and queries. A command's path must
be declared on that command type, not inherited from a base type. Avoid reusing
an explicit path for another command or for a conventional command route; route
collisions can make endpoints ambiguous. A command path is a literal route: route
templates such as `{id}` are not supported for commands, and it should not end with `/`
(the validation endpoint appends `/validate`).

## Discovery and API descriptions

| Attribute | Valid on | Effect |
|---|---|---|
| `[ExcludeFromDiscovery]` (`Cratis.Arc`) | command type, read model type, query method, controller or controller action | Omits the marked operation from `/.cratis/commands` or `/.cratis/queries` and generated OpenAPI descriptions (including command validation endpoints). |
| `[assembly: ArtifactsRequireOptIn]` (`Cratis.Arc`) | assembly | Does not serve the assembly's model-bound commands or read-model queries unless the host includes the assembly through `ArcOptions.Artifacts.Include(assembly)`. They are not mapped, catalogued, described or included in artifact startup validation. |

The marker is inherited: marking a base command type or read model also hides every type
that derives from it. This differs from a command's `[Path]`, which is not inherited.

This marker **does not unmap the HTTP endpoint or authorize it**. Protect it with
`[Authorize]`, a trusted ingress, or both; do not rely on hiding metadata as a
security boundary. Controller-based commands are not in the command catalog, but
marked controller actions are omitted from MVC's API descriptions.

Use `[assembly: ArtifactsRequireOptIn]` when a library's commands and queries belong
in its own private container, such as Chronicle's kernel, rather than every host
that references it. Unlike `[ExcludeFromDiscovery]`, this marker removes endpoints,
not just their descriptions. Hosts that serve the library explicitly
[include its assembly in code](configuration/index.md#including-a-private-librarys-artifacts).
Controller actions, proxy generation, and other extension points discovered from
the assembly, such as filters and validators, are not affected. Unmarked assemblies
keep their existing behavior.

## Authorization

| Attribute | Valid on | Effect |
|---|---|---|
| `[Authorize]` | class, method | Requires an authenticated caller, optionally satisfying a policy. |
| `[Roles]` | class, method | Requires **at least one** of the listed roles. |
| `[AllowAnonymous]` | class, method | Opens a specific artifact or operation back up. |

These attributes are in `Cratis.Arc.Authorization`. Apply them at class level
for a shared declaration; an explicit method declaration replaces the class
declaration. Every authorization attribute on the effective declaration must
pass. Combining `[AllowAnonymous]` with a restriction on the same declaration
is rejected as ambiguous.

Without an explicit declaration, Arc consults any
`IFallbackAuthorizationEvaluator` implementations for baseline requirements.
Without those requirements, the pipeline allows any caller. An ASP.NET Core
[fallback policy](asp-net-core/authorization.md) protects the HTTP boundary
separately; it does not protect in-process pipeline calls. An authorization
denial surfaces as `isAuthorized: false` in the result rather than as an
exception, so the frontend can react to it.

## Validation

| Attribute | Valid on | Effect |
|---|---|---|
| `[IgnoreValidation]` | controller, action | Skips Arc's validation filter for an ASP.NET Core controller or action, leaving ASP.NET Core's default model validation behavior. It has no effect on model-bound commands and queries. |

Most validation is declared in a `CommandValidator<T>` or `ConceptValidator<T>`
rather than with an attribute. See [validation](commands/validation.md).

## Hosting and registration

| Attribute | Valid on | Effect |
|---|---|---|
| `[IgnoreAutoRegistration]` | class | Excludes a type from automatic registration mechanisms that honor this marker. |
| `[AspNetResult]` | controller, action | Returns an ASP.NET Core controller action's result directly instead of wrapping it in Arc's result envelope. It applies to controller-based endpoints only. |

`[AspNetResult]` opts out of the envelope the generated client expects, so a
proxy will not consume that endpoint in the usual way. Reach for it when an
endpoint exists for something other than your own frontend.

## With the Chronicle integration

These arrive with Chronicle rather than Arc itself, and only apply when the
[Chronicle integration](chronicle/index.md) is in use.

| Attribute | Valid on | Effect |
|---|---|---|
| `[EventType]` | class | Marks a record as an event. Takes no arguments for a new event. |
| `[Key]` (`Cratis.Chronicle.Keys`) | property, positional record parameter | Identifies the event source a command acts on. Without Chronicle, Arc instead reads `System.ComponentModel.DataAnnotations.KeyAttribute` as the command key; Chronicle ignores that one, so unless the command has an `EventSourceId`-typed property, it can silently resolve no event source; [ARCCHR0008](chronicle/code-analysis/ARCCHR0008.md) warns about it. |
| `[NotAudited]` | class, struct, property, parameter | Keeps a secret out of the causation chain written with every event. |
| `[ExecuteCommandsAsSystem]` | class | Runs a reactor's returned commands without a user principal. |

`[NotAudited]` is the one worth knowing before you need it: a command's property
values are recorded in the causation chain of every event it appends, so an
unmarked token or password reaches the event log in clear text and stays there.
It is not interchangeable with Chronicle's `[PII]`, which encrypts and enrolls a
value in erasure — that is the right marking for personal data and the wrong one
for a secret.

## Related

- [Code analysis](code-analysis/index.md) — the analyzers that check these are used correctly.
- [Commands](commands/index.md) and [Queries](queries/index.md) — what the artifact attributes actually enable.
