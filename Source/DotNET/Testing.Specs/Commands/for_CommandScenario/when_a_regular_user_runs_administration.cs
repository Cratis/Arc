// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_a_regular_user_runs_administration : Specification
{
    readonly CommandScenario<RunAdministration> _scenario = new();
    CommandResult _result = default!;

    void Establish()
    {
        var principalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        principalAccessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "User")],
            authenticationType: "test")));
        _scenario.Services.AddSingleton(principalAccessor);
    }

    async Task Because() => _result = await _scenario.Execute(new RunAdministration());

    [Fact] void should_not_be_authorized() => _result.ShouldNotBeAuthorized();
    [Fact] void should_not_execute_successfully() => _result.IsSuccess.ShouldBeFalse();

    void Destroy() => _scenario.Dispose();
}
