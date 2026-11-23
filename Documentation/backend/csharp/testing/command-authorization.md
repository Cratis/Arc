---
uid: Arc.Testing.CommandAuthorization
title: Test command authorization without executing a command
description: Evaluate an explicit caller with real Arc authorization in a dedicated scope, with separate authorized, denied and unsupported outcomes.
---

Use `Cratis.Arc.Testing.Commands.CommandAuthorization` to ask Arc whether an explicit principal satisfies a command's authorization declaration. It uses the real `AuthorizationDeclarations`, `AuthorizationEvaluation` and policy runtime. It does not execute the command pipeline.

The result is one of **Authorized**, **Denied** or **Unsupported**. Unsupported is not a denial: it means the standalone adapter cannot evaluate the configured boundary. For example, a named authentication scheme needs its host.

## Start with a role requirement

This complete specification uses an authorization-only provider with real Arc services. Add it to a project referencing `Cratis.Arc.Testing` and `Cratis.Specifications`. It has no command filters, tenant membership or HTTP host to reproduce.

```csharp
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Command]
[Authorize(Roles = "Writer")]
public record PublishArticle(string Title)
{
    public void Handle() { }
}

public class when_a_writer_publishes : Specification
{
    ServiceProvider _root;
    IServiceScope _scope;
    CommandAuthorizationResult _result;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpRequestContextAccessor, HttpRequestContextAccessor>();
        services.AddSingleton<CurrentPrincipalAccessor>();
        services.AddSingleton<ICurrentPrincipalAccessor>(provider => provider.GetRequiredService<CurrentPrincipalAccessor>());
        services.AddSingleton<ICurrentPrincipalOverride>(provider => provider.GetRequiredService<CurrentPrincipalAccessor>());
        services.AddSingleton<IInstancesOf<IAnonymousEvaluator>>(new KnownInstancesOf<IAnonymousEvaluator>([new AnonymousEvaluator()]));
        services.AddSingleton<IInstancesOf<IAuthorizationAttributeEvaluator>>(new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new AuthorizationAttributeEvaluator()]));
        services.AddSingleton<AuthorizationDeclarations>();
        services.AddSingleton<IAuthorizationEvaluator, AuthorizationEvaluator>();
        services.AddScoped<IAuthorizationPolicyRuntime, ArcAuthorizationPolicyRuntime>();
        services.AddScoped<AuthorizationEvaluation>();
        _root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _root.CreateScope();
    }

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Writer")], "spec"));
        _result = await CommandAuthorization.For<PublishArticle>(_scope.ServiceProvider, principal);
    }

    [Fact] void should_authorize_the_writer() => _result.ShouldBeAuthorized();

    void Destroy()
    {
        _scope.Dispose();
        _root.Dispose();
    }
}
```

Run `dotnet test`: the writer is authorized. Replace the role with `Reader` and use `ShouldBeDenied()` to specify rejection. The title is not constructed or inspected in this type-only evaluation. `ShouldBeUnsupported()` asserts that a standalone evaluation is unavailable; neither of the other assertions accepts Unsupported.

For an application's actual authorization behavior, use its service registrations, including policies, declaration evaluators and fallback requirements, in a **dedicated scope**. The helper does not install missing services, replace evaluators or modify application registrations. Do not remove application filters or membership checks to force an Authorized result. Use a scenario instead when those checks matter.

## Evaluate a resource-dependent policy

Policies and fallback requirements are discovered by the configured runtime. Requirements on the type take precedence over fallback requirements; `[AllowAnonymous]` is resolved by Arc rather than reimplemented by the helper.

Type-only evaluation supports authentication and roles. Async requirements need a real resource:

```csharp
var result = await CommandAuthorization.For(
    scope.ServiceProvider, principal, new PublishArticle("A draft"), cancellationToken);
result.ShouldBeAuthorized();
```

Register named policies with `AddArcAuthorizationPolicy<TPolicy>("name")` in that provider. The helper awaits asynchronous policy work and forwards cancellation. Missing policies are Unsupported, not successful authorization. Policy exceptions other than recognized runtime refusals propagate unchanged, including cancellation.

For policy fixtures that need dependencies, values, a fixed received time or validation-severity settings, construct a `CommandContext` and pass it to `ForContext<TCommand>(services, principal, context, cancellationToken)`. Its type and command instance must match `TCommand`; its provider must be the supplied provider or null. Supply a context that has never been authorized. The helper creates a fresh context using those fixtures and evaluates it with the explicit principal. It does not bind missing dependencies or copy an old authorization verdict back to the fixture.

## Understand the boundary

The helper never calls `Handle()`, `Provide()`, validators, command filters, command operations or commit/append stages. However, a policy or lazy context provider can execute application code, read protected state or have side effects. This is **not** a promise of pure or side-effect-free evaluation.

The explicit principal is temporarily installed and restored, including on rejection, exceptions and cancellation. Use separate scopes for parallel evaluations. The helper also enters a temporary command-caller frame with no execution host. It cannot manufacture the pipeline's fresh-scope host or its operation snapshot.

Recognized caller refusals, including `UnauthorizedCause` during preparation, produce Denied. Escaping `ReadModelKeyResolutionDenied` (also when wrapped) produces Unsupported with `read-model-key-resolution`. The helper does not reproduce key-resolution refusals during pipeline binding. It likewise does not reproduce tenant-membership verification.

Commands marked `[RequiresApproval]`, including inherited requirements, are Unsupported with `approval-required`. This applies both with and without an approval flow: a registered flow is arbitrary application behavior, and without one both pipeline execution and validation refuse the command. Use a command scenario to exercise that boundary.

Custom command filters are Unsupported with `custom-filter`, including ordinary `ICommandFilter` implementations that do not implement an authorization marker interface. They can still refuse a command. The helper recognizes audited built-in filters by exact type, not by an authorization marker or namespace. Arc's built-in authorization filter is reproduced by the helper; its built-in validation filters (including Chronicle's required-concept filter) are not executed. Do not infer that validation will pass from an Authorized result.

Arc's real built-in `AvailabilityFilter` is supported when no gate is registered or `IAvailabilityGate.IsGated(commandType)` is false. A gated command is Unsupported with `availability-gate`. The helper only checks `IsGated`; it does not call `Decide` or run the availability filter. Thus an application using the standard built-in filters can evaluate an ungated command without removing registrations.

Unsupported results expose a stable reason code and a safe explanation, without principal claims, resource contents or exception messages:

| Reason code | Next step |
| --- | --- |
| `services-missing` | Register Arc authorization in the scoped provider. |
| `runtime-unsupported` | Use a hosted scenario for HTTP or custom policy runtimes. |
| `host-required` | Use a hosted scenario for an active request or named authentication scheme. |
| `principal-unavailable` | Use compatible principal accessor and override registrations. |
| `principal-selection` | Use a hosted scenario for an authenticated replacement principal. |
| `resource-required` | Supply an instance or an explicit context. |
| `context-invalid` | Supply a matching, unevaluated context owned by this provider. |
| `invalid-configuration` | Correct the declaration or policy registration. |
| `async-runtime-required` | Use an asynchronous authorization runtime. |
| `approval-required` | Use a command scenario for a command requiring approval, with or without a registered flow. |
| `availability-gate` | Use a command scenario to evaluate a gated command's availability. |
| `custom-filter` | Use a command scenario for any custom command filter, including ordinary filters. |
| `tenant-membership` | Use a hosted scenario for tenant membership and operation snapshots. |
| `read-model-key-resolution` | Use a command scenario to exercise binding and caller-derived lookups. |

**Authorized does not mean the command will run.** Availability, protected binding, validation and other pipeline stages can still reject it. Use [command scenarios](./command-scenario.md) for the full command pipeline and a hosted test for middleware, endpoint routing, authentication schemes and tenant membership.
