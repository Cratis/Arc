---
title: Coming from MediatR or MVC
description: If you already build .NET apps with MediatR and ASP.NET Core controllers, here's how those ideas map onto Arc — and what changes.
---

If you've built ASP.NET Core apps with controllers, DTOs, and MediatR, you already know most of the *concepts* in Arc — they just have less ceremony. This page maps what you know onto Arc so you can move fast.

## The one-paragraph version

In MVC you write a controller action that takes a request model, validates it, calls a handler, and returns a response. With MediatR you split that into an `IRequest` and an `IRequestHandler`. **Arc collapses all of that into one thing:** a command is a record with a `Handle()` method on it. There's no controller and no separate handler class — Arc maps the command to an HTTP endpoint and generates a typed client for your frontend.

## How the pieces map

| You know (MediatR / MVC) | In Arc |
|---|---|
| `IRequest<T>` + `IRequestHandler<T>` | A `[Command]` record with `Handle()` defined **on the record** |
| Controller action + routing attributes | Automatic — Arc maps the command/query to HTTP for you |
| Request/response DTOs | The command record itself; the result is what `Handle()` returns |
| FluentValidation / `ModelState` | A `CommandValidator<T>` discovered by convention |
| MediatR pipeline behaviors | The command pipeline and filters |
| `INotification` / handlers | Domain **events** appended to [Chronicle](/chronicle/), observed by [reactors](/chronicle/reactors/) |
| A query action returning a DTO from EF | A **query** method on a read model, served from a [projection](/chronicle/concepts/projection/) |
| Hand-written `fetch`/HttpClient on the frontend | A **generated TypeScript proxy** — no manual client code |

## What stays the same

- You still think in commands and queries — the CQRS split you already use with MediatR is first-class here.
- You still write small, focused handlers and validators.
- You still use dependency injection; constructor-inject collaborators into `Handle()` and into validators.

## What changes (and why it's less code)

- **No controllers.** You don't route, bind, or serialize by hand — the command *is* the endpoint. That removes a whole layer of boilerplate and a common source of drift.
- **No separate handler class.** `Handle()` lives on the command record, so the intent and its implementation sit together in one [vertical slice](/arc/) instead of across `Commands/` and `Handlers/` folders.
- **No second source of truth for the frontend.** The proxy is generated from your C# types, so the client can't fall out of sync — change the command and the compiler flags the frontend.
- **State changes become events, not row updates.** A command's `Handle()` returns the event(s) that happened; Chronicle stores them and projections build the read side. If that part is new to you, read [Why Event Sourcing](/chronicle/why-event-sourcing/).

## A side-by-side

A "register customer" feature in MediatR + MVC is a request record, a handler, a validator, a controller action, and a frontend client call. In Arc it's one slice:

```csharp
[Command]
public record RegisterCustomer(CompanyName Name)
{
    public CustomerRegistered Handle() => new(Name);
}

[EventType]
public record CustomerRegistered(CompanyName Name);
```

Arc exposes this over HTTP and generates the typed client. Your React component calls the generated proxy — no controller, no DTO, no handwritten fetch.

## Where to go next

- Build your first command in the [getting started](/arc/backend/getting-started/) guide.
- See the full command and query options under [Backend](/arc/backend/).
- New to the event-sourced read side? Start with [Why Event Sourcing](/chronicle/why-event-sourcing/).
