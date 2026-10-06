// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_authentication_enforcement;

public class with_asynchronously_disposable_scheme_provider : given.a_host
{
    IAuthenticationSchemeProvider _schemeProvider;

    void Establish()
    {
        _schemeProvider = Substitute.For<IAuthenticationSchemeProvider, IAsyncDisposable>();
        _schemeProvider.GetDefaultAuthenticateSchemeAsync().Returns(new AuthenticationScheme("Discovery", null, typeof(CookieAuthenticationHandler)));
        _registrations.AddScoped(_ => _schemeProvider);
        _registrations.AddAuthorization();
        Build();
    }

    void Because() => _problem = _guard.FindEnforcementProblem(_services);

    [Fact] void should_allow_authenticated_discovery() => _problem.ShouldBeNull();
    [Fact] void should_dispose_the_scheme_provider_asynchronously() => ((IAsyncDisposable)_schemeProvider).Received(1).DisposeAsync();
}
