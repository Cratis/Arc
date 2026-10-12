// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_a_policy_runtime_is_registered : Specification
{
    readonly CommandScenario<InspectPolicyRuntime> _scenario = new();
    IAuthorizationPolicyRuntime _runtime = default!;
    CommandResult _result = default!;

    void Establish()
    {
        var resolution = Substitute.For<IAuthorizationPolicyResolution>();
        resolution.SelectPrincipal(Arg.Any<ClaimsPrincipal?>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.ArgAt<ClaimsPrincipal?>(0)));
        _runtime = Substitute.For<IAuthorizationPolicyRuntime>();
        _runtime.Resolve(Arg.Any<IReadOnlyList<AuthorizationRequirement>>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(resolution));
        _scenario.Services.AddSingleton(_runtime);
    }

    async Task Because() => _result = await _scenario.Execute(new InspectPolicyRuntime(_runtime));

    [Fact] void should_execute_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_resolve_the_registered_instance() => ((CommandResult<bool>)_result).Response.ShouldBeTrue();
    [Fact] void should_prepare_authorization_with_the_registered_runtime() =>
        _runtime.Received(1).Resolve(Arg.Any<IReadOnlyList<AuthorizationRequirement>>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());

    void Destroy() => _scenario.Dispose();
}
