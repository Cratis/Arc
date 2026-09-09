---
uid: Arc.Testing.CommandScenario
title: Command scenarios
description: Configure and execute standalone command scenarios, and assert precise validation and authorization outcomes.
---

`CommandScenario<TCommand>` exercises Arc's real command pipeline. Use it when a direct `Handle()` call would miss validation, authorization, dependency resolution, or execution scopes. It complements fast decision specs; it is not a requirement for every command test. Start with [the decision-and-pipeline lesson](./command-decisions.md), or use [Testing](./index.md) to choose the right boundary. This page is the scenario API reference.

<a id="package"></a>
<a id="basic-usage"></a>

## Package and API

Use `Cratis.Arc.Testing` for standalone Arc tests, plus `Cratis.Specifications.XUnit` when writing Specifications. Chronicle is not required. The optional `Cratis.Testing` meta-package also brings Chronicle support; it is not the minimal standalone choice.

Import `Cratis.Arc.Testing.Commands` for both the scenario and result assertion extensions.

| Member                         | Contract                                                                                                                                         |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Services`                     | `IServiceCollection` for registrations made before initialization                                                                                |
| `Context`                      | `IDictionary<string, object>` populated by extenders                                                                                             |
| `Execute(TCommand command)`    | Returns `Task<CommandResult>`; runs filters, argument resolution (including `Provide()`), the handler, response processing, and execution scopes |
| `Validate(TCommand command)`   | Returns `Task<CommandResult>`; runs pipeline filters but skips handler argument resolution, `Provide()`, `Handle()`, and execution scopes        |
| `Dispose()` / `DisposeAsync()` | Releases the provider and disposable context values                                                                                              |

There is no typed `Execute<TResult>` overload on the scenario. For a known successful response, cast to `CommandResult<TResponse>` and inspect its public `Response`, as in the [standalone checkpoint](./index.md#try-a-standalone-command-spec). The pipeline constructs that generic result from the returned value's runtime type; do not assume a cast to a base/interface response type will work. Failure or no-response results need not have that generic type.

<a id="how-it-works"></a>
<a id="registering-additional-services"></a>
<a id="what-the-scenario-provides"></a>

## Initialization and dependencies

Instantiate the scenario in your spec. At construction it creates `Services` and `Context`, configures options and logging without a sink, then discovers and invokes `ICommandScenarioExtender` implementations. Extenders need a public parameterless constructor. Optional packages can therefore change the scenario's services without a different base class.

The first `Execute` or `Validate` call builds the service provider and resolves `ICommandPipeline`. Before that call, register application services, substitutes, and options through `Services`, usually in `Establish()`. Later registrations do not rebuild the provider. Discovered validators are constructed on demand using the command scope; you do not need to manually register every validator.

This runs pipeline behavior, **not everything in your production host**. There is no HTTP routing, request binding, authentication middleware, or automatic copy of your host's registrations. Application services may still access external infrastructure unless you replace them. Use host/integration tests for those boundaries.

To opt into console logging while debugging, use this setup fragment before the first pipeline call (with `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging` imported and the console logging package available):

```csharp
_scenario.Services.AddLogging(logging => logging.AddConsole());
```

<a id="validating-without-executing"></a>
<a id="example-validation-spec"></a>

## Validate without executing

This complete test file uses the packages from the [standalone quick start](./index.md). It defines the command and validator so the message assertion has a known source. Compile it separately from the quick-start file, or give the types distinct names.

```csharp
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using FluentValidation;
using Xunit;

namespace ValidationSpecs;

[Command]
public record NormalizeName(string Name)
{
    public string Handle() => Name.Trim();
}

public class NormalizeNameValidator : CommandValidator<NormalizeName>
{
    public NormalizeNameValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("Name is required");
    }
}

public class when_validating_an_empty_name : Specification
{
    readonly CommandScenario<NormalizeName> _scenario = new();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Validate(new NormalizeName(string.Empty));

    [Fact] void should_have_validation_errors() =>
        _result.ShouldHaveValidationErrors();

    [Fact] void should_report_the_required_name() =>
        _result.ShouldHaveValidationErrorFor("Name is required");

    void Destroy() => _scenario.Dispose();
}
```

Run `dotnet test`; both facts should pass. `Validate` still requires a discoverable command handler, but does not call it. A successful validation result does not prove that `Provide()`, `Handle()`, or a commit-time check will succeed during execution. Custom filters can also perform work, so validation is not a universal side-effect-free sandbox.

## CommandResult assertion helpers

These extension methods return `void`. Built-in assertion failures throw `CommandResultAssertionException`. After its own check passes, **every** helper below applies discovered `ICommandResultAssertionPolicy` implementations through `CommandResultAssertionPolicies`; a policy can still fail the assertion or throw its own exception.

| Method                                                              | Built-in check                                                                                                                 |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `ShouldBeSuccessful()`                                              | `IsSuccess` is true; includes failure reasons otherwise                                                                        |
| `ShouldNotBeSuccessful()`                                           | `IsSuccess` is false                                                                                                           |
| `ShouldBeValid()`                                                   | `IsValid` is true                                                                                                              |
| `ShouldHaveValidationErrors()`                                      | `IsValid` is false, and the validation results are not exclusively `DependencyUnavailable`                                     |
| `ShouldHaveValidationErrorFor(string message)`                      | A validation result's message contains the text using ordinal, case-sensitive matching; this is **not** a property-path lookup |
| `ShouldHaveValidationErrorBecauseOf(ValidationResultReason reason)` | A validation result carries that reason                                                                                        |
| `ShouldHaveConstraintViolationFor(string constraintName)`           | A validation result has reason `ConstraintViolation` and `ReasonDetail` exactly equal to the name                              |
| `ShouldBeAuthorized()`                                              | `IsAuthorized` is true                                                                                                         |
| `ShouldNotBeAuthorized()`                                           | `IsAuthorized` is false                                                                                                        |
| `ShouldNotHaveExceptions()`                                         | `HasExceptions` is false                                                                                                       |
| `ShouldHaveExceptions()`                                            | `HasExceptions` is true                                                                                                        |

<a id="assert-the-constraint-name-not-the-message"></a>

A broad failure assertion can pass for the wrong reason. Pair it with a specific message, reason, or constraint assertion. Message matching fails if the message is reworded to remove the expected text; it does not silently become a no-op. For optional Chronicle constraints, prefer `ShouldHaveConstraintViolationFor` with your application's constraint-name constant instead of relying on prose.

## Dependency unavailable is not a business-rule rejection

A missing required read model can reject a command before its validator is constructed. `ShouldHaveValidationErrors()` deliberately fails when **all** validation results have reason `DependencyUnavailable`: otherwise a spec might pass without the business rule ever running. If other validation reasons are present too, the broad assertion can pass; it still does not identify which rule ran.

Seed/register the required state when testing a business rule. When unavailable state is itself the intended outcome, assert the reason explicitly. This is an assertion fragment for a spec with `_result` already assigned; import `Cratis.Arc.Validation`:

```csharp
[Fact] void should_reject_unavailable_state() =>
    _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.DependencyUnavailable);
```

This distinguishes a registered provider returning missing required state or an unusable key from an unregistered required service, which can instead produce an exception outcome. See [ARC0006](../code-analysis/ARC0006.md) for the resolution distinction and the [optional Chronicle seeding helpers](./chronicle.md#testing-commands-that-take-read-model-dependencies) for event-sourced tests.

<a id="example-authorization-spec"></a>

## Supply a principal for authorization

Pipeline authorization reads `ICurrentPrincipalAccessor`. Test its behavior by supplying a principal; this does not test login or token validation. The following complete standalone test file additionally requires `NSubstitute`:

```csharp
using System.Security.Claims;
using System.Threading.Tasks;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace AuthorizationSpecs;

public enum ApplicationRole { Administrator, User }

[Command]
[Roles(nameof(ApplicationRole.Administrator))]
public record RunAdministration()
{
    public string Handle() => "Completed";
}

public class when_a_regular_user_runs_administration : Specification
{
    readonly CommandScenario<RunAdministration> _scenario = new();
    CommandResult _result = default!;

    void Establish()
    {
        var principalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        principalAccessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, nameof(ApplicationRole.User))],
            authenticationType: "test")));
        _scenario.Services.AddSingleton(principalAccessor);
    }

    async Task Because() =>
        _result = await _scenario.Execute(new RunAdministration());

    [Fact] void should_not_be_authorized() =>
        _result.ShouldNotBeAuthorized();

    void Destroy() => _scenario.Dispose();
}
```

Run `dotnet test`; the fact should pass because the authenticated user lacks the required role. Arc pipeline authorization and host-level policy/scheme enforcement are distinct boundaries.

## Disposal

The scenario owns its provider and disposable values in `Context`. Keep a field-initialized scenario `readonly` when the spec does not replace it; a field assigned or replaced in `Establish()` can remain mutable. `Specification` already implements xUnit's `IAsyncLifetime` and calls `Destroy()` by convention for cleanup. Use `void Destroy() => _scenario.Dispose();`; no additional disposal interface is needed on the spec class.

With plain xUnit, use the test framework's supported disposal lifecycle. Asynchronous scenario disposal prefers `IAsyncDisposable` on owned values and falls back to `IDisposable`.

Disposal is idempotent. Calling `Execute` or `Validate` afterwards throws `ObjectDisposedException`. Dispose per scenario to avoid accumulating providers during long test runs.

## Next steps

- [Optional Chronicle testing](./chronicle.md) adds in-process event-log assertions and seeded read model state.
- [Command pipeline](../commands/command-pipeline.md) explains the production execution path.
