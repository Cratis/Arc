---
uid: Arc.Testing
---
# Testing

Arc provides first-class testing support through focused NuGet packages that let you drive commands through the real pipeline infrastructure — validation filters, authorization filters, and command handlers — without an HTTP server or an external database.

The `CommandScenario<TCommand>` class is the single entry point for all command testing. When the Chronicle-specific testing package is also referenced, it automatically extends itself with an in-memory event log.

## Packages

| Package | Description |
| ------- | ----------- |
| `Cratis.Arc.Testing` | Core `CommandScenario<TCommand>` class and `CommandResult` assertion helpers. No event sourcing dependency. |
| `Cratis.Arc.Chronicle.Testing` | Automatically extends `CommandScenario<TCommand>` with an in-memory event log when referenced. |
| `Cratis.Testing` | Meta-package that pulls in both of the above. Reference this single package in most projects. |

## Topics

| Topic | Description |
| ------- | ----------- |
| [Command Scenarios](./command-scenario.md) | How to test commands with `CommandScenario<TCommand>` and the `CommandResult` assertion helpers. |
| [Chronicle Extension](./chronicle-command-scenario.md) | How the Chronicle in-memory event log activates automatically and how to seed events and assert against the log. |

## Quick Start

### 1. Add the package

```xml
<PackageReference Include="Cratis.Testing" />
```

### 2. Write a spec

Create a `CommandScenario<TCommand>` instance as a field, then call `Execute` directly inside each `[Fact]`:

```csharp
public class when_adding_item_to_cart
{
    readonly CommandScenario<AddItemToCart> _scenario = new();

    [Fact]
    public async Task should_succeed()
    {
        var result = await _scenario.Execute(new AddItemToCart("SKU-123", 2));
        result.ShouldBeSuccessful();
    }
}
```

`Execute` runs the command through the same validation, authorization, and handler pipeline that production uses — no mocking required. The scenario initializes itself lazily on the first `Execute` or `Validate` call.

### 3. Test Chronicle-backed commands

When `Cratis.Arc.Chronicle.Testing` (or the `Cratis.Testing` meta-package) is referenced, `CommandScenario<TCommand>` automatically gains an `EventScenario` extension property exposing `EventLog` (for assertions) and `Given` (for event seeding):

```csharp
public class when_registering_author
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();

    [Fact]
    public async Task should_succeed()
    {
        var result = await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        result.ShouldBeSuccessful();
    }

    [Fact]
    public async Task should_have_appended_registered_event()
    {
        await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        await _scenario.EventScenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(EventSequenceNumber.First);
    }
}
```

No base class, no interface, no setup method — just a field and inline async `[Fact]` methods.


