// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.given;

public class a_gate_over_cookie_schemes : an_aspnet_request
{
    internal CredentialPresentationGate _gate;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddAuthentication().AddCookie("Custom", options => options.Cookie.Name = "my-app-session");
        var provider = services.BuildServiceProvider();
        _gate = new(additionalSessionCookieNames: new CookieSchemeSessionCookieNames(provider));
    }
}
