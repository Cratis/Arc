---
uid: Arc.Testing.CommandWiring
title: Check command wiring without executing commands
description: Catch missing Handle services and discoverable-validator constructor dependencies across command assemblies before running scenarios.
---

A command scenario proves one invocation. A wiring spec checks the declarations across a command assembly, so a newly added `Handle()` dependency does not remain unnoticed until somebody invokes that command.

Use `CommandWiring` from `Cratis.Arc.Testing.Commands` with your application's configured **scoped** provider:

```csharp
CommandWiring.ShouldBeExecutableIn(scope.ServiceProvider, typeof(SubmitOrder).Assembly);
```

The checker discovers concrete, closed `[Command]` types and uses Arc's instance `Handle()` discovery, including non-public methods. It does not construct command objects or execute `Handle()`, `Provide()`, authorization, or validation rules. Dependency factories and discoverable-validator constructors **do execute**: use test registrations for services whose activation has external effects. The caller owns and disposes the provider and scope; the checker never creates a scope or host for you.

Handle bindings follow runtime precedence: validated `[GeneratedValue]` declarations, the invocation's `CancellationToken`, potential `Provide()` results, extension parameter resolvers, then DI. Cancellation tokens and context-free generated values do not require DI registrations, and no generated factory is invoked. Context-dependent generation and potential `Provide()` results report `context-required`: their invocation values cannot be proved without executing command code. Awaited tuple and union return declarations are inspected without invoking `Provide()`. Opaque tuple/union results and possible attempts to supply generated arguments also require invocation evidence rather than receiving a guessed pass. The command object, context and same-named command properties are not implicit Handle bindings; they require a real runtime source like any other dependency. Handle-only bindings do not apply to validator constructor parameters.

A nullable ordinary dependency may be absent, as at runtime. A registered dependency whose factory throws still fails, even when the parameter is nullable. Validator discovery uses the application's `ITypes` universe and only `IDiscoverableValidator<T>` implementations. An ordinary `IValidator<T>` registration does not become discoverable merely because the checker is running. Duplicate discoverable validators and missing discovery infrastructure fail explicitly.

## Build a small provider in a plain spec

A host is not required. Register `ITypes` with the same discoverable-validator universe as the application, plus its ordinary dependencies. This example uses NSubstitute to make that universe explicit; it does not pretend an empty substitute represents a whole application.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Specifications;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace WiringSpecs;

[Command]
public class SubmitOrder
{
    public void Handle(IOrderService orders) { }
}

public interface IOrderService;
public class OrderService : IOrderService;

public class SubmitOrderValidator(IOrderService orders) : CommandValidator<SubmitOrder>
{
    public IOrderService Orders { get; } = orders;
}

public class when_checking_command_wiring : Specification
{
    ServiceProvider _root = default!;
    IServiceScope _scope = default!;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>))
            .Returns([typeof(SubmitOrderValidator)]);
        _root = new ServiceCollection()
            .AddSingleton(types)
            .AddScoped<IOrderService, OrderService>()
            .BuildServiceProvider();
        _scope = _root.CreateScope();
    }

    void Because() => CommandWiring.ShouldBeExecutableIn(
        _scope.ServiceProvider, typeof(SubmitOrder).Assembly);

    void Destroy()
    {
        _scope.Dispose();
        _root.Dispose();
    }

    [Fact] void should_have_executable_wiring() { }
}
```

`SubmitOrderValidator` needs `IOrderService` just as `Handle()` does. Removing that registration reports both dependencies. For an unregistered discoverable validator the checker selects Arc's longest public constructor, preflights its dependencies, and uses Arc's construction path. A registered validator wins in ordinary mode. None of these steps calls `Validate()`.

## Check a subset or inspect all failures

```csharp
CommandWiring.ShouldBeExecutableOf(scope.ServiceProvider, typeof(SubmitOrder));
var problems = CommandWiring.FindWiringProblems(
    scope.ServiceProvider, new[] { typeof(SubmitOrder) });
```

The assertion APIs throw `CommandsAreNotExecutable` with its ordered `Problems` collection. Each `CommandWiringProblem` names the command, member, parameter and category, and gives corrective guidance. `Cause` retains an original exception for debugging; the assertion message does not include its message or command values. Empty selections, empty assemblies and incomplete assembly loading do not pass silently.

## Add Chronicle binding checks

Use `ChronicleCommandWiring` from `Cratis.Arc.Chronicle.Testing.Commands` when dependencies include Chronicle read models:

```csharp
ChronicleCommandWiring.ShouldBeExecutableIn(
    scope.ServiceProvider, typeof(SubmitOrder).Assembly);
```

Register the application's `IClientArtifactsProvider` as well as `ITypes`. Its projections, model-bound projections and reducers identify Chronicle-backed models; `[ReadModel]` alone is not evidence of backing storage. The wrapper adds the Chronicle metadata rule automatically. It checks explicit lookup properties, implicit-key ambiguity in read-protection On mode, and caller-provider declaration shape. It never constructs or executes caller-key providers, reads command properties, acquires models, or asks Chronicle to admit a decision read. Default event-source lookups are checked without manufacturing an identity or invoking identifier generation.

A Chronicle-backed `IMongoCollection<T>` is refused when the effective read-protection mode is On, unless its parameter or model explicitly declares `[Unprotected]`, matching the runtime advisory boundary. Off and Measure modes use ordinary DI. The application's operational cap is applied before this check. Backing `IQueryable<T>` activation remains explicitly unsupported rather than constructing a query provider to guess its safety.

A discoverable validator that consumes a contextual model needs an **explicit model fixture registration**. Without one the checker reports `context-required` rather than invoking the read resolver. A fixture lets the validator constructor run; it does not prove that a production read returns that model.

Decision-read tokens and per-stream models are recognized, and redirected lookup declarations are rejected. Their invocation-specific factory provenance, routes and template values require a [command scenario](./command-scenario.md); the assembly check reports `unsupported` rather than approving them. Protected commands use the effective command policy and the provider's protected-dependency checks. Protected contextual bindings and protected validators likewise require invocation evidence and are reported as unsupported.

## Cover other parameter resolvers explicitly

`CommandWiringOptions.Rules` accepts ordered `ICommandWiringRule` implementations. Each declares the exact resolver types it covers and returns not-applicable, structural support, or a failure for a parameter declaration. The first applicable rule owns that parameter. A rule must inspect metadata, not call the resolver or read command state. Resolver types found through application discovery or registered resolver collections without a matching rule produce one provider-level `unsupported` problem per type, not one per command.

Passing this check establishes declarations and activation, not business behavior, authorization, runtime keys, model existence, read admission, or protected-state ownership. Keep [direct decision specs](./command-decisions.md) and command scenarios for those contracts.
