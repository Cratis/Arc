// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_a_scheme_is_requested : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();
    QueryResult _result = default!;

    void Establish()
    {
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("permission", "read")], "test")));
        _scenario.Services.AddSingleton(accessor);
        _scenario.Services.AddArcAuthorizationPolicy<CanReadPolicy>("CanRead");
    }
    async Task Because() => _result = await _scenario.Perform(nameof(ScenarioReadModel.SchemeRestricted));

    [Fact] void should_fail_closed() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_return_data() => _result.Data.ShouldBeNull();

    void Destroy() => _scenario.Dispose();
}
