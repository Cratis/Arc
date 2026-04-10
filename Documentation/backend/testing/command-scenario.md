---
uid: Arc.Testing.CommandScenario
---
# Command Scenarios

`CommandScenario<TCommand>` is a self-contained class for testing any Arc command through the **real** command pipeline — the same infrastructure used in production. Validation filters, authorization filters, and the command handler all execute; nothing is mocked by default.

## Package

```xml
<PackageReference Include="Cratis.Arc.Testing" />
```

Or via the meta-package:

```xml
<PackageReference Include="Cratis.Testing" />
```

## How It Works

`CommandScenario<TCommand>` is a concrete class that you **instantiate** in your test class. Create it as a field, register any additional services via `Services`, then call `Execute` or `Validate` directly inside each `[Fact]`. The service provider and pipeline are built lazily on the first `Execute` or `Validate` call so all services registered before that point are available.

At construction time `CommandScenario<TCommand>` discovers all `ICommandScenarioExtender` implementations loaded in the test process and calls each one. Extension packages such as `Cratis.Arc.Chronicle.Testing` use this mechanism to register additional services and expose them through C# extension properties — without requiring any base class or explicit setup.

## Basic Usage

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

    [Fact]
    public async Task should_be_valid()
    {
        var result = await _scenario.Execute(new AddItemToCart("SKU-123", 2));
        result.ShouldBeValid();
    }
}
```

## Registering Additional Services

Register mocks or stub implementations in the test class constructor via `scenario.Services`. The constructor runs before any `[Fact]`, so all registrations are in place when the pipeline is built:

```csharp
public class when_adding_item_to_cart
{
    readonly IInventoryService _inventory = Substitute.For<IInventoryService>();
    readonly CommandScenario<AddItemToCart> _scenario = new();

    public when_adding_item_to_cart() =>
        _scenario.Services.AddSingleton(_inventory);

    [Fact]
    public async Task should_succeed()
    {
        _inventory.IsInStock("SKU-123").Returns(true);
        var result = await _scenario.Execute(new AddItemToCart("SKU-123", 2));
        result.ShouldBeSuccessful();
    }
}
```

## Validating Without Executing

Use `Validate` instead of `Execute` to run only the authorization and validation filters without invoking the command handler. This is useful for verifying validation rules in isolation:

```csharp
public class when_adding_item_with_empty_sku
{
    readonly CommandScenario<AddItemToCart> _scenario = new();

    [Fact]
    public async Task should_not_be_valid()
    {
        var result = await _scenario.Validate(new AddItemToCart(string.Empty, 2));
        result.ShouldHaveValidationErrors();
    }

    [Fact]
    public async Task should_report_sku_error()
    {
        var result = await _scenario.Validate(new AddItemToCart(string.Empty, 2));
        result.ShouldHaveValidationErrorFor("Sku");
    }
}
```

## CommandResult Assertion Helpers

The `CommandResultShouldExtensions` class provides fluent BDD-style assertions for `CommandResult`. All helpers throw `CommandResultAssertionException` with a descriptive message on failure.

| Method | Asserts that... |
| ------ | --------------- |
| `ShouldBeSuccessful()` | `IsSuccess` is `true`; prints all failure reasons on failure |
| `ShouldNotBeSuccessful()` | `IsSuccess` is `false` |
| `ShouldBeValid()` | `IsValid` is `true`; lists all validation errors on failure |
| `ShouldHaveValidationErrors()` | `IsValid` is `false` |
| `ShouldHaveValidationErrorFor(message)` | At least one validation error contains the given text |
| `ShouldBeAuthorized()` | `IsAuthorized` is `true` |
| `ShouldNotBeAuthorized()` | `IsAuthorized` is `false` |
| `ShouldNotHaveExceptions()` | `HasExceptions` is `false` |
| `ShouldHaveExceptions()` | `HasExceptions` is `true` |

### Example: Validation spec

```csharp
public class when_adding_item_with_zero_quantity
{
    readonly CommandScenario<AddItemToCart> _scenario = new();

    [Fact]
    public async Task should_not_be_valid()
    {
        var result = await _scenario.Validate(new AddItemToCart("SKU-123", 0));
        result.ShouldHaveValidationErrors();
    }

    [Fact]
    public async Task should_have_quantity_error()
    {
        var result = await _scenario.Validate(new AddItemToCart("SKU-123", 0));
        result.ShouldHaveValidationErrorFor("must be greater than zero");
    }
}
```

### Example: Authorization spec

```csharp
public class when_admin_command_executed_by_regular_user
{
    readonly CommandScenario<DeleteAllOrders> _scenario = new();

    public when_admin_command_executed_by_regular_user() =>
        _scenario.Services.AddSingleton<IIdentityProvider>(new StubIdentityProvider(roles: []));

    [Fact]
    public async Task should_not_be_authorized()
    {
        var result = await _scenario.Execute(new DeleteAllOrders());
        result.ShouldNotBeAuthorized();
    }
}
```

## What the Scenario Provides

`CommandScenario<TCommand>` calls `Services.AddCratisArcCore()` when first initialized, which wires:

- Type discovery for all handlers, validators, and filters
- The real `ICommandPipeline`
- All built-in validation and authorization filters
- Logging to the console

Everything that runs in production runs in the spec — there is no hidden short-circuiting.
