// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_a_native_role_requirement_allows : Specification
{
    readonly CommandScenario<RunAdministration> _scenario = new();
    CommandResult _result = default!;

    void Establish()
    {
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Administrator")], "test")));
        _scenario.Services.AddSingleton(accessor);
    }

    async Task Because() => _result = await _scenario.Execute(new RunAdministration());

    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_execute_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_handler_response() => ((CommandResult<string>)_result).Response.ShouldEqual("Completed");

    void Destroy() => _scenario.Dispose();
}
