---
title: Use decision tokens in validators
description: Opt into fresh, provider-bound construction of validators for ProtectedDecision commands on untrimmed JIT runtimes.
---

Use this opt-in when a `[ProtectedDecision]` command outside read protection `On` needs a discoverable validator that takes `DecisionRead<T>` or the command-aware `IDecisionReads`. Prefer [plain read-model parameters under On](./validators.md) if you do not need explicit tokens.

This capability supports **untrimmed JIT runtimes only**. Arc refuses it when dynamic code is unavailable. Trimmed JIT is unsupported even if dynamic code is available; provider certification does not preserve constructor metadata or make an application NativeAOT-safe. Existing parameterless-validator and `On` behavior is unchanged.

## Opt in at the host

Keep your existing Arc and Chronicle configuration. Install Arc's provider factory **after** `AddCratisArc`, which configures the default provider. This excerpt assumes `builder` is your `WebApplicationBuilder` and you have already configured Arc with Chronicle:

```csharp
using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.Hosting;

builder.Host.UseServiceProviderFactory(new ProtectedDecisionServiceProviderFactory());
```

The same factory works with `IHostBuilder.UseServiceProviderFactory` or `IHostApplicationBuilder.ConfigureContainer`. It must be the host's actual Microsoft DI factory, not a wrapper around another container. Do not build a second root inside a running host.

For an independently owned service collection, call `services.BuildProtectedDecisionServiceProvider(options)` after all registrations. Dispose the returned root and execute through a child scope, or use the pipeline's providerless overload, which creates its own scope. Executing directly against the root is refused for this capability.

ASP.NET request scopes, explicitly supplied native scopes and scopes created through an injected native `IServiceScopeFactory` use the same proof. A forwarding provider, a custom container, a disposed scope or a collection populated by copying descriptors does not acquire the original root's evidence. `CommandScenario` continues to build its ordinary provider by default.

## Configure rules without an arbitrary factory

Let convention discovery register the validator. Its only public constructor may take recognized decision reads and recursively constructor-bound transient collaborators. Using the domain types from [Validators and read protection](./validators.md), mark `BorrowBook` with `[ProtectedDecision]` (`Cratis.Arc.Chronicle.ReadModels`) and replace its plain-read validator with:

```csharp
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using FluentValidation;

public class BorrowBookValidator : CommandValidator<BorrowBook>
{
    public BorrowBookValidator(DecisionRead<BookStock> stock) =>
        RuleFor(command => command.Isbn)
            .Must(_ => stock.Instance is { Available: > 0 })
            .WithMessage("No copies of this book are available.");
}
```

If you need to add rules at registration time, use `AddProtectedDecisionValidator<TValidator>(configure)` rather than a DI factory. For that token-taking validator:

```csharp
using Cratis.Arc.DependencyInjection;
using FluentValidation;

builder.Services.AddProtectedDecisionValidator<BorrowBookValidator>(validator =>
    validator.RuleFor(command => command.Member)
        .NotNull()
        .WithMessage("A member is required."));
```

Arc constructs a fresh validator and its inspected collaborators for each validation invocation, runs the callback once on that exact instance, then records its rule and component identities. A callback-added denial remains a denial; Arc never substitutes an undecorated validator. A thrown callback fails the command. On ordinary commands, the registration still runs the callback using ordinary constructor resolution, with DI owning the returned validator.

This registration removes at most one Arc convention binding. It does not remove application bindings: a second explicit registration makes the protected construction plan ineligible. The callback and validator constructor are trusted application code, not a sandbox.

## Know what is refused

Arc checks the complete constructor dependency plan before constructing its validators or collaborators. It accepts a discovered unregistered validator, a single Arc convention transient binding, or a single Arc-owned configuration registration. Each type needs exactly one public constructor.

| Dependency or registration | Result |
| --- | --- |
| Framework-issued `DecisionRead<T>` or the framework command-aware `IDecisionReads` | Accepted after invocation checks |
| Constructor-bound transient collaborator with an entirely inspectable graph | Constructed fresh; never taken from a DI cache |
| Scoped or singleton validator or collaborator | Refused |
| Application validator type binding, ordinary factory or prebuilt instance | Refused; no fallback that drops its rules |
| Factory or instance collaborator, cycles, ambiguous constructors, keyed bindings or open generic construction | Refused |
| Provider, scope factory, deferred factory, optional constructor parameter or collection dependency | Refused, including `IEnumerable<IDecisionReads>` and token collections |
| Plain Chronicle read model, raw `IReadModels`, `IEventStore` or `IEventLog` | Refused; use a token, or move to `On` |
| Replaced or duplicated token binding | Refused |

The effective `IDecisionReads` binding must be the framework's scoped command-aware binding. Every earlier binding must also have a framework-recorded origin; standard `WithChronicle` composition may retain superseded raw readers. Any unrecorded type, factory, instance or copied binding anywhere in that list refuses this construction path. Collections are not allowed to resolve those superseded readers.

Register Chronicle through Arc's `WithChronicle`: client registrations made directly by the application outside that call have no recorded origin and are refused by this new construction path.

A closed framework token factory may coexist with Arc's origin-recorded open token convention binding, which is never activated; an application-added open binding is still refused.

Registration evidence is frozen when each root is built. Mutating the original collection afterward cannot add or remove rules in that root. Every new-path validator is bound to its executing scope and command invocation, and its rule/component identities are rechecked before invocation. Changing those identities or reusing an instance across scopes or invocations refuses. Captures, static reads, condition changes and nested-child mutations remain [outside the certificate](./validators.md#what-arc-does-not-check).

Arc releases owned validators and collaborators in reverse construction order after model validation, including rejection, exceptions and cancellation. Read leaves stay DI-owned. Cleanup failures fail the command before commit; a caller's allowed-severity override cannot turn a construction or cleanup refusal into success.

`Validate` remains advisory: it uses its own fresh invocation, does not append and does not promise that a subsequent `Execute` will commit. For callback-added business denials to block under `Off`, declare the appropriate [blocking severity](../../commands/validation-severity-filtering.md).
