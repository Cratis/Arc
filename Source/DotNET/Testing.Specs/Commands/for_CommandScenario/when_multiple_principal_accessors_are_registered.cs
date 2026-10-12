// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_multiple_principal_accessors_are_registered : Specification
{
    readonly CommandScenario<RunAdministration> _scenario = new();
    CommandResult _result = default!;

    void Establish()
    {
        foreach (var role in new[] { "User", "Administrator" })
        {
            var accessor = Substitute.For<ICurrentPrincipalAccessor>();
            accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test")));
            _scenario.Services.AddSingleton(accessor);
        }
    }

    async Task Because() => _result = await _scenario.Execute(new RunAdministration());

    [Fact] void should_preserve_both_explicit_registrations() =>
        _scenario.Services.Count(registration => registration.ServiceType == typeof(ICurrentPrincipalAccessor)).ShouldEqual(2);
    [Fact] void should_authorize_using_the_last_registered_accessor() => _result.IsAuthorized.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
