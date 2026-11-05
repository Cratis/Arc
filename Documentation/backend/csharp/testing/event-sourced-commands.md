---
title: Cratis Arc event-sourced command tests
description: Test an event-sourced Cratis Arc command directly, then use Chronicle scenarios to check that the event is appended under the returned identity.
---

Test an event-sourced Cratis Arc command at two boundaries: call `Handle()` to check the returned decision, then use Cratis Chronicle's extension to `CommandScenario` to check the append. The event must reach the intended event source, the identity whose history it belongs to.

This lesson registers an author and checks that the returned author id matches the stored event's identity. The checks run in process without an external Chronicle server. If your command does not use Chronicle, follow [the standalone decision and pipeline lesson](./command-decisions.md) instead.

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

`AuthorName` gives a string its domain meaning. `AuthorId` also tells Chronicle that the value identifies an event source, so the integration can use it for the append. Reuse both concepts within the author feature; shared concepts normally live in their own files.

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

Add decision branches alongside this spec. To catch mistakes in identity resolution or event appending, run the same command through the scenario next.

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

The expected result is five passing facts across the two specs. The scenario exercises identity creation and argument resolution, then checks the response against the captured append. Matching both the returned identity and payload catches misrouted or incorrect events; the separate count assertion catches duplicate appends.

This catches a mistake a direct test could miss: `(Guid, AuthorRegistered)` can return a plausible GUID and event while the integration uses a different fallback event-source ID. Returning `AuthorId : EventSourceId<Guid>` makes the intent explicit. [ARCCHR0010](../chronicle/code-analysis/ARCCHR0010.md) warns about suspicious signatures, while this behavioral spec protects the actual contract.

## Prove rejection for the intended reason

When you add a rejection rule, check its specific validation reason or named constraint and the absence of new events. A broad “not successful” assertion could pass because a dependency was unavailable, without ever reaching the rule.

Seed the state required by that rule before executing. Historical log events and a pinned read-model instance serve different purposes; the [Chronicle scenario reference](./chronicle.md) explains which builder to use. In ordinary scenarios, captured append lists include event-log setup, so establish a baseline before checking additions. Protected decision scenarios capture successful command appends separately from their seeds.

## Test each additional boundary on purpose

| Question                                                                              | Appropriate next test                                                                |
| ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Does a sequence of events produce the right read model?                               | `ReadModelScenario<TReadModel>` with representative history and expected state       |
| Does a reactor decide on the right follow-up?                                         | Direct handler spec or `ReactorScenario<TReactor>`, with deliberate dependency setup |
| Does the application discover and register its real reactor?                          | Hosted integration test using the real application composition                       |
| Does a command-triggered automation actually finish?                                  | Hosted integration test that waits for the intended observer/event outcome           |
| Do database mappings, serialization, and constraints work with the deployed provider? | Provider-backed integration test                                                     |
| Does the caller receive the expected HTTP result and generated-client value?          | Host/transport or full-stack test                                                    |

A successful `CommandScenario.Execute()` does not mean every projection or reactor in a running application has finished. A projection derives read-model state from events; a reactor handles events to perform follow-up work. For live observer tests, wait for the intended outcome with a deadline rather than sleeping for an arbitrary interval.

Use [choosing a test boundary](./index.md#choose-the-boundary-that-can-catch-the-bug) to select the next test your feature needs. The [Chronicle testing reference](./chronicle.md) covers seeding, read-model dependencies, and append assertions in more detail.
