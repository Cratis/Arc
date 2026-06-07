---
title: MediatR, MVC, and Arc
description: If you already build .NET apps with MediatR and ASP.NET Core controllers, here's how those ideas map onto Arc.
---

If you've built ASP.NET Core apps with controllers, DTOs, and MediatR, you already know most of the *concepts* in Arc. This page maps those familiar pieces onto Arc's command/query model.

## The one-paragraph version

In MVC, a controller action takes a request model, validates it, calls a handler, and returns a response. With MediatR, that often becomes an `IRequest` and an `IRequestHandler`. In Arc, the command is a record with a `Handle()` method on it; Arc maps that command to an HTTP endpoint and generates a typed client for your frontend.

## How the pieces map

| You know (MediatR / MVC) | In Arc |
|---|---|
| `IRequest<T>` + `IRequestHandler<T>` | A `[Command]` record with `Handle()` defined **on the record** |
| Controller action + routing attributes | Automatic — Arc maps the command/query to HTTP for you |
| Request/response DTOs | The command record itself; the result is what `Handle()` returns |
| FluentValidation / `ModelState` | A `CommandValidator<T>` discovered by convention |
| MediatR pipeline behaviors | The command pipeline and filters |
| `INotification` / handlers | A follow-up command, domain service, or optional [Chronicle reactor](/arc/backend/chronicle/) when you are event-sourcing |
| A query action returning a DTO from EF | A **query** method on a `[ReadModel]`, served directly over HTTP |
| Application-specific `fetch`/HttpClient on the frontend | A **generated TypeScript proxy** |

## What stays the same

- You still think in commands and queries — the CQRS split you already use with MediatR is first-class here.
- You still write small, focused handlers and validators.
- You still use dependency injection; constructor-inject collaborators into `Handle()` and into validators.

## What changes

- **Commands are endpoints.** The command record carries the request shape and the `Handle()` method; Arc maps it to HTTP.
- **The handler sits with the intent.** `Handle()` lives on the command record, so the intent and its implementation sit together in one [vertical slice](/arc/) instead of across `Commands/` and `Handlers/` folders.
- **The frontend model is generated.** The proxy is generated from your C# types, so command/query shape changes are caught by TypeScript.
- **The read side is explicit.** Instead of returning arbitrary DTOs from controllers, you name the read model and expose query methods on it. Those methods are generated into the frontend just like commands.

## A side-by-side

A "register customer" feature in MediatR + MVC is often a request record, a handler, a validator, a controller action, and a frontend client call. In Arc, the same feature is modeled as a slice:

```csharp
[Command]
public record RegisterCustomer(CustomerId Id, CompanyName Name)
{
    public Task Handle(IMongoCollection<Customer> customers) =>
        customers.InsertOneAsync(new Customer(Id, Name));
}

[ReadModel]
public record Customer([property: Key] CustomerId Id, CompanyName Name)
{
    public static ISubject<IEnumerable<Customer>> AllCustomers(IMongoCollection<Customer> customers) =>
        customers.Observe();
}
```

Arc exposes both members over HTTP and generates the typed client. Your React component calls the generated command and query proxies; no controller, DTO mapper, or hand-written fetch layer sits between them.

## Where to go next

- Build your first command in the [getting started](/arc/backend/getting-started/) guide.
- See the full command and query options under [Backend](/arc/backend/).
- Need history, replay, or reactors later? The [Chronicle integration](/arc/backend/chronicle/) shows how Arc adds event sourcing as an optional write-side choice.
