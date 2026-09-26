// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_multiple_principal_accessors_are_registered : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();

    void Establish()
    {
        foreach (var name in new[] { "first", "second" })
        {
            var accessor = Substitute.For<ICurrentPrincipalAccessor>();
            accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test")));
            _scenario.Services.AddSingleton(accessor);
        }
    }

    async Task Because() => await _scenario.Perform(nameof(ScenarioReadModel.All));

    [Fact] void should_preserve_both_explicit_registrations() =>
        _scenario.Services.Count(registration => registration.ServiceType == typeof(ICurrentPrincipalAccessor)).ShouldEqual(2);

    void Destroy() => _scenario.Dispose();
}
