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

`CommandScenario<TCommand>` is a concrete class that you **instantiate** in your test class. Create it as a field, register any additional services via `Services`, then call `Execute` or `Validate`. The service provider and pipeline are built lazily on the first `Execute` or `Validate` call so all services registered before that point are available.

To integrate with **xUnit**, implement `IAsyncLifetime` in your test class:

| Step | What happens |
| ---- | ------------ |
| Test class constructor | Create the scenario, register services |
| `InitializeAsync()` | Call `Execute` or `Validate`; scenario initializes itself on first call |
| `[Fact]` methods | Assert on the stored result |
| `DisposeAsync()` | No-op in most cases |

## Basic Usage

```csharp
public class when_adding_item_to_cart : IAsyncLifetime
{
    readonly CommandScenario<AddItemToCart> _scenario = new();
    CommandResult _result = null!;

    public async Task InitializeAsync() =>
        _result = await _scenario.Execute(new AddItemToCart("SKU-123", 2));

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
```

## Registering Additional Services

Register mocks or stub implementations in the test class constructor via `scenario.Services`. The constructor runs before `InitializeAsync`, so all registrations are in place when the pipeline is built:

```csharp
public class when_adding_item_to_cart : IAsyncLifetime
{
    readonly IInventoryService _inventory = Substitute.For<IInventoryService>();
    readonly CommandScenario<AddItemToCart> _scenario;
    CommandResult _result = null!;

    public when_adding_item_to_cart()
    {
        _scenario = new();
        _scenario.Services.AddSingleton(_inventory);
    }

    public async Task InitializeAsync()
    {
        _inventory.IsInStock("SKU-123").Returns(true);
        _result = await _scenario.Execute(new AddItemToCart("SKU-123", 2));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
}
```

## Validating Without Executing

Use `Validate` instead of `Execute` to run only the authorization and validation filters without invoking the command handler. This is useful for verifying validation rules in isolation:

```csharp
public class when_adding_item_with_empty_sku : IAsyncLifetime
{
    readonly CommandScenario<AddItemToCart> _scenario = new();
    CommandResult _result = null!;

    public async Task InitializeAsync() =>
        _result = await _scenario.Validate(new AddItemToCart(string.Empty, 2));

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_not_be_valid() => _result.ShouldHaveValidationErrors();
    [Fact] void should_report_sku_error() => _result.ShouldHaveValidationErrorFor("Sku");
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
public class when_adding_item_with_zero_quantity : IAsyncLifetime
{
    readonly CommandScenario<AddItemToCart> _scenario = new();
    CommandResult _result = null!;

    public async Task InitializeAsync() =>
        _result = await _scenario.Validate(new AddItemToCart("SKU-123", 0));

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_not_be_valid() => _result.ShouldHaveValidationErrors();
    [Fact] void should_have_quantity_error() => _result.ShouldHaveValidationErrorFor("must be greater than zero");
}
```

### Example: Authorization spec

```csharp
public class when_admin_command_executed_by_regular_user : IAsyncLifetime
{
    readonly CommandScenario<DeleteAllOrders> _scenario;
    CommandResult _result = null!;

    public when_admin_command_executed_by_regular_user()
    {
        _scenario = new();
        _scenario.Services.AddSingleton<IIdentityProvider>(new StubIdentityProvider(roles: []));
    }

    public async Task InitializeAsync() =>
        _result = await _scenario.Execute(new DeleteAllOrders());

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_not_be_authorized() => _result.ShouldNotBeAuthorized();
}
```

## What the Scenario Provides

`CommandScenario<TCommand>` calls `Services.AddCratisArcCore()` when first initialized, which wires:

- Type discovery for all handlers, validators, and filters
- The real `ICommandPipeline`
- All built-in validation and authorization filters
- Logging to the console

Everything that runs in production runs in the spec — there is no hidden short-circuiting.
