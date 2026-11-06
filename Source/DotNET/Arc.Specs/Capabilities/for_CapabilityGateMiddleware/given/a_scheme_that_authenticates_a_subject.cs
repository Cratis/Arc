// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware.given;

public class a_scheme_that_authenticates_a_subject : a_capability_gate
{
    protected const string OtherScheme = "other";

    /// <summary>Gets the claims the other scheme authenticates.</summary>
    /// <returns>The claims, or null to authenticate no one.</returns>
    protected virtual IReadOnlyList<Claim>? OtherClaims() => null;

    protected override void Configure(IServiceCollection services)
    {
        services.AddSingleton(new OtherSchemeClaims(OtherClaims()));
        services.AddAuthentication(options => options.DefaultAuthenticateScheme = OtherScheme)
            .AddScheme<AuthenticationSchemeOptions, OtherSchemeHandler>(OtherScheme, null);
    }

    sealed record OtherSchemeClaims(IReadOnlyList<Claim>? Claims);

    sealed class OtherSchemeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        OtherSchemeClaims claims) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(claims.Claims is null
                ? AuthenticateResult.NoResult()
                : AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims.Claims, OtherScheme)), OtherScheme)));
    }
}
