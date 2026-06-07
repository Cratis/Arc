---
uid: Arc.Testing.CommandScenario
---
# Command Scenarios

`CommandScenario<TCommand>` is a self-contained class for testing any Arc command through the **real** command pipeline — the same infrastructure used in production. Validation filters, authorization filters, and the command handler all execute; nothing is mocked by default.

The examples use [Cratis Specifications](/testing-with-cratis/) so the spec reads as given/when/then: `Establish()` registers dependencies, `Because()` runs the command, and each `[Fact]` asserts one outcome.

## Package

```xml
<PackageReference Include="Cratis.Specifications.XUnit" />
<PackageReference Include="Cratis.Arc.Testing" />
```

Or via the meta-package:

```xml
<PackageReference Include="Cratis.Specifications.XUnit" />
<PackageReference Include="Cratis.Testing" />
```

## How It Works

`CommandScenario<TCommand>` is a concrete class that you **instantiate** in your test class. Create it as a field, register any additional services via `Services`, then call `Execute` or `Validate` from `Because()` so each `[Fact]` asserts the same behavior. The service provider and pipeline are built lazily on the first `Execute` or `Validate` call so all services registered before that point are available.

At construction time `CommandScenario<TCommand>` discovers all `ICommandScenarioExtender` implementations loaded in the test process and calls each one. Extension packages such as `Cratis.Arc.Chronicle.Testing` use this mechanism to register additional services and expose them through C# extension properties — without requiring any base class or explicit setup.

## Basic Usage

```csharp
public class when_adding_item_to_cart : Specification
{
    readonly CommandScenario<AddItemToCart> _scenario = new();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new AddItemToCart("SKU-123", 2));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] void should_be_valid() =>
        _result.ShouldBeValid();
}
```

## Registering Additional Services

Register mocks or stub implementations in `Establish()` via `scenario.Services`. `Establish()` runs before `Because()`, so all registrations are in place when the pipeline is built:

```csharp
public class when_adding_item_to_cart : Specification
{
    readonly IInventoryService _inventory = Substitute.For<IInventoryService>();
    readonly CommandScenario<AddItemToCart> _scenario = new();
    CommandResult _result = default!;

    void Establish()
    {
        _inventory.IsInStock("SKU-123").Returns(true);
        _scenario.Services.AddSingleton(_inventory);
    }

    async Task Because() =>
        _result = await _scenario.Execute(new AddItemToCart("SKU-123", 2));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();
}
```

## Validating Without Executing

Use `Validate` instead of `Execute` to run only the authorization and validation filters without invoking the command handler. This is useful for verifying validation rules in isolation:

```csharp
public class when_adding_item_with_empty_sku : Specification
{
    readonly CommandScenario<AddItemToCart> _scenario = new();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Validate(new AddItemToCart(string.Empty, 2));

    [Fact] void should_not_be_valid() =>
        _result.ShouldHaveValidationErrors();

    [Fact] void should_report_sku_error() =>
        _result.ShouldHaveValidationErrorFor("Sku");
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
public class when_adding_item_with_zero_quantity : Specification
{
    readonly CommandScenario<AddItemToCart> _scenario = new();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Validate(new AddItemToCart("SKU-123", 0));

    [Fact] void should_not_be_valid() =>
        _result.ShouldHaveValidationErrors();

    [Fact] void should_have_quantity_error() =>
        _result.ShouldHaveValidationErrorFor("must be greater than zero");
}
```

### Example: Authorization spec

```csharp
public class when_admin_command_executed_by_regular_user : Specification
{
    readonly CommandScenario<DeleteAllOrders> _scenario = new();
    CommandResult _result = default!;

    void Establish()
    {
        // Arc authorization reads the current principal from IHttpRequestContextAccessor.
        // Supply a request context whose user lacks the "admin" role that DeleteAllOrders
        // requires via [Authorize(Roles = "admin")].
        var requestContext = Substitute.For<IHttpRequestContext>();
        requestContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "user")], authenticationType: "test")));

        var requestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        requestContextAccessor.Current.Returns(requestContext);

        _scenario.Services.AddSingleton(requestContextAccessor);
    }

    async Task Because() =>
        _result = await _scenario.Execute(new DeleteAllOrders());

    [Fact] void should_not_be_authorized() =>
        _result.ShouldNotBeAuthorized();
}
```

## What the Scenario Provides

`CommandScenario<TCommand>` adds console logging and calls `Services.AddCratisArcCore()` when first initialized, which wires:

- Type discovery for all handlers, validators, and filters
- The real `ICommandPipeline`
- All built-in validation and authorization filters

Everything that runs in production runs in the spec — there is no hidden short-circuiting.
