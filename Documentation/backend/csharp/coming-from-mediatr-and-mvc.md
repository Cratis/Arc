---
title: Adopt Cratis Arc from MediatR and ASP.NET Core MVC
description: Map MediatR requests, handlers, and MVC controller actions to Cratis Arc commands and queries, while keeping your business logic and storage choices.
---

Cratis Arc brings a request's inputs and handler together in a model-bound command record, then exposes it over HTTP. If you use MediatR and ASP.NET Core MVC, start with one feature: move its request and handler into that record, expose its reads as query methods, and configure proxy generation for the frontend. Your business rules and storage choices remain yours.

## The one-paragraph version

An MVC action often accepts a data transfer object (DTO), validates it, calls a handler, and returns a response. MediatR separates that request into an `IRequest` and an `IRequestHandler`. With Arc's model-bound style, `Handle()` lives on the command record. Arc discovers the record and maps its endpoint; a configured post-build tool generates the TypeScript proxy. This changes how you expose the feature, not what its business logic must do.

## How the pieces map

| You know (MediatR / MVC)                                | In Arc                                                                                                                    |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `IRequest<T>` + `IRequestHandler<T>`                    | A `[Command]` record with `Handle()` defined **on the record**                                                            |
| Controller action + routing attributes                  | Arc discovers commands and queries and maps HTTP endpoints                                                               |
| Request/response DTOs                                   | The command record itself; the result is what `Handle()` returns                                                          |
| FluentValidation / `ModelState`                         | A `CommandValidator<T>` discovered by convention                                                                          |
| MediatR pipeline behaviors                              | The command pipeline and filters                                                                                          |
| `INotification` / handlers                              | A follow-up command, domain service, or optional [Chronicle reactor](/arc/backend/csharp/chronicle/) when you are event-sourcing |
| A query action returning a DTO from EF                  | A **query** method on a `[ReadModel]`, served directly over HTTP                                                          |
| Application-specific `fetch`/HttpClient on the frontend | A **generated TypeScript proxy**                                                                                          |

## What stays the same

- You can keep commands that change state separate from queries that read it: the Command Query Responsibility Segregation (CQRS) pattern.
- You still write small, focused handlers and validators.
- You still use dependency injection; declare collaborators as `Handle()` / `Provide()` method parameters, and constructor-inject them into validators. Command constructor parameters describe request data, not services.

## What changes

The command record carries both the request shape and `Handle()`, so you no longer need a controller action just to forward the request. Put the command and its related artifacts in one [vertical slice](../../vertical-slices.mdx), a folder organized around a feature rather than around technical roles such as `Commands/` and `Handlers/`.

On the read side, name the model the client receives and expose query methods on it. The proxy-generation tool reads the compiled backend contracts and produces TypeScript command and query proxies. Regenerate them after a contract change, then type-check the frontend to find callers that need updating; proxy generation is not a runtime compatibility check.

## A side-by-side

A "register customer" feature in MediatR and MVC often has a request record, handler, validator, controller action, and frontend client call. The fragment below shows the Arc command and read model together. It uses MongoDB and assumes application-owned `CustomerId` and `CompanyName` concepts plus the [standalone setup](../../backend/csharp/getting-started/index.md), including `System.ComponentModel.DataAnnotations` for `Key` and a configured proxy build:

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

Arc exposes the command and query over HTTP. After proxy generation, a React component can use their typed proxies without a forwarding controller, DTO mapper, or hand-written fetch layer for this feature. Neither this direct database write nor the observable query requires event sourcing.

:::tip[Declare inline work on model-bound commands]
The direct database call remains supported. Prefer [command operations](../../backend/csharp/commands/operations/index.md) when a model-bound command can decide what inline work to perform before executing it. Returning an operation directly from an MVC action does not execute it; use a model-bound command through Arc's pipeline. Operations support a flat sequential boundary, not nested command orchestration.
:::

## Where to go next

- Build your first command in the [getting started](/arc/backend/csharp/getting-started/) guide.
- See the full command and query options under [Backend](/arc/backend/csharp/).
- Need history, replay, or reactors later? The [Chronicle integration](/arc/backend/csharp/chronicle/) shows how Arc adds event sourcing as an optional write-side choice.
