// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.given;

public class a_legacy_handler : a_handler
{
    protected async Task<AuthenticateResult> AuthenticateLegacy(bool? trust)
    {
        var services = new ServiceCollection();
        if (trust.HasValue)
        {
            services.AddOptions<ArcOptions>().Configure(options => options.TrustForwardedIdentityHeaders = trust.Value);
        }
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = IdentityIdFromHeader;
        context.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = IdentityNameFromHeader;
        context.Request.Headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader] = PrincipalHeaderValue("aad");
        var options = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());
        var handler = new compatible_header_handler(options, NullLoggerFactory.Instance, UrlEncoder.Default);
        await handler.InitializeAsync(new AuthenticationScheme("LegacyHeaders", null, typeof(compatible_header_handler)), context);
        return await handler.AuthenticateAsync();
    }

#pragma warning disable CS0618 // Pins the original public constructor used by existing subclasses.
    class compatible_header_handler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : MicrosoftIDentityPlatformAuthHandler(options, logger, encoder);
#pragma warning restore CS0618
}
