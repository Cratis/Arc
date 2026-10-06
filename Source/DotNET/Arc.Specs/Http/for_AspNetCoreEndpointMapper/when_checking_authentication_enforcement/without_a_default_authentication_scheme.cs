// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_authentication_enforcement;

public class without_a_default_authentication_scheme : given.a_host
{
    void Establish()
    {
        _registrations.AddAuthentication().AddCookie("Other");
        _registrations.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = null;
            options.DefaultAuthenticateScheme = null;
        });
        _registrations.AddAuthorization();
        Build();
    }

    void Because() => _problem = _guard.FindEnforcementProblem(_services);

    [Fact] void should_report_missing_default_scheme() => _problem.ShouldEqual("Requiring authentication on the discovery endpoints needs a default ASP.NET Core authentication scheme.");
}
