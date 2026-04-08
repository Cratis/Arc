---
uid: Arc.Testing.CommandScenario
---
# Command Scenarios

`CommandScenarioFor<TCommand>` is the base class for testing any Arc command through the **real** command pipeline — the same infrastructure used in production. Validation filters, authorization filters, and the command handler all execute; nothing is mocked by default.

## Package

```xml
<PackageReference Include="Cratis.Arc.Testing" />
```

Or via the meta-package:

```xml
<PackageReference Include="Cratis.Testing" />
```

## How It Works

`CommandScenarioFor<TCommand>` is a plain base class with no test-framework dependency. It exposes two virtual methods that you override to set up and tear down the scenario:

| Method | Purpose |
| ------ | ------- |
| `InitializeAsync()` | Builds the Arc DI container and pipeline. Override to add setup and run the command. |
| `DisposeAsync()` | Cleans up after the scenario. Override if you need resource disposal. |

To integrate with **xUnit**, your test class also implements `IAsyncLifetime`. xUnit automatically calls `InitializeAsync()` before each test method, and `DisposeAsync()` after. The base class virtual methods satisfy the interface so xUnit's lifecycle is wired up with a single declaration.

## Basic Usage

```csharp
public class when_adding_item_to_cart
    : CommandScenarioFor<AddItemToCart>, IAsyncLifetime
{
    CommandResult _result = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync(); // builds the DI container and pipeline
        _result = await Execute(new AddItemToCart("SKU-123", 2));
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
```

`base.InitializeAsync()` must be called first — it wires up the service provider and the `ICommandPipeline`. After that, `Execute` and `Validate` are available.

## Registering Additional Services

Register mocks or stub implementations in the class constructor using the `Services` property. Because xUnit creates a new instance of the test class for each `[Fact]`, the constructor runs before `InitializeAsync`, so all registrations are available when the pipeline is built:

```csharp
public class when_adding_item_to_cart
    : CommandScenarioFor<AddItemToCart>, IAsyncLifetime
{
    readonly IInventoryService _inventory = Substitute.For<IInventoryService>();
    CommandResult _result = null!;

    public when_adding_item_to_cart()
    {
        Services.AddSingleton(_inventory);
    }

    public override async Task InitializeAsync()
    {
        _inventory.IsInStock("SKU-123").Returns(true);
        await base.InitializeAsync();
        _result = await Execute(new AddItemToCart("SKU-123", 2));
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
}
```

## Validating Without Executing

Use `Validate` instead of `Execute` to run only the authorization and validation filters without invoking the command handler. This is useful for verifying validation rules in isolation:

```csharp
public class when_adding_item_with_empty_sku
    : CommandScenarioFor<AddItemToCart>, IAsyncLifetime
{
    CommandResult _result = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _result = await Validate(new AddItemToCart(string.Empty, 2));
    }

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
public class when_adding_item_with_zero_quantity
    : CommandScenarioFor<AddItemToCart>, IAsyncLifetime
{
    CommandResult _result = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _result = await Validate(new AddItemToCart("SKU-123", 0));
    }

    [Fact] void should_not_be_valid() => _result.ShouldHaveValidationErrors();
    [Fact] void should_have_quantity_error() => _result.ShouldHaveValidationErrorFor("must be greater than zero");
}
```

### Example: Authorization spec

```csharp
public class when_admin_command_executed_by_regular_user
    : CommandScenarioFor<DeleteAllOrders>, IAsyncLifetime
{
    CommandResult _result = null!;

    public when_admin_command_executed_by_regular_user()
    {
        Services.AddSingleton<IIdentityProvider>(new StubIdentityProvider(roles: []));
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _result = await Execute(new DeleteAllOrders());
    }

    [Fact] void should_not_be_authorized() => _result.ShouldNotBeAuthorized();
}
```

## What the Base Class Provides

`base.InitializeAsync()` calls `Services.AddCratisArcCore()`, which wires:

- Type discovery for all handlers, validators, and filters
- The real `ICommandPipeline`
- All built-in validation and authorization filters
- Logging to the test output

Everything that runs in production runs in the spec — there is no hidden short-circuiting.
