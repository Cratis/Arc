---
title: Test a command
description: Exercise the standalone Arc pipeline in process, register its collaborators explicitly, and assert both results and effects without a web server or database.
---

**Goal:** verify the command's result and its actual effect without pretending that a successful result proves a database write.

## Use the standalone harness

Reference `Cratis.Arc.Testing` in an xUnit spec project, together with `Cratis.Specifications.XUnit`, `NSubstitute`, and the usual xUnit runner/test SDK. `Cratis.Testing` is the wider integration meta-package; it is not required here. A dedicated spec project can run in Debug and Release; use `#if DEBUG` only when intentionally embedding specs in an application assembly that excludes them from Release.

For decision-only cases, a direct `Handle()` call can be the simpler test. The [decision-and-pipeline lesson](/arc/backend/testing/command-decisions/) shows how the two approaches complement each other. This recipe deliberately tests service interaction through Arc.

`CommandScenario<TCommand>` runs the real validation, authorization, `Provide`, and handler pipeline. It does **not** automatically fake your Mongo collection, context, or application services. Register all collaborators before the first `Execute` or `Validate`; the service provider is built lazily then. Dispose the scenario afterward.

:::tip[Test the decision separately from execution]
For immediate inline side effects, prefer [command operations](../backend/commands/operations/index.md); the direct service-call example below remains supported. An operation-returning `Handle()` can be tested by inspecting its data without executing the work. It is pure only when it uses supplied inputs without I/O, clock reads, randomness, or mutable external state. [Operation scenarios](../backend/testing/command-operations.md) test actual execution and recovery with controlled collaborators, not automatic recording stubs.
:::

## Define the service-backed example

This example uses only the `AuthorId` and `AuthorName` concept declarations from the [backend lesson](/arc/backend/getting-started/your-first-command/), not its database-backed `RegisterAuthor`. Put these declarations in `Library.Authors` in the application project:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Library.Authors;

public interface IAuthorRegistration
{
    Task Register(AuthorId id, AuthorName name);
}

[Command]
public record RecordAuthor(AuthorId Id, AuthorName Name)
{
    public Task Handle(IAuthorRegistration registration) => registration.Register(Id, Name);
}

public class RecordAuthorValidator : CommandValidator<RecordAuthor>
{
    public RecordAuthorValidator() => RuleFor(command => command.Name.Value).NotEmpty();
}
```

`IAuthorRegistration` is an **application-owned interface**, not an Arc API. Production supplies its implementation; the spec supplies a substitute. If you test the Mongo tutorial command instead, supply its actual collection/validator dependencies or deliberately run a database integration spec.

## Assert the effect through the pipeline

Create `when_recording_an_author.cs` in the spec project:

```csharp
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Library.Authors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Library.Specs;

public class when_recording_an_author : Specification
{
    readonly CommandScenario<RecordAuthor> _scenario = new();
    readonly AuthorId _id = AuthorId.New();
    readonly AuthorName _name = new("Ada Lovelace");
    IAuthorRegistration _registration = null!;
    CommandResult _result = null!;

    void Establish()
    {
        _registration = Substitute.For<IAuthorRegistration>();
        _registration.Register(_id, _name).Returns(Task.CompletedTask);
        _scenario.Services.AddSingleton(_registration);
    }

    async Task Because() => _result = await _scenario.Execute(new RecordAuthor(_id, _name));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_register_the_author() => await _registration.Received(1).Register(_id, _name);

    void Destroy() => _scenario.Dispose();
}
```

The first assertion checks the pipeline result; the second proves the handler called its collaborator with the intended values. Neither proves a production database mapping or index — test those at their integration boundary.

## Check rejection independently

In a separate spec, use the same service setup and execute `new RecordAuthor(_id, new AuthorName(string.Empty))`. Assert **both** `ShouldNotBeSuccessful()` and `ShouldHaveValidationErrors()`, and verify `Register` was not called. `Validate` is available when you intentionally want pre-flight validation without invoking the handler; keep that action in a separate spec as well.

For protected commands, set up the principal explicitly and assert `ShouldNotBeAuthorized()` separately from validation. The [scenario reference](/arc/backend/testing/command-scenario/) covers context setup and the complete assertion API.

## Optional Chronicle testing

Only event-sourced commands need the [Chronicle testing extension](/arc/backend/testing/chronicle/). It adds event/read-model seeding and append assertions. Keep those specs labeled as integration behavior; standalone Arc tests assert database/application-service effects instead.
