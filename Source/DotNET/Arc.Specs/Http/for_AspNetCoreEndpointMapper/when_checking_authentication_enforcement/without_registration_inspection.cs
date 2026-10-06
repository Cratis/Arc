// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_authentication_enforcement;

public class without_registration_inspection : given.a_host
{
    IAuthorizationHandler _handler;

    void Establish()
    {
        _handler = Substitute.For<IAuthorizationHandler, IDisposable>();
        _registrations.AddAuthorization();
        _registrations.AddScoped(_ => _handler);
        Build(supportsRegistrationInspection: false);
    }

    void Because() => _problem = _guard.FindEnforcementProblem(null);

    [Fact] void should_allow_authenticated_discovery() => _problem.ShouldBeNull();
    [Fact] void should_dispose_scoped_authorization_handlers() => ((IDisposable)_handler).Received(1).Dispose();
}
