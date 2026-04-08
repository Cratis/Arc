---
uid: Arc.Testing
---
# Testing

Arc provides first-class testing support through three focused NuGet packages that let you drive commands through the real pipeline infrastructure — validation filters, authorization filters, and command handlers — without an HTTP server, an external database, or a running Chronicle instance.

## Packages

| Package | Description |
| ------- | ----------- |
| `Cratis.Arc.Testing` | Core testing base class and `CommandResult` assertion helpers. No event sourcing dependency. |
| `Cratis.Arc.Chronicle.Testing` | Extends the core package with an in-memory event log for testing Chronicle-integrated commands. |
| `Cratis.Testing` | Meta-package that pulls in both of the above. Reference this single package in most projects. |

## Topics

| Topic | Description |
| ------- | ----------- |
| [Command Scenarios](./command-scenario.md) | How to test commands with `CommandScenarioFor<TCommand>` and the `CommandResult` assertion helpers. |
| [Chronicle Command Scenarios](./chronicle-command-scenario.md) | How to test Chronicle-backed commands with an in-memory event log using `ChronicleCommandScenarioFor<TCommand>`. |

## Quick Start

### 1. Add the package

```xml
<PackageReference Include="Cratis.Testing" />
```

### 2. Write a spec

Inherit from `CommandScenarioFor<TCommand>` (or `ChronicleCommandScenarioFor<TCommand>` for event-sourced commands) and also implement `IAsyncLifetime`. Override `InitializeAsync()` to set up state and execute the command, then assert on the result:

```csharp
public class when_adding_item_to_cart
    : CommandScenarioFor<AddItemToCart>, IAsyncLifetime
{
    CommandResult _result = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _result = await Execute(new AddItemToCart("SKU-123", 2));
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
}
```

`Execute` runs the command through the same validation, authorization, and handler pipeline that production uses — no mocking required. `IAsyncLifetime` is an xUnit interface; the base class provides virtual `InitializeAsync()` and `DisposeAsync()` methods that satisfy the contract. xUnit's lifecycle is therefore wired up automatically once your test class declares `: IAsyncLifetime`.


