// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_one_of_two_scenarios_registers_a_policy_runtime : Specification
{
    readonly CommandScenario<InspectPolicyRuntime> _overriding = new();
    readonly CommandScenario<ReportSingletons> _other = new();
    IAuthorizationPolicyRuntime _runtime = default!;
    CommandResult _overridingResult = default!;
    ResolvedSingletons _fromTheOther = default!;

    void Establish()
    {
        var resolution = Substitute.For<IAuthorizationPolicyResolution>();
        resolution.SelectPrincipal(Arg.Any<ClaimsPrincipal?>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.ArgAt<ClaimsPrincipal?>(0)));
        _runtime = Substitute.For<IAuthorizationPolicyRuntime>();
        _runtime.Resolve(Arg.Any<IReadOnlyList<AuthorizationRequirement>>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(resolution));
        _overriding.Services.AddSingleton(_runtime);
    }

    async Task Because()
    {
        _overridingResult = await _overriding.Execute(new InspectPolicyRuntime(_runtime));
        _fromTheOther = ((CommandResult<ResolvedSingletons>)await _other.Execute(new ReportSingletons())).Response!;
    }

    [Fact] void should_resolve_the_registered_runtime_in_the_overriding_scenario() => ((CommandResult<bool>)_overridingResult).Response.ShouldBeTrue();
    [Fact] void should_not_resolve_it_in_the_other_scenario() => _fromTheOther.PolicyRuntime.ShouldNotEqual(_runtime);
    [Fact] void should_resolve_arcs_runtime_in_the_other_scenario() => _fromTheOther.PolicyRuntime.ShouldBeOfExactType<ArcAuthorizationPolicyRuntime>();

    void Destroy()
    {
        _overriding.Dispose();
        _other.Dispose();
    }
}
