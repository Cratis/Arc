---
title: Test an event-sourced command
description: Combine direct decision specs with Arc–Chronicle pipeline specs that check event content, identity, and append outcomes.
---

A command returning the right event is a good start. For an event-sourced application, also prove that Arc and Chronicle append that event to the **intended event source**, and that rejection appends nothing.

Use direct specs for the decision and the Chronicle extension to `CommandScenario` for the append contract. You do not need an external Chronicle server for these in-process checks.

## Set up an integration spec project

Use the current .NET SDK and current Cratis packages. Keep this project separate from the standalone shipping lesson so its optional Chronicle extender does not change that lesson's test environment:

```bash
dotnet new classlib -n Catalog
dotnet add Catalog package Cratis.Arc.Chronicle
dotnet new xunit -n Catalog.Specs
dotnet add Catalog.Specs reference Catalog/Catalog.csproj
dotnet add Catalog.Specs package Cratis.Specifications.XUnit
dotnet add Catalog.Specs package Cratis.Arc.Chronicle.Testing
dotnet add Catalog.Specs package Cratis.Arc
```

The explicit `Cratis.Arc` reference supplies the Arc ASP.NET Core assembly and the `Microsoft.AspNetCore.App` shared framework used by the in-process testing infrastructure. The current Chronicle testing package does not bring both prerequisites transitively into this isolated class-library setup. This reference does not start an HTTP host or an external Chronicle server.

Remove the generated placeholder class/test if present. Keep application and spec projects on compatible current Cratis packages. The in-process Chronicle testing package has its own target-framework requirements; a test project's target need not be identical to an older application target.

## Keep identity creation out of the decision

Create `Catalog/RegisterAuthor.cs`:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Catalog;

public record AuthorId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly AuthorId NotSet = new(Guid.Empty);
    public static AuthorId New() => new(Guid.NewGuid());
    public static implicit operator AuthorId(Guid value) => new(value);
}

public record AuthorName(string Value) : ConceptAs<string>(Value)
{
    public static readonly AuthorName NotSet = new(string.Empty);
    public static implicit operator AuthorName(string value) => new(value);
}

[Command]
public record RegisterAuthor(AuthorName Name)
{
    public AuthorId Provide() => AuthorId.New();

    public (AuthorId, AuthorRegistered) Handle(AuthorId authorId) =>
        (authorId, new AuthorRegistered(Name));
}

/// <summary>Records that an author was registered with a name.</summary>
[EventType]
public record AuthorRegistered(AuthorName Name);
```

The concepts have different jobs. `AuthorName` gives an ordinary value its domain meaning. `AuthorId` additionally tells Chronicle that the value identifies an event source. Reuse both within the author feature; in an application, shared concepts normally live in their own files there.

`Provide()` creates the new identity. `Handle()` is deterministic for a given command and supplied identity: it returns the identity and a fact containing the name. The event does not repeat its own event-source ID; Chronicle stores that in event context.

This example deliberately focuses on identity and appending. Add a `ConceptValidator<AuthorName>` for name invariants in the application; [the standalone testing lesson](./command-decisions.md) demonstrates how to prove concept validation through Arc.

## Specify the returned decision directly

Create `Catalog.Specs/for_RegisterAuthor/when_deciding_to_register_an_author.cs`:

```csharp
using Catalog;
using Cratis.Specifications;
using Xunit;

namespace Catalog.Specs;

public class when_deciding_to_register_an_author : Specification
{
    readonly AuthorId _id = AuthorId.New();
    readonly AuthorName _name = "Sample Author";
    AuthorId _returnedId = AuthorId.NotSet;
    AuthorRegistered _event = null!;

    void Because() =>
        (_returnedId, _event) = new RegisterAuthor(_name).Handle(_id);

    [Fact] void should_return_the_supplied_identity() => _returnedId.ShouldEqual(_id);

    [Fact] void should_record_the_name() => _event.Name.ShouldEqual(_name);
}
```

This is plain C# testing. It does not call `Provide()`, append an event, or validate the command. The supplied identity makes the decision easy to test without controlling a random-number generator or mocking an event log.

Direct specs are a good place for all the decision branches. They are not a replacement for the next boundary.

## Prove the response and appended identity agree

Create `Catalog.Specs/for_RegisterAuthor/when_registering_an_author_through_arc.cs`:

```csharp
using Catalog;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using Xunit;

namespace Catalog.Specs;

public class when_registering_an_author_through_arc : Specification
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    readonly AuthorName _name = "Sample Author";
    CommandResult _result = null!;

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterAuthor(_name));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact] void should_append_one_event() => _scenario.AppendedEvents.Count.ShouldEqual(1);

    [Fact] async Task should_append_the_name_under_the_returned_identity()
    {
        var response = ((CommandResult<AuthorId>)_result).Response!;
        response.ShouldNotEqual(AuthorId.NotSet);
        await _scenario.ShouldHaveAppendedEvent<RegisterAuthor, AuthorRegistered>(
            (EventSourceId)response,
            @event => @event.Name == _name);
    }

    void Destroy() => _scenario.Dispose();
}
```

Run:

```bash
dotnet test Catalog.Specs
```

The five facts across the two specs should pass. The scenario spec exercises identity creation, argument resolution, response handling, and the in-process append. The final assertion searches the captured append by the **returned identity and expected payload**; the separate count assertion rules out duplicate appends.

This catches a mistake a direct test could miss: `(Guid, AuthorRegistered)` can return a plausible GUID and event while the integration uses a different fallback event-source ID. Returning `AuthorId : EventSourceId<Guid>` makes the intent explicit. [ARCCHR0010](../chronicle/code-analysis/ARCCHR0010.md) warns about suspicious signatures, while this behavioral spec protects the actual contract.

## Prove rejection for the intended reason

For a rejected command, assert its specific validation reason or named constraint **and** that it appended no new events. A broad “not successful” assertion can otherwise pass because a dependency was unavailable or the host was misconfigured.

Seed the state required by the rule before executing. Choose between historical events and a pinned read-model instance deliberately; they are not interchangeable setup shortcuts. The [Chronicle scenario reference](./chronicle.md) shows those builders and precise assertions. Captured append lists can include setup appends, so compare the post-action additions or establish the appropriate baseline rather than assuming the list always starts empty.

## Test each additional boundary on purpose

| Question                                                                              | Appropriate next test                                                                |
| ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Does a sequence of events produce the right read model?                               | `ReadModelScenario<TReadModel>` with representative history and expected state       |
| Does a reactor decide on the right follow-up?                                         | Direct handler spec or `ReactorScenario<TReactor>`, with deliberate dependency setup |
| Does the application discover and register its real reactor?                          | Hosted integration test using the real application composition                       |
| Does a command-triggered automation actually finish?                                  | Hosted integration test that waits for the intended observer/event outcome           |
| Do database mappings, serialization, and constraints work with the deployed provider? | Provider-backed integration test                                                     |
| Does the caller receive the expected HTTP result and generated-client value?          | Host/transport or full-stack test                                                    |

A successful `CommandScenario.Execute()` is not proof that every projection and reactor in a running application has finished. Do not import that assumption from another framework's asynchronous test harness. For live observer tests, wait on an observable completion condition with a deadline—not an arbitrary sleep.

This combination keeps most business specs quick and focused, while a smaller set of boundary tests proves that the framework, storage, and application are connected correctly. Continue with [choosing a test boundary](./index.md#choose-the-boundary-that-can-catch-the-bug) and the [Chronicle testing reference](./chronicle.md).
