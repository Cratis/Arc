// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_authentication_enforcement;

public class with_scoped_authentication_scheme_provider : given.a_host
{
    void Establish()
    {
        _registrations.AddAuthorization();
        _registrations.AddScoped<IAuthenticationSchemeProvider>(services => new AuthenticationSchemeProvider(services.GetRequiredService<IOptions<AuthenticationOptions>>()));
        Build();
    }

    void Because() => _problem = _guard.FindEnforcementProblem(_services);

    [Fact] void should_allow_authenticated_discovery() => _problem.ShouldBeNull();
}
