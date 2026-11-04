// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Microsoft.Extensions.Hosting;
using Samples.Authentication;

namespace Samples.ArcCore.Specs.for_CookieAuthenticationHandler.given;

public class a_handler_in_environment : Specification
{
    const string ForgedPrincipal = /*lang=json,strict*/ """{"userId":"forged-3657","userDetails":"forged","userRoles":["admin"],"claims":[]}""";
    protected AuthenticationResult _result;

    protected async Task<AuthenticationResult> Handle(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var context = Substitute.For<IHttpRequestContext>();
        context.Cookies.Returns(new Dictionary<string, string>
        {
            [CookieAuthenticationHandler.CookieName] = Convert.ToBase64String(Encoding.UTF8.GetBytes(ForgedPrincipal))
        });

        return await new CookieAuthenticationHandler(environment).HandleAuthentication(context);
    }
}
