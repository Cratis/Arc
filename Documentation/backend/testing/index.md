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
| [Command Scenarios](./command-scenario.md) | How to test commands with `CommandScenario<TCommand>` and the `CommandResult` assertion helpers. |
| [Chronicle Command Scenarios](./chronicle-command-scenario.md) | How to test Chronicle-backed commands with an in-memory event log using `ChronicleCommandScenario<TCommand>`. |

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


