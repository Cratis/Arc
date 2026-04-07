---
uid: Arc.Testing.CommandScenario
---
# Command Scenarios

`CommandScenarioFor<TCommand>` is the base class for testing any Arc command through the **real** command pipeline — the same infrastructure used in production. Validation filters, authorization filters, and the command handler all execute; nothing is mocked by default.

The class inherits from [`Specification`](https://github.com/Cratis/Specifications) (the Cratis BDD base class), so specs follow the familiar `Establish` / `Because` / `should_*` pattern.

## Package

```xml
<PackageReference Include="Cratis.Arc.Testing" />
```

Or via the meta-package:

```xml
<PackageReference Include="Cratis.Testing" />
```

## Basic Usage

```csharp
public class when_adding_item_to_cart : CommandScenarioFor<AddItemToCart>
{
    CommandResult _result;

    async Task Because() =>
        _result = await Execute(new AddItemToCart("SKU-123", 2));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
```

The `Execute` helper runs the command through the pipeline and returns a `CommandResult`. No HTTP, no infrastructure dependencies.

## Registering Additional Services

Inject mocks or stub implementations into the spec's constructor using the `Services` property before the pipeline is built:

```csharp
public class when_adding_item_to_cart : CommandScenarioFor<AddItemToCart>
{
    readonly IInventoryService _inventory = Substitute.For<IInventoryService>();
    CommandResult _result;

    public when_adding_item_to_cart()
    {
        // Register the mock before Establish builds the service provider
        Services.AddSingleton(_inventory);
    }

    void Establish() =>
        _inventory.IsInStock("SKU-123").Returns(true);

    async Task Because() =>
        _result = await Execute(new AddItemToCart("SKU-123", 2));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
}
```

> **Order guarantee**: The base class `Establish` runs first (it builds the pipeline), then derived `Establish` methods, and finally `Because`. Services registered in the constructor are always available when the pipeline is built.

## Validating Without Executing

Use `Validate` instead of `Execute` to run only the authorization and validation filters without invoking the command handler. This is useful for verifying validation rules in isolation:

```csharp
public class when_adding_item_with_empty_sku : CommandScenarioFor<AddItemToCart>
{
    CommandResult _result;

    async Task Because() =>
        _result = await Validate(new AddItemToCart(string.Empty, 2));

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
public class when_adding_item_with_zero_quantity : CommandScenarioFor<AddItemToCart>
{
    CommandResult _result;

    async Task Because() =>
        _result = await Validate(new AddItemToCart("SKU-123", 0));

    [Fact] void should_not_be_valid() => _result.ShouldHaveValidationErrors();
    [Fact] void should_have_quantity_error() => _result.ShouldHaveValidationErrorFor("must be greater than zero");
}
```

### Example: Authorization spec

```csharp
public class when_admin_command_executed_by_regular_user : CommandScenarioFor<DeleteAllOrders>
{
    CommandResult _result;

    public when_admin_command_executed_by_regular_user()
    {
        // Set up an identity context for a non-admin user
        Services.AddSingleton<IIdentityProvider>(new StubIdentityProvider(roles: []));
    }

    async Task Because() =>
        _result = await Execute(new DeleteAllOrders());

    [Fact] void should_not_be_authorized() => _result.ShouldNotBeAuthorized();
}
```

## What the Base Class Provides

`CommandScenarioFor<TCommand>` calls `Services.AddCratisArcCore()` in its internal `Establish`, which wires:

- Type discovery for all handlers, validators, and filters
- The real `ICommandPipeline`
- All built-in validation and authorization filters
- Logging to the test output

Everything that runs in production runs in the spec — there is no hidden short-circuiting.
