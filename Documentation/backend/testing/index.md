---
uid: Arc.Testing
---
# Testing

Arc provides first-class testing support through focused NuGet packages that let you drive commands through the real pipeline infrastructure — validation filters, authorization filters, and command handlers — without an HTTP server or an external database.

In Cratis applications, those tests are usually written as [Cratis Specifications](/testing-with-cratis/): a light BDD-style wrapper over xUnit where `Establish()` sets up the context, `Because()` performs the behavior, and `[Fact]` methods assert the outcomes. That lines up with Arc's model: given a command context, when a command runs, then the command result, appended events, or read model state should look a certain way.

The `CommandScenario<TCommand>` class is the single entry point for all command testing. When the Chronicle-specific testing package is also referenced, it automatically extends itself with an in-memory event log.

## Packages

| Package | Description |
| ------- | ----------- |
| `Cratis.Specifications.XUnit` | BDD-style `Specification` base class and `Should*` assertion helpers on top of xUnit. |
| `Cratis.Arc.Testing` | Core `CommandScenario<TCommand>` class and `CommandResult` assertion helpers. No event sourcing dependency. |
| `Cratis.Arc.Chronicle.Testing` | Automatically extends `CommandScenario<TCommand>` with an in-memory event log when referenced. |
| `Cratis.Testing` | Convenience meta-package for full Cratis projects that use both Arc and Chronicle. Use `Cratis.Arc.Testing` when you want Arc without event sourcing. |

## Topics

| Topic | Description |
| ------- | ----------- |
| [Command Scenarios](./command-scenario.md) | How to test commands with `CommandScenario<TCommand>` and the `CommandResult` assertion helpers. |
| [Chronicle Extension](./chronicle.md) | How the Chronicle in-memory event log activates automatically, how to seed events and read model state, and how to assert against the log. |

## Quick Start

### 1. Add the package

```xml
<PackageReference Include="Cratis.Specifications.XUnit" />
<PackageReference Include="Cratis.Arc.Testing" />
```

### 2. Write a spec

Create a `CommandScenario<TCommand>` instance as a field, execute the command in `Because()`, and assert the result from `[Fact]` methods:

```csharp
public class when_adding_item_to_cart : Specification
{
    readonly CommandScenario<AddItemToCart> _scenario = new();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new AddItemToCart("SKU-123", 2));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();
}
```

`Execute` runs the command through the same validation, authorization, and handler pipeline that production uses — no mocking required. The scenario initializes itself lazily on the first `Execute` or `Validate` call.

### 3. Add Chronicle assertions when the command appends events

For a Chronicle-backed slice, add the Chronicle testing extension alongside the Arc package:

```xml
<PackageReference Include="Cratis.Arc.Chronicle.Testing" />
```

You can also reference `Cratis.Testing` instead of the two focused packages when the test project is a
full Cratis test suite.

When `Cratis.Arc.Chronicle.Testing` (or the `Cratis.Testing` meta-package) is referenced, `CommandScenario<TCommand>` automatically gains three extension properties:

- `EventScenario` — exposes the full scenario, including `Given` for event seeding
- `EventLog` — shortcut to the in-memory event log for assertions
- `EventSequence` — the same instance, compatible with Chronicle's assertion helpers

```csharp
public class when_registering_author : Specification
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    readonly EventSourceId _authorId = EventSourceId.New();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterAuthor(_authorId, "Jane Austen"));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_appended_registered_event() =>
        _scenario.ShouldHaveAppendedEvent<RegisterAuthor, AuthorRegistered>(
            _authorId,
            e => e.Name == "Jane Austen");
}
```

The command still runs through Arc's real command pipeline. The Chronicle testing extension captures the events appended during that execution so the spec can assert on the facts without starting a Chronicle server.

For the cross-product testing model — Specifications, Arc command scenarios, Chronicle event/read-model/reactor scenarios, and full stack slice specs — see [Testing with Cratis](/testing-with-cratis/).
