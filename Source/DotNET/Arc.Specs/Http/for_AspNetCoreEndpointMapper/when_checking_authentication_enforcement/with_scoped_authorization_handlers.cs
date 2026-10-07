// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_authentication_enforcement;

public class with_scoped_authorization_handlers : given.a_host
{
    bool _handlerCreated;

    void Establish()
    {
        _registrations.AddAuthorization();
        _registrations.AddScoped(_ =>
        {
            _handlerCreated = true;
            return Substitute.For<IAuthorizationHandler>();
        });
        Build();
    }

    void Because() => _problem = _guard.FindEnforcementProblem(_services);

    [Fact] void should_allow_authenticated_discovery() => _problem.ShouldBeNull();
    [Fact] void should_not_construct_authorization_handlers() => _handlerCreated.ShouldBeFalse();
}
